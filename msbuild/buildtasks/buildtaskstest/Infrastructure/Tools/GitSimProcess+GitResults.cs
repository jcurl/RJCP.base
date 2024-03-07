namespace RJCP.MSBuildTasks.Infrastructure.Tools
{
    using System.Collections.Generic;

    // This class simulates the GIT binary, so that we don't need to have GIT actually installed to run unit test
    // cases. The kind of tests that can be run depend on the folder name of the VirtualTopLevel.
    //
    // Repo: ...\norepo
    // * Top Level: Exit Code 128
    //   * fatal: not a git repository (or any of the parent directories): .git
    //
    // Repo: ...\emptyrepo
    // * Top Level: Exit Code 0
    //   * ...\emptyrepo
    //
    // Repo: ...\normal-utc     (last log timestamp is +00:00). No tags.
    //
    // Repo: ...\normal-usa     (last log timestamp is -06:00). No tags.
    //
    // Repo: ...\normal-eu      (last log timestamp is +03:00). No tags.
    //
    // Repo: ...\detached       No tags. Detached HEAD.
    //
    // Repo: ...\dirty-unstaged No tags. Unstaged changes to existing files.
    //
    // Repo: ...\dirty-staged   No tags. Staged changes to existing files.
    //
    // Repo: ...\normal-tagged  Tagged and no differences.
    //
    // Repo: ...\normal-tagmod  Tagged and modified.

    internal struct GitResult
    {
        public GitResult(int exitCode, string result)
        {
            ExitCode = exitCode;
            StdOut = result;
        }

        public int ExitCode { get; private set; }

        public string StdOut { get; private set; }
    }

    internal class GitResults
    {
        public GitResult HeadCommit { get; set; }

        public GitResult LastCommit { get; set; }

        public GitResult LastCommitDate { get; set; }

        public GitResult IsDirty { get; set; }

        public GitResult TagCommit { get; set; }

        public GitResult CurrentBranch { get; set; }

        public GitResult GetDiff { get; set; }
    }

    internal partial class GitSimProcess
    {
        private readonly static Dictionary<string, GitResults> GitResults = new() {
            ["norepo"] = new GitResults {
                HeadCommit = new GitResult(128, "fatal: Not a git repository (or any of the parent directories): .git"),
                LastCommit = new GitResult(128, "fatal: Not a git repository (or any of the parent directories): .git"),
                LastCommitDate = new GitResult(128, "fatal: Not a git repository (or any of the parent directories): .git"),
                IsDirty = new GitResult(128, "fatal: Not a git repository (or any of the parent directories): .git"),
                TagCommit = new GitResult(128, "fatal: Not a git repository (or any of the parent directories): .git"),
                CurrentBranch = new GitResult(128, "fatal: Not a git repository (or any of the parent directories): .git"),
                GetDiff = new GitResult(129, "Not a git repository")
            },
            ["emptyrepo"] = new GitResults {
                HeadCommit = new GitResult(128, "fatal: ambiguous argument 'HEAD': unknown revision or path not in the working tree."),
                LastCommit = new GitResult(128, "fatal: your current branch 'master' does not have any commits yet"),
                LastCommitDate = new GitResult(128, "fatal: your current branch 'master' does not have any commits yet"),
                IsDirty = new GitResult(128, "fatal: bad revision 'HEAD'"),
                TagCommit = new GitResult(1, string.Empty),
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(128, "fatal: git diff not expected when not tagged in test case")
            },
            ["normal-utc"] = new GitResults {
                HeadCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommitDate = new GitResult(0, "2016-06-14T13:13:46+00:00"),
                IsDirty = new GitResult(0, string.Empty),       // No changes
                TagCommit = new GitResult(1, string.Empty),     // Untagged
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(128, "fatal: git diff not expected when not tagged in test case")
            },
            ["normal-eu"] = new GitResults {
                HeadCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7e"),
                LastCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7e"),
                LastCommitDate = new GitResult(0, "2016-06-14T16:13:46+03:00"),
                IsDirty = new GitResult(0, string.Empty),       // No changes
                TagCommit = new GitResult(1, string.Empty),     // Untagged
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(128, "fatal: git diff not expected when not tagged in test case")
            },
            ["normal-usa"] = new GitResults {
                HeadCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7f"),
                LastCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7f"),
                LastCommitDate = new GitResult(0, "2016-06-14T07:13:46-06:00"),
                IsDirty = new GitResult(0, string.Empty),       // No changes
                TagCommit = new GitResult(1, string.Empty),     // Untagged
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(128, "fatal: git diff not expected when not tagged in test case")
            },
            ["detached"] = new GitResults {
                HeadCommit = new GitResult(0, "270d954ae8550af71f45cd39e69aab92ff672fcd"),
                LastCommit = new GitResult(0, "270d954ae8550af71f45cd39e69aab92ff672fcd"),
                LastCommitDate = new GitResult(0, "2016-05-25T15:34:13+03:00"),
                IsDirty = new GitResult(0, string.Empty),       // No changes
                TagCommit = new GitResult(0, "e61fe3337b05fd5dfc3273f60e2811afa3d4a649"),
                CurrentBranch = new GitResult(1, string.Empty), // Detached, no branch
                GetDiff = new GitResult(0, string.Empty)        // Files match the tag
            },
            ["dirty-unstaged"] = new GitResults {
                HeadCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommitDate = new GitResult(0, "2016-06-14T13:13:46+00:00"),
                IsDirty = new GitResult(1, string.Empty),       // Changes
                TagCommit = new GitResult(1, string.Empty),     // Untagged
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(128, "fatal: git diff not expected when not tagged in test case")
            },
            ["dirty-staged"] = new GitResults {
                HeadCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommitDate = new GitResult(0, "2016-06-14T13:13:46+00:00"),
                IsDirty = new GitResult(1, string.Empty),       // Changes
                TagCommit = new GitResult(1, string.Empty),     // Untagged
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(128, "fatal: git diff not expected when not tagged in test case")
            },
            ["normal-tagged"] = new GitResults {
                HeadCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommitDate = new GitResult(0, "2016-06-14T13:13:46+00:00"),
                IsDirty = new GitResult(0, string.Empty),       // No changes
                TagCommit = new GitResult(0, "e61fe3337b05fd5dfc3273f60e2811afa3d4a649"),
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(0, string.Empty)        // Files match the tag
            },
            ["normal-tagmod"] = new GitResults {
                HeadCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommit = new GitResult(0, "563b794078ffc51b8f0154b09c597abb96645f7d"),
                LastCommitDate = new GitResult(0, "2016-06-14T13:13:46+00:00"),
                IsDirty = new GitResult(0, string.Empty),       // No changes
                TagCommit = new GitResult(0, "e61fe3337b05fd5dfc3273f60e2811afa3d4a649"),
                CurrentBranch = new GitResult(0, "master"),
                GetDiff = new GitResult(1, string.Empty)        // Files differ
            }
        };
    }
}
