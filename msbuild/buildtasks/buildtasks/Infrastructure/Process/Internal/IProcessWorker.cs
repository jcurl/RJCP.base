namespace RJCP.MSBuildTasks.Infrastructure.Process.Internal
{
    using System;

    internal interface IProcessWorker : IDisposable
    {
        int ExitCode { get; }

        void Start();

        bool Wait(int timeout);

        void Terminate();

        event EventHandler<ConsoleDataEventArgs> OutputDataReceived;

        event EventHandler<ConsoleDataEventArgs> ErrorDataReceived;

        event EventHandler<ProcessExitedEventArgs> ProcessExitEvent;
    }
}
