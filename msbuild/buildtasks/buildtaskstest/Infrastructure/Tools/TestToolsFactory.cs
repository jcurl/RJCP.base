namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using Infrastructure.Process;

    internal sealed class TestToolFactory : IToolFactory
    {
        public bool SignToolAvailable { get; set; } = true;

        public Executable GetTool(string tool)
        {
            Executable newTool = new SignToolMock(SignToolAvailable);
            OnToolCreated(this, newTool);
            return newTool;
        }

        private void OnToolCreated(object sender, Executable tool)
        {
            EventHandler<ToolCreatedEventArgs> handler = ToolCreatedEvent;
            if (handler != null) handler(sender, new ToolCreatedEventArgs(tool));
        }

        public event EventHandler<ToolCreatedEventArgs> ToolCreatedEvent;
    }
}
