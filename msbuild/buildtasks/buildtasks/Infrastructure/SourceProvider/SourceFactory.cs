namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;
    using System.Threading.Tasks;

    internal class SourceFactory : ISourceFactory
    {
        private static readonly object s_Lock = new object();
        private static ISourceFactory s_SourceFactory;

        /// <summary>
        /// Gets or sets the global instance of this source factory.
        /// </summary>
        /// <value>A global instance of a <see cref="ISourceFactory"/> to get executable tools.</value>
        /// <exception cref="ArgumentNullException">The value given is <see langword="null"/>.</exception>
        public static ISourceFactory Instance
        {
            get
            {
                if (s_SourceFactory == null) {
                    lock (s_Lock) {
                        if (s_SourceFactory == null) {
                            s_SourceFactory = new SourceFactory();
                        }
                    }
                }
                return s_SourceFactory;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                lock (s_Lock) {
                    s_SourceFactory = value;
                }
            }
        }

        /// <summary>
        /// Creates the source provider based on the provider name required and the file system path.
        /// </summary>
        /// <param name="provider">The provider name.</param>
        /// <param name="path">The path to manage.</param>
        /// <returns>
        /// A <see cref="ISourceControl"/> object to manage the source at the <paramref name="path"/> given.
        /// </returns>
        /// <exception cref="UnknownSourceProviderException"></exception>
        public Task<ISourceControl> CreateAsync(string provider, string path)
        {
            if (provider.Equals("git", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("auto", StringComparison.OrdinalIgnoreCase)) {
                ISourceFactory factory = new GitSourceFactory();
                return factory.CreateAsync("git", path);
            }

            throw new UnknownSourceProviderException(Resources.Infra_Source_UnknownProvider, provider);
        }
    }
}
