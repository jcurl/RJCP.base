namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using Infrastructure.Process;

    internal interface IToolFactory
    {
        Executable GetTool(string tool);
    }
}
