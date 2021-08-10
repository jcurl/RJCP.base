namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;

    internal class ConsoleDataEventArgs : EventArgs
    {
        public ConsoleDataEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}
