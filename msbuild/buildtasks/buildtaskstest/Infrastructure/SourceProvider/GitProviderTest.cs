namespace RJCP.MSBuildTasks.Infrastructure.SourceProvider
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Infrastructure.Process;
    using Infrastructure.Tools;
    using NUnit.Framework;
    using RJCP.CodeQuality.NUnitExtensions;

    [TestFixture]
    public class GitProviderTest
    {
        // The GitProvider queries the object ToolFactory.Instance to get the command to execute GIT commands. The
        // TestToolFactory is used to return a GitToolMock, and that returns a GitSimProcess which simulates as if GIT
        // were run.
        //
        // For the various test cases, the GitSimProcess simulates multiple repositories. The responses and the test
        // cases depend on the commands given by the virtual repositories.
        //
        // Because the GitProvider checks the path exists before it executes, it's important to ensure that testing is
        // done on a path that is real, then everything underneath it is virtual. As the path may change per machine the
        // test cases run on, the GitToolMock.VirtualTopLevel is set.
        //
        // See comments in GitSimProcess for the virtual repositories and the expected behaviour, which is used in these
        // test cases.

        [Test]
        public async Task GetGitProvider()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("normal-utc", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);
                Assert.That(gitprovider, Is.TypeOf<GitProvider>());
                Assert.That(gitprovider.RevisionControlType, Is.EqualTo("git"));
            }
        }

        [Test]
        public void GetGitProviderUnavailable()
        {
            GitToolMock git;
            TestToolFactory factory = TestToolFactory.InitToolFactory();
            factory.GitToolAvailable = false;
            factory.ToolCreatedEvent += (s, e) => {
                git = (GitToolMock)e.Tool;
                git.VirtualTopLevel = Environment.CurrentDirectory;
            };

            Assert.That(async () => {
                _ = await SourceFactory.Instance.CreateAsync("git", Environment.CurrentDirectory);
            }, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task GitProviderNoRepo()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("norepo", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(async () => { _ = await gitprovider.GetCommitAsync("."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());

                Assert.That(async () => { _ = await gitprovider.GetCommitShortAsync("."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());

                Assert.That(async () => { _ = await gitprovider.GetCommitDateTimeAsync("."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());

                Assert.That(async () => { _ = await gitprovider.GetCurrentBranchAsync("."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());

                Assert.That(await gitprovider.IsDirtyAsync("."), Is.True);

                Assert.That(async () => { _ = await gitprovider.IsTaggedAsync("tag", "."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());
            }
        }

        [Test]
        public async Task GitProviderEmptyRepo()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("emptyrepo", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(async () => { _ = await gitprovider.GetCommitAsync("."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());

                Assert.That(async () => { _ = await gitprovider.GetCommitShortAsync("."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());

                Assert.That(async () => { _ = await gitprovider.GetCommitDateTimeAsync("."); },
                    Throws.TypeOf<InvalidOperationException>().Or.TypeOf<RunProcessException>());

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.True);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);
            }
        }

        private static DateTime GetDateTime(int year, int month, int day, int hour, int minute, int second, double hourOffset)
        {
            DateTime dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            return dateTime.AddHours(-hourOffset);
        }

        [Test]
        public async Task GitProviderRepoCommitUtc()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("normal-utc", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);
            }
        }

        [Test]
        public async Task GitProviderRepoCommitUsa()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("normal-usa", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7f"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 7, 13, 46, -6))); // 2016-06-14T07:13:46-06:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);
            }
        }

        [Test]
        public async Task GitProviderRepoCommitEu()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("normal-eu", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7e"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 16, 13, 46, 3))); // 2016-06-14T16:13:46+03:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);
            }
        }

        [Test]
        public async Task GitProviderRepoDetachedHead()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("detached", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("270d954ae8550af71f45cd39e69aab92ff672fcd"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("270d954"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 5, 25, 15, 34, 13, 3))); // 2016-05-25T15:34:13+03:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo(string.Empty));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.True);  // We've checked out to this tag
            }
        }

        [Test]
        public async Task GitProviderRepoDirtyUnstaged()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("dirty-unstaged", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.True);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);
            }
        }

        [Test]
        public async Task GitProviderRepoDirtyStaged()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("dirty-staged", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.True);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);
            }
        }

        [Test]
        public async Task GitProviderRepoTagged()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("normal-tagged", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.True);
            }
        }

        [Test]
        public async Task GitProviderRepoTaggedModified()
        {
            using (ScratchPad scratch = GitProviderRepo.GetRepo("normal-tagmod", out string repo)) {
                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", repo);

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);
            }
        }

        [Test]
        public async Task GitProviderQueryTwiceNoTag()
        {
            GitToolMock git = null;
            TestToolFactory factory = TestToolFactory.InitToolFactory();
            using (ScratchPad scratch = Deploy.ScratchPad()) {
                // We do the initialization here as we need the 'GitToolMock' object, which is only available after the
                // callback.
                Deploy.CreateDirectory(Path.Combine(scratch.RelativePath, "normal-utc"));
                string toplevel = Path.Combine(scratch.Path, "normal-utc");
                factory.ToolCreatedEvent += (s, e) => {
                    git = (GitToolMock)e.Tool;
                    git.VirtualTopLevel = toplevel;
                };

                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", toplevel);

                int gitCount;

                //
                // Query 1
                //

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);

                gitCount = git.GitExecutions;

                //
                // Query 2: Best is that the results are cached.
                //

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate2 = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate2.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.False);

                // Everything should have been cached, so there shouldn't be any new calls.
                Assert.That(git.GitExecutions, Is.EqualTo(gitCount));

                //
                // Query 3: Query a different tag, we should see a new git command
                //

                Assert.That(await gitprovider.IsTaggedAsync("mytag2", "."),
                    Is.False);

                // Only one new query is needed because of the new tag. This tag doesn't exist, so it won't diff.
                Assert.That(git.GitExecutions - gitCount, Is.EqualTo(1));
                gitCount = git.GitExecutions;

                //
                // Query 4: Query a different path, but the tag doesn't exist, so no new commands.
                //

                Assert.That(await gitprovider.IsTaggedAsync("mytag2", "config"),
                    Is.False);

                // No new queries are made with this call, as the tag 'mytag2' never existed.
                Assert.That(git.GitExecutions - gitCount, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task GitProviderQueryTwiceWithTag()
        {
            GitToolMock git = null;
            TestToolFactory factory = TestToolFactory.InitToolFactory();
            using (ScratchPad scratch = Deploy.ScratchPad()) {
                // We do the initialization here as we need the 'GitToolMock' object, which is only available after the
                // callback.
                Deploy.CreateDirectory(Path.Combine(scratch.RelativePath, "normal-tagged"));
                string toplevel = Path.Combine(scratch.Path, "normal-tagged");
                factory.ToolCreatedEvent += (s, e) => {
                    git = (GitToolMock)e.Tool;
                    git.VirtualTopLevel = toplevel;
                };

                ISourceControl gitprovider =
                    await SourceFactory.Instance.CreateAsync("git", toplevel);

                int gitCount;

                //
                // Query 1
                //

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.True);

                gitCount = git.GitExecutions;

                //
                // Query 2: Best is that the results are cached.
                //

                Assert.That(await gitprovider.GetCommitAsync("."),
                    Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));

                Assert.That(await gitprovider.GetCommitShortAsync("."),
                    Is.EqualTo("563b794"));

                DateTime commitDate2 = await gitprovider.GetCommitDateTimeAsync(".");
                Assert.That(commitDate2.ToUniversalTime(),
                    Is.EqualTo(GetDateTime(2016, 6, 14, 13, 13, 46, 0))); // 2016-06-14T13:13:46+00:00

                Assert.That(await gitprovider.GetCurrentBranchAsync("."),
                    Is.EqualTo("master"));

                Assert.That(await gitprovider.IsDirtyAsync("."),
                    Is.False);

                Assert.That(await gitprovider.IsTaggedAsync("mytag", "."),
                    Is.True);

                // Everything should have been cached, so there shouldn't be any new calls.
                Assert.That(git.GitExecutions, Is.EqualTo(gitCount));

                //
                // Query 3: Query a different tag, we should see a new git command to get the commit
                //

                Assert.That(await gitprovider.IsTaggedAsync("mytag2", "."),
                    Is.True);

                // Only one new query is needed because of the new tag. The tag 'mytag2' has the same commit as 'mytag'
                // and so no further queries are needed.
                Assert.That(git.GitExecutions - gitCount, Is.EqualTo(1));
                gitCount = git.GitExecutions;

                //
                // Query 4: Query a different path, we should see a new git command for the diff
                //

                Assert.That(await gitprovider.IsTaggedAsync("mytag2", "config"),
                    Is.True);

                // The tag 'mytag2' is cached, but the path is different, so we should expect a new diff.
                Assert.That(git.GitExecutions - gitCount, Is.EqualTo(1));
            }
        }
    }
}