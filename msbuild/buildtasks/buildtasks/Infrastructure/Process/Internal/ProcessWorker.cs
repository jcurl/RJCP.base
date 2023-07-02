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
        /// This property is undefind if the process has not yet terminated.
        /// </remarks>
        public int ExitCode { get; private set; }

        private Task m_MonitorProcess;
        private Task m_MonitorOutput;

        private readonly ManualResetEventSlim m_ProcessExited = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim m_ProcessOutputClosed = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim m_ProcessTerminated = new ManualResetEventSlim(false);

        /// <summary>
        /// Start the process and monitoring. Raise events as they occur.
        /// </summary>
        public void Start()
        {
            m_MonitorProcess = MonitorProcessAsync();
            m_MonitorOutput = MonitorOutputAsync();
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
                        m_ProcessExited.Set();
                        m_ProcessOutputClosed.Wait(10000);

                        // We ensure this is set before the event, so that if the user disposes
                        // within the event, we don't deadlock.
                        m_ProcessTerminated.Set();
                        OnProcessExitedEvent(this, new ProcessExitedEventArgs(ExitCode));
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
                    OnOutputDataReceived(this, new ConsoleDataEventArgs(line));
                    output[0] = m_Process.StandardOutput.ReadLineAsync();
                }

                result = HandleOutputTask(output[1], out line);
                if (!result) {
                    output[1] = null;
                } else if (line != null) {
                    OnErrorDataReceived(this, new ConsoleDataEventArgs(line));
                    output[1] = m_Process.StandardError.ReadLineAsync();
                }
            }
            m_ProcessOutputClosed.Set();
        }

        /// <summary>
        /// Check the task result of ReadLineAsync().
        /// </summary>
        /// <param name="output">The task that may contain a result.</param>
        /// <param name="line">The line that was retrieved. Is null in case the task wasn't yet finished.</param>
        /// <returns>
        /// Returns true if the output stream is not closed, false if the stream has reached the end.
        /// When the stream has reached the end, you shouldn't query this stream any longer.
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
        /// <param name="timeout">The duration, in milliseconds, to wait for the process to terminate. Provide -1 to wait forever.</param>
        /// <returns>Is true if the process has terminated, false otherwise.</returns>
        public bool Wait(int timeout)
        {
            return m_ProcessTerminated.Wait(timeout);
        }

        public void Terminate()
        {
            try {
                if (!m_Process.HasExited) {
                    m_Process.Kill();
                }
            } catch (InvalidOperationException) {
                /* Ignore that the process has ended */
            } catch (Win32Exception) {
                /* Ignore process termination errors */
            }
        }

        public event EventHandler<ConsoleDataEventArgs> OutputDataReceived;

        public event EventHandler<ConsoleDataEventArgs> ErrorDataReceived;

        public event EventHandler<ProcessExitedEventArgs> ProcessExitEvent;

        private void OnOutputDataReceived(object sender, ConsoleDataEventArgs e)
        {
            EventHandler<ConsoleDataEventArgs> handler = OutputDataReceived;
            if (handler != null) handler(sender, e);
        }

        private void OnErrorDataReceived(object sender, ConsoleDataEventArgs e)
        {
            EventHandler<ConsoleDataEventArgs> handler = ErrorDataReceived;
            if (handler != null) handler(sender, e);
        }

        private void OnProcessExitedEvent(object sender, ProcessExitedEventArgs e)
        {
            EventHandler<ProcessExitedEventArgs> handler = ProcessExitEvent;
            if (handler != null) handler(sender, e);
        }

        public void Dispose()
        {
            // This method may be called from OnProcessExitedEvent, which is in the thread
            // from MonitorProcessAsync. We must be careful not to enter a deadlock.

            if (!m_ProcessTerminated.IsSet) {
                Terminate();
                Wait(1000);
            }
            m_Process.Dispose();
            m_ProcessExited.Dispose();
            m_ProcessTerminated.Dispose();
        }
    }
}
