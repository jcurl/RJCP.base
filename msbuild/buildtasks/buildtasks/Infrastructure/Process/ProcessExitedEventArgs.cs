namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;

    internal class ProcessExitedEventArgs : EventArgs
    {
        public ProcessExitedEventArgs(int result)
        {
            Result = result;
        }

        public int Result { get; private set; }
    }
}
