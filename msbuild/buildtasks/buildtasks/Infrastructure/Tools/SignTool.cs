namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Infrastructure.Process;

    internal class SignTool : Executable
    {
        protected override string ErrorToolNotAvailable
        {
            get { return Resources.SignTool_ToolsNotAvailable; }
        }

        protected override async Task<string> InitializeAsync()
        {
            foreach (string signPath in FindFiles("signtool.exe")) {
                if (await CheckToolAsync(signPath)) return signPath;
            }

            string cwd = Path.Combine(Environment.CurrentDirectory, "signtool.exe");
            if (await CheckToolAsync(cwd)) return cwd;

            return null;
        }

        private static Task<bool> CheckToolAsync(string path)
        {
            return CheckFileExistsAsync(path);
        }
    }
}
