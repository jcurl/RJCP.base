namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for a source provider, such as git, etc.
    /// </summary>
    internal interface ISourceFactory
    {
        /// <summary>
        /// Creates the source provider based on the provider name required and the file system path.
        /// </summary>
        /// <param name="provider">The provider name.</param>
        /// <param name="path">The path to manage.</param>
        /// <returns>
        /// A <see cref="ISourceControl"/> object to manage the source at the <paramref name="path"/> given.
        /// </returns>
        Task<ISourceControl> CreateAsync(string provider, string path);
    }
}
