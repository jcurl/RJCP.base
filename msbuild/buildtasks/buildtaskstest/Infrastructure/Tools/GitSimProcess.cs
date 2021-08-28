namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System;
    using System.IO;
    using System.Threading;
    using Infrastructure.Process;

    internal partial class GitSimProcess : RunProcess
    {
        public GitSimProcess(string command, string workDir, string arguments)
            : base(GitTool, command, workDir, arguments) { }

        public string VirtualTopLevel { get; set; }

        private static int GitTool(RunProcess process, string command, string arguments, CancellationToken token)
        {
            GitSimProcess git = (GitSimProcess)process;
            Console.WriteLine($"{git.Command} (CWD={git.WorkingDirectory})");

            string[] args = Windows.SplitCommandLine(arguments);

            string repoType = Path.GetFileName(git.VirtualTopLevel);
            if (!GitResults.TryGetValue(repoType, out GitResults gitSim)) {
                git.LogStdOut("fatal: not a git repository (or any of the parent directories): .git");
                return 128;
            }

            // Most of the work is now simulating GIT
            switch (args[0]) {
            case "rev-parse":
                return GitRevParse(git, gitSim, args);
            case "symbolic-ref":
                return GitGetBranch(git, gitSim, args);
            case "log":
                return GitGetLog(git, gitSim, args);
            case "diff":
                return GitGetDiff(git, gitSim, args);
            case "diff-index":
                return GitGetDirty(git, gitSim, args);
            case "show-ref":
                return GitGetTag(git, gitSim, args);
            default:
                git.LogStdOut("fatal: Unknown command");
                Console.WriteLine($"Unknown command: {command} {arguments}");
                return 128;
            }
        }

        private static int GitRevParse(GitSimProcess git, GitResults sim, string[] arguments)
        {
            // git rev-parse --show-toplevel

            if (arguments.Length != 2) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            if (arguments[1].Equals("--show-toplevel")) {
                git.LogStdOut($"{git.VirtualTopLevel}");
                return 0;
            }

            // git rev-parse HEAD

            if (arguments[1].Equals("HEAD")) {
                git.LogStdOut(sim.HeadCommit.StdOut);
                return sim.HeadCommit.ExitCode;
            }

            git.LogStdOut("fatal: Error in arguments");
            return 128;
        }

        private static int GitGetBranch(GitSimProcess git, GitResults sim, string[] arguments)
        {
            // git symbolic-ref -q --short HEAD

            if (arguments.Length != 4) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            if (!arguments[1].Equals("-q") || !arguments[2].Equals("--short") || !arguments[3].Equals("HEAD")) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            git.LogStdOut(sim.CurrentBranch.StdOut);
            return sim.CurrentBranch.ExitCode;
        }

        private static int GitGetLog(GitSimProcess git, GitResults sim, string[] arguments)
        {
            if (arguments.Length != 5) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            if (!arguments[1].Equals("-1") ||
                !(arguments[2].Equals("--format=%H") || arguments[2].Equals("--format=%cI")) ||
                !arguments[3].Equals("--") ||
                string.IsNullOrWhiteSpace(arguments[4])) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            // git log -1 --format=%H -- path     - Get the commit

            if (arguments[2].Equals("--format=%H")) {
                git.LogStdOut(sim.LastCommit.StdOut);
                return sim.LastCommit.ExitCode;
            }

            // git log -1 --format=%cI -- path    - Get the date

            git.LogStdOut(sim.LastCommitDate.StdOut);
            return sim.LastCommitDate.ExitCode;
        }

        private static int GitGetDiff(GitSimProcess git, GitResults sim, string[] arguments)
        {
            // git diff --quiet commit tagcommit -- path

            if (arguments.Length != 6) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            if (!arguments[1].Equals("--quiet") ||
                string.IsNullOrWhiteSpace(arguments[2]) ||
                string.IsNullOrWhiteSpace(arguments[3]) ||
                !arguments[4].Equals("--") ||
                string.IsNullOrWhiteSpace(arguments[5])) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            git.LogStdOut(sim.GetDiff.StdOut);
            return sim.GetDiff.ExitCode;
        }

        private static int GitGetTag(GitSimProcess git, GitResults sim, string[] arguments)
        {
            // git show-ref -s tag

            if (arguments.Length != 3) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            if (!arguments[1].Equals("-s") ||
                string.IsNullOrWhiteSpace(arguments[2])) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            git.LogStdOut(sim.TagCommit.StdOut);
            return sim.TagCommit.ExitCode;
        }

        private static int GitGetDirty(GitSimProcess git, GitResults sim, string[] arguments)
        {
            // git diff-index --quiet HEAD -- path

            if (arguments.Length != 5) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            if (!arguments[1].Equals("--quiet") ||
                !arguments[2].Equals("HEAD") ||
                !arguments[3].Equals("--") ||
                string.IsNullOrWhiteSpace(arguments[4])) {
                git.LogStdOut("fatal: Error in arguments");
                return 128;
            }

            git.LogStdOut(sim.IsDirty.StdOut);
            return sim.IsDirty.ExitCode;
        }
    }
}
