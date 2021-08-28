namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Process;

    internal class GitToolMock : Executable
    {
        private const string GitTool = "git.exe";

        private readonly bool m_Available;

        public GitToolMock() : this(true) { }

        public GitToolMock(bool available) : base()
        {
            m_Available = available;
        }

        protected override string ErrorToolNotAvailable
        {
            get { return "Git (Mock) not found"; }
        }

        public string VirtualTopLevel { get; set; }

        private int m_GitExecutions;

        /// <summary>
        /// Gets the number of times the GIT binary is called.
        /// </summary>
        /// <value>The number of times the GIT binary is called.</value>
        public int GitExecutions { get { return m_GitExecutions; } }

        protected override Task<string> InitializeAsync()
        {
            if (!m_Available) return Task.FromResult<string>(null);
            return Task.FromResult(GitTool);
        }

        protected override Task<RunProcess> ExecuteProcessAsync(params string[] arguments)
        {
            return ExecuteProcessAsync(null, arguments);
        }

        protected override async Task<RunProcess> ExecuteProcessAsync(string workDir, string[] arguments)
        {
            GitSimProcess process = new GitSimProcess(GitTool, workDir,
                RunProcess.Windows.JoinCommandLine(arguments)) {
                VirtualTopLevel = VirtualTopLevel
            };

            Interlocked.Increment(ref m_GitExecutions);
            await process.ExecuteAsync();
            return process;
        }

        protected override Task<RunProcess> ExecuteProcessAsync(string[] arguments, CancellationToken token)
        {
            return ExecuteProcessAsync(null, arguments, token);
        }

        protected override async Task<RunProcess> ExecuteProcessAsync(string workDir, string[] arguments, CancellationToken token)
        {
            GitSimProcess process = new GitSimProcess(GitTool, workDir,
                RunProcess.Windows.JoinCommandLine(arguments)) {
                VirtualTopLevel = VirtualTopLevel
            };

            Interlocked.Increment(ref m_GitExecutions);
            await process.ExecuteAsync(token);
            return process;
        }
    }
}
