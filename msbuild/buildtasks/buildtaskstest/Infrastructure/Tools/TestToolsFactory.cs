namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using System.Threading.Tasks;
    using Infrastructure.Process;

    internal sealed class TestToolFactory : IToolFactory
    {
        public static TestToolFactory InitToolFactory()
        {
            TestToolFactory factory = new TestToolFactory();
            ToolFactory.Instance = factory;
            return factory;
        }

        public bool SignToolAvailable { get; set; } = true;

        public bool GitToolAvailable { get; set; } = true;

        public async Task<Executable> GetToolAsync(string tool)
        {
            Executable newTool;
            switch (tool) {
            case ToolFactory.SignTool:
                newTool = new SignToolMock(SignToolAvailable);
                break;
            case ToolFactory.GitTool:
                newTool = new GitToolMock(GitToolAvailable);
                break;
            default:
                throw new ArgumentException(Resources.Infra_Tools_InvalidTool);
            }

            await newTool.FindExecutableAsync(true);
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
