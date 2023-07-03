namespace RJCP.MSBuildTasks.Infrastructure.Process.Internal
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class ProcessWorker : IProcessWorker
    {
        private readonly Process m_Process;
        private readonly ManualResetEventSlim m_ProcessOutputClosed = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim m_ProcessTerminated = new ManualResetEventSlim(false);

        public ProcessWorker(string command, string workingDir, string arguments)
        {
            m_Process = new Process();
            m_Process.StartInfo.FileName = command;
            m_Process.StartInfo.Arguments = arguments;
            m_Process.StartInfo.ErrorDialog = false;
            m_Process.StartInfo.UseShellExecute = false;
            m_Process.StartInfo.CreateNoWindow = true;
            if (workingDir != null)
                m_Process.StartInfo.WorkingDirectory = workingDir;
            m_Process.StartInfo.RedirectStandardError = true;
            m_Process.StartInfo.RedirectStandardOutput = true;
            m_Process.StartInfo.RedirectStandardInput = false;
        }

        /// <summary>
        /// Get the exit code of the process.
        /// </summary>
        /// <remarks>
        /// This property is undefined if the process has not yet terminated.
        /// </remarks>
        public int ExitCode { get; private set; }

        private int m_ProcessStarted = 0;

        /// <summary>
        /// Start the process and monitoring. Raise events as they occur.
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref m_ProcessStarted, 1, 0) != 0)
                throw new InvalidOperationException("Process was already hosted by ProcessWorker.");

            // Threads are started, but we don't monitor them for errors, except that the tasks abort, and we notify the
            // user as such. If there is an abort, the process may continue to run, we don't kill it. But we no longer
            // track it either.

            _ = MonitorProcessAsync().ContinueWith((t) => {
                m_ProcessTerminated.Set();
                if (t.IsFaulted) ExitCode = -1;
                try {
                    OnProcessExitedEvent(this, new ProcessExitedEventArgs(ExitCode));
                } catch { /* Ignore user exceptions */ }
            });

            _ = MonitorOutputAsync().ContinueWith((t) => {
                m_ProcessOutputClosed.Set();
            });
        }

        private async Task MonitorProcessAsync()
        {
            m_Process.Start();
            await Task.Run(() => {
                bool processRunning = true;
                while (processRunning) {
                    processRunning = !m_Process.WaitForExit(2000);
                    if (!processRunning) {
                        ExitCode = m_Process.ExitCode;
                        m_ProcessOutputClosed.Wait(10000);
                    }
                }
            });
        }

        private async Task MonitorOutputAsync()
        {
            Task<string>[] output = new Task<string>[2] {
                m_Process.StandardOutput.ReadLineAsync(),
                m_Process.StandardError.ReadLineAsync()
            };

            bool notifyOutput = true;
            while (true) {
                if (output[0] != null && output[1] != null) {
                    await Task.WhenAny(output);
                } else if (output[0] != null) {
                    await output[0];
                } else if (output[1] != null) {
                    await output[1];
                } else {
                    break;
                }

                bool result = HandleOutputTask(output[0], out string line);
                if (!result) {
                    output[0] = null;
                } else if (line != null) {
                    try {
                        if (notifyOutput)
                            OnOutputDataReceived(this, new ConsoleDataEventArgs(line));
                    } catch {
                        // In case the user raises an exception, we don't notify them. But we still capture traffic so
                        // that the process doesn't block.
                        notifyOutput = false;
                    }
                    output[0] = m_Process.StandardOutput.ReadLineAsync();
                }

                result = HandleOutputTask(output[1], out line);
                if (!result) {
                    output[1] = null;
                } else if (line != null) {
                    try {
                        if (notifyOutput)
                            OnErrorDataReceived(this, new ConsoleDataEventArgs(line));
                    } catch {
                        // In case the user raises an exception, we don't notify them. But we still capture traffic so
                        // that the process doesn't block.
                        notifyOutput = false;
                    }
                    output[1] = m_Process.StandardError.ReadLineAsync();
                }
            }
        }

        /// <summary>
        /// Check the task result of ReadLineAsync().
        /// </summary>
        /// <param name="output">The task that may contain a result.</param>
        /// <param name="line">The line that was retrieved. Is null in case the task wasn't yet finished.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the output stream is not closed, <see langword="false"/> if the stream has
        /// reached the end. When the stream has reached the end, you shouldn't query this stream any longer.
        /// </returns>
        private static bool HandleOutputTask(Task<string> output, out string line)
        {
            line = null;
            if (output == null) return false;
            if (output.IsFaulted) return false;
            if (!output.IsCompleted) return true;

            line = output.Result;
            return line != null;
        }

        /// <summary>
        /// Wait until the process and output is complete.
        /// </summary>
        /// <param name="timeout">
        /// The duration, in milliseconds, to wait for the process to terminate. Provide <see cref="Timeout.Infinite"/>
        /// to wait forever.
        /// </param>
        /// <returns>
        /// Is <see langword="true"/> if the process has terminated, <see langword="false"/> otherwise.
        /// </returns>
        public bool Wait(int timeout)
        {
            // Don't wait if the process isn't yet started.
            if (Volatile.Read(ref m_ProcessStarted) == 0) return false;

            return m_ProcessTerminated.Wait(timeout);
        }

        /// <summary>
        /// Terminates this instance.
        /// </summary>
        public void Terminate()
        {
            try {
                if (Volatile.Read(ref m_ProcessStarted) == 0) return;

                if (!m_Process.HasExited) {
                    m_Process.Kill();
                }
            } catch (InvalidOperationException) {
                /* Ignore that the process has ended */
            } catch (Win32Exception) {
                /* Ignore process termination errors */
            }
        }

        private void OnOutputDataReceived(object sender, ConsoleDataEventArgs e)
        {
            EventHandler<ConsoleDataEventArgs> handler = OutputDataReceived;
            if (handler != null) handler(sender, e);
        }

        /// <summary>
        /// Occurs when a line is received on the standard output.
        /// </summary>
        /// <remarks>
        /// Any exceptions that occur during the event will prevent further events for stdout or stderr from occurring.
        /// </remarks>
        public event EventHandler<ConsoleDataEventArgs> OutputDataReceived;

        private void OnErrorDataReceived(object sender, ConsoleDataEventArgs e)
        {
            EventHandler<ConsoleDataEventArgs> handler = ErrorDataReceived;
            if (handler != null) handler(sender, e);
        }

        /// <summary>
        /// Occurs when a line is received on the standard error.
        /// </summary>
        /// <remarks>
        /// Any exceptions that occur during the event will prevent further events for stdout or stderr from occurring.
        /// </remarks>
        public event EventHandler<ConsoleDataEventArgs> ErrorDataReceived;

        private void OnProcessExitedEvent(object sender, ProcessExitedEventArgs e)
        {
            EventHandler<ProcessExitedEventArgs> handler = ProcessExitEvent;
            if (handler != null) handler(sender, e);
        }

        /// <summary>
        /// Occurs when the process has exited and there is no more output on stdout or stderr.
        /// </summary>
        /// <remarks>Use this to clean up resources. You may call <see cref="Dispose"/> from within this event.</remarks>
        public event EventHandler<ProcessExitedEventArgs> ProcessExitEvent;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting managed or unmanaged
        /// resources.
        /// </summary>
        /// <remarks>
        /// This method is not intended to be used asynchronously with any other method. It may only be used
        /// synchronously with all other API, or within a <see cref="ProcessExitEvent"/>.
        /// </remarks>
        public void Dispose()
        {
            // This method may be called from OnProcessExitedEvent, which is in the thread
            // from MonitorProcessAsync. We must be careful not to enter a deadlock.

            if (!m_ProcessTerminated.IsSet) {
                Terminate();
                Wait(1000);
            }
            m_Process.Dispose();
            m_ProcessOutputClosed.Dispose();
            m_ProcessTerminated.Dispose();
        }
    }
}
