namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using Infrastructure.Process;

    internal class ToolCreatedEventArgs : EventArgs
    {
        public ToolCreatedEventArgs(Executable tool)
        {
            Tool = tool;
        }

        public Executable Tool { get; private set; }
    }
}
