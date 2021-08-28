namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using System.Threading.Tasks;
    using Infrastructure.Process;

    internal class ToolFactory : IToolFactory
    {
        /// <summary>
        /// Get the SignTool executable.
        /// </summary>
        public const string SignTool = nameof(SignTool);

        /// <summary>
        /// Get the GIT executable.
        /// </summary>
        public const string GitTool = nameof(GitTool);

        private static readonly object s_Lock = new object();
        private static IToolFactory s_ToolFactory;

        /// <summary>
        /// Gets or sets the global instance of this tool factory.
        /// </summary>
        /// <value>A global instance of a <see cref="IToolFactory"/> to get executable tools.</value>
        /// <exception cref="ArgumentNullException">The value given is <see langword="null"/>.</exception>
        public static IToolFactory Instance
        {
            get
            {
                if (s_ToolFactory == null) {
                    lock (s_Lock) {
                        if (s_ToolFactory == null) {
                            s_ToolFactory = new ToolFactory();
                        }
                    }
                }
                return s_ToolFactory;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                lock (s_Lock) {
                    s_ToolFactory = value;
                }
            }
        }

        /// <summary>
        /// Gets the tool from this factory.
        /// </summary>
        /// <param name="tool">The tool to create an instance for.</param>
        /// <returns>An <see cref="Executable"/> for the tool requested.</returns>
        /// <exception cref="ArgumentException">Un unknown tool was requested.</exception>
        /// <exception cref="InvalidOperationException">The tool is not available.</exception>
        public async Task<Executable> GetToolAsync(string tool)
        {
            Executable exe;
            switch (tool) {
            case SignTool:
                exe = new SignTool();
                break;
            case GitTool:
                exe = new GitTool();
                break;
            default:
                throw new ArgumentException(Resources.Infra_Tools_InvalidTool);
            }

            await exe.FindExecutableAsync(true);
            return exe;
        }
    }
}
