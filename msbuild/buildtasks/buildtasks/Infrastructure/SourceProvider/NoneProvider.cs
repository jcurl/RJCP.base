namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;
    using System.Threading.Tasks;

    internal class NoneProvider : ISourceControl
    {
        private readonly DateTime m_DateTime;

        public NoneProvider()
        {
            m_DateTime = DateTime.UtcNow;
        }

        public string RevisionControlType { get { return "none"; } }

        public Task<string> GetCurrentBranchAsync(string path)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<string> GetCommitAsync(string path)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<string> GetCommitShortAsync(string path)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<DateTime> GetCommitDateTimeAsync(string path)
        {
            return Task.FromResult(m_DateTime);
        }

        public Task<bool> IsDirtyAsync(string path)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsTaggedAsync(string tag, string path)
        {
            return Task.FromResult(true);
        }
    }
}
