namespace RJCP.MSBuildTasks.Infrastructure.Process.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class ProcessSim : IProcessWorker
    {
        private readonly RunProcess m_Sender;
        private readonly RunProcess.SimAction m_Simulation;
        private readonly CancellationTokenSource m_TokenSource;
        private readonly string m_Command;
        private readonly string m_Arguments;
        private Task<int> m_SimTask;

        public ProcessSim(RunProcess sender, RunProcess.SimAction simulation, string command, string arguments)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            if (sender == null) throw new ArgumentNullException(nameof(sender));

            m_TokenSource = new CancellationTokenSource();
            m_Sender = sender;
            m_Simulation = simulation;
            m_Command = command;
            m_Arguments = arguments;
        }

        public int ExitCode { get; private set; }

        public void Start()
        {
            m_SimTask = Task.Run(() => {
                return m_Simulation(m_Sender, m_Command, m_Arguments, m_TokenSource.Token);
            }, m_TokenSource.Token).ContinueWith(NotifyCompletion);
        }

        private int NotifyCompletion(Task<int> ct)
        {
            int exitCode = -1;
            if (ct.Status == TaskStatus.RanToCompletion && !m_TokenSource.IsCancellationRequested)
                exitCode = ct.Result;
            OnProcessExitedEvent(this, new ProcessExitedEventArgs(exitCode));
            return exitCode;
        }

        public bool Wait(int timeout)
        {
            return m_SimTask.Wait(timeout);
        }

        public void Terminate()
        {
            if (!m_SimTask.IsCompleted) {
                m_TokenSource.Cancel();
            }
        }

#pragma warning disable 67 // The event is never used
        public event EventHandler<ConsoleDataEventArgs> OutputDataReceived;

        public event EventHandler<ConsoleDataEventArgs> ErrorDataReceived;
#pragma warning restore 67

        public event EventHandler<ProcessExitedEventArgs> ProcessExitEvent;

        private void OnProcessExitedEvent(object sender, ProcessExitedEventArgs e)
        {
            EventHandler<ProcessExitedEventArgs> handler = ProcessExitEvent;
            if (handler != null) handler(sender, e);
        }

        public void Dispose()
        {
            m_TokenSource.Dispose();
        }
    }
}
