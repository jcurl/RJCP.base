namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System.Threading.Tasks;

    internal class GitSourceFactory : ISourceFactory
    {
        public async Task<ISourceControl> CreateAsync(string provider, string path)
        {
            return await GitProvider.CreateAsync(path);
        }
    }
}
