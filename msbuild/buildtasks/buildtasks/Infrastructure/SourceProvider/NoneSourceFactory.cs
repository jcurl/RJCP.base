namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System.Threading.Tasks;

    internal class NoneSourceFactory : ISourceFactory
    {
        public Task<ISourceControl> CreateAsync(string provider, string path)
        {
            ISourceControl none = new NoneProvider();
            return Task.FromResult(none);
        }
    }
}
