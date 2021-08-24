namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System.Threading.Tasks;
    using Infrastructure.Process;

    internal interface IToolFactory
    {
        Task<Executable> GetToolAsync(string tool);
    }
}
