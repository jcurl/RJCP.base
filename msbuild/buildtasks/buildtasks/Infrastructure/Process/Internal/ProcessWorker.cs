namespace RJCP.MSBuildTasks.Infrastructure.Process.Internal
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;

    internal sealed class ProcessWorker : IProcessWorker
    {
        private readonly Process m_Process;

        private bool m_Started;
        private bool m_DisposePending;
        private readonly object m_DisposeSync = new object();

        public ProcessWorker(string command, string workingDir, string arguments)
        {
            m_Process = new Process();
            m_Process.StartInfo.FileName = command;
            m_Process.StartInfo.Arguments = arguments;
            m_Process.StartInfo.ErrorDialog = false;
            m_Process.StartInfo.UseShellExecute = false;
            m_Process.StartInfo.CreateNoWindow = true;
            if (workingDir != null) m_Process.StartInfo.WorkingDirectory = workingDir;
            m_Process.StartInfo.RedirectStandardError = true;
            m_Process.StartInfo.RedirectStandardOutput = true;
            m_Process.StartInfo.RedirectStandardInput = false;
            m_Process.OutputDataReceived += Process_OutputDataReceived;
            m_Process.ErrorDataReceived += Process_ErrorDataReceived;
        }

        public int ExitCode { get; private set; }

        public void Start()
        {
            m_Process.Start();
            m_Process.BeginOutputReadLine();
            m_Process.BeginErrorReadLine();

            // If ProcessExitEvent calls Dispose() while we still haven't finished Start(), then we need to now dispose
            // the object.
            lock (m_DisposeSync) {
                m_Started = true;
                if (m_DisposePending) m_Process.Dispose();
            }
        }

        public bool Wait(int timeout)
        {
            bool completed = m_Process.WaitForExit(timeout);
            if (completed) {
                ExitCode = m_Process.ExitCode;
                OnProcessExitedEvent(this, new ProcessExitedEventArgs(ExitCode));
            }
            return completed;
        }

        private bool m_IsTerminated;

        public void Terminate()
        {
            try {
                if (!m_Process.HasExited) {
                    m_IsTerminated = true;
                    m_Process.Kill();
                }
            } catch (InvalidOperationException) {
                /* Ignore that the process has ended */
            } catch (Win32Exception) {
                /* Ignore process termination errors */
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) {
                ConsoleDataEventArgs dataArgs = new ConsoleDataEventArgs(e.Data);
                OnOutputDataReceived(sender, dataArgs);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) {
                ConsoleDataEventArgs dataArgs = new ConsoleDataEventArgs(e.Data);
                OnErrorDataReceived(sender, dataArgs);
            }
        }

        public event EventHandler<ConsoleDataEventArgs> OutputDataReceived;

        public event EventHandler<ConsoleDataEventArgs> ErrorDataReceived;

        private int m_ProcessExitEventCount;

        private event EventHandler<ProcessExitedEventArgs> LocalProcessExitEvent;

        public event EventHandler<ProcessExitedEventArgs> ProcessExitEvent
        {
            add
            {
                m_ProcessExitEventCount++;
                LocalProcessExitEvent += value;

                if (m_ProcessExitEventCount == 1) {
                    m_Process.EnableRaisingEvents = true;
                    m_Process.Exited += Process_Exited;
                }
            }

            remove
            {
                LocalProcessExitEvent -= value;
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            if (m_IsTerminated) {
                Wait(5000);
            } else {
                Wait(Timeout.Infinite);
            }
        }

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
            EventHandler<ProcessExitedEventArgs> handler = LocalProcessExitEvent;
            if (handler != null) handler(sender, e);
        }

        public void Dispose()
        {
            // If the user calls Dispose() while Start() is running (e.g. because they're calling it from within the
            // ProcessExitEvent), then we need to delay the dispose until we've finished the actual start.
            lock (m_DisposeSync) {
                if (!m_Started) {
                    m_DisposePending = true;
                    return;
                }
            }
            m_Process.Dispose();
        }
    }
}
