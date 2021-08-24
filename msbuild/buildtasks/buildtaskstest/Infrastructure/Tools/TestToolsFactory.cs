namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using System.Threading.Tasks;
    using Infrastructure.Process;

    internal sealed class TestToolFactory : IToolFactory
    {
        public bool SignToolAvailable { get; set; } = true;

        public async Task<Executable> GetToolAsync(string tool)
        {
            Executable newTool;
            switch (tool) {
            case ToolFactory.SignTool:
                newTool = new SignToolMock(SignToolAvailable);
                await newTool.FindExecutableAsync(true);
                break;
            default:
                throw new ArgumentException(Resources.Infra_Tools_InvalidTool);
            }

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
