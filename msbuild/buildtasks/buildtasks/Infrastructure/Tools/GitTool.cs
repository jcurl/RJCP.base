namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Infrastructure.Process;
    using Microsoft.Win32;

    internal class GitTool : Executable
    {
        protected override string ErrorToolNotAvailable
        {
            get { return Resources.Git_ToolsNotAvailable; }
        }

        protected async override Task<string> InitializeAsync()
        {
            if (Platform.IsWinNT()) {
                foreach (string gitPath in FindFiles("git.exe")) {
                    if (await CheckToolAsync(gitPath)) return gitPath;
                }

                try {
                    string gitPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Programs", "Git", "bin", "git.exe");
                    if (await CheckToolAsync(gitPath)) return gitPath;
                } catch (PlatformNotSupportedException) {
                    // Ignore this test
                }

                try {
                    string gitPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "Git", "bin", "git.exe");
                    if (await CheckToolAsync(gitPath)) return gitPath;
                } catch (PlatformNotSupportedException) {
                    // Ignore this test
                }

                try {
                    string gitPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        "Git", "bin", "git.exe");
                    if (await CheckToolAsync(gitPath)) return gitPath;
                } catch (PlatformNotSupportedException) {
                    // Ignore this test
                }

                // If we're running on Windows 64-bit as a 32-bit process, we would probably miss
                // the 64-bit folder which is just as good.
                try {
                    using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)) {
                        string programFiles = (string)key.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion")
                            .GetValue("ProgramFilesDir");
                        string gitPath = Path.Combine(programFiles, "Git", "bin", "git.exe");
                        if (await CheckToolAsync(gitPath)) return gitPath;
                    }
                } catch {
                    /* Ignore errors and just return we couldn't find GIT */
                }
            } else {
                foreach (string gitPath in FindFiles("git")) {
                    if (await CheckToolAsync(gitPath)) return gitPath;
                }
            }

            return null;
        }

        private static async Task<bool> CheckToolAsync(string path)
        {
            if (!CheckFileExists(path)) return false;

            RunProcess git = await RunProcess.RunAsync(path, "version");
            return git.ExitCode == 0;
        }
    }
}
