namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Process;

    internal class SignToolMock : Executable
    {
        private const string SignTool = "signtool.exe";

        private readonly bool m_Available;

        public SignToolMock() : this(true) { }

        public SignToolMock(bool available) : base()
        {
            m_Available = available;
        }

        protected override string ErrorToolNotAvailable
        {
            get { return "SignTool (Mock) not found"; }
        }

        protected override Task<string> InitializeAsync()
        {
            if (!m_Available) return Task.FromResult<string>(null);
            return Task.FromResult(SignTool);
        }

        public string[] SignToolArguments { get; private set; }
        public StoreName ExpectedStoreName { get; set; } = StoreName.My;
        public StoreLocation ExpectedStoreLocation { get; set; } = StoreLocation.CurrentUser;
        public string ExpectedThumbPrint { get; set; } = string.Empty;
        public string ExpectedTimeStampUri { get; set; } = null;

        protected override Task<RunProcess> ExecuteProcessAsync(params string[] arguments)
        {
            return ExecuteProcessAsync(null, arguments);
        }

        protected override async Task<RunProcess> ExecuteProcessAsync(string workDir, string[] arguments)
        {
            SignToolArguments = arguments;
            SignToolSimProcess process = new(SignTool, workDir,
                RunProcess.Windows.JoinCommandLine(arguments)) {
                ExpectedThumbPrint = ExpectedThumbPrint,
                ExpectedStoreLocation = ExpectedStoreLocation,
                ExpectedStoreName = ExpectedStoreName,
                ExpectedTimeStampUri = ExpectedTimeStampUri
            };

            await process.ExecuteAsync();
            return process;
        }

        protected override Task<RunProcess> ExecuteProcessAsync(string[] arguments, CancellationToken token)
        {
            return ExecuteProcessAsync(null, arguments, token);
        }

        protected override async Task<RunProcess> ExecuteProcessAsync(string workDir, string[] arguments, CancellationToken token)
        {
            SignToolArguments = arguments;
            SignToolSimProcess process = new(SignTool, workDir,
                RunProcess.Windows.JoinCommandLine(arguments)) {
                ExpectedThumbPrint = ExpectedThumbPrint,
                ExpectedStoreLocation = ExpectedStoreLocation,
                ExpectedStoreName = ExpectedStoreName,
                ExpectedTimeStampUri = ExpectedTimeStampUri
            };

            await process.ExecuteAsync(token);
            return process;
        }
    }
}
