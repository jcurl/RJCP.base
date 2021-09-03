namespace RJCP.MSBuildTasks
{
    using System;
    using Infrastructure.SourceProvider;
    using Infrastructure.Tools;
    using NUnit.Framework;
    using RJCP.CodeQuality.NUnitExtensions;

    [TestFixture]
    public class RevisionControlTest
    {
        [Test]
        public void GitRepository()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            using (ScratchPad scratch = GitProviderTest.GetRepo("normal-utc", out string repo)) {
                RevisionControl task = new RevisionControl {
                    BuildEngine = buildEngine.BuildEngine,
                    Type = "git",
                    Path = repo,
                };

                bool result = task.Execute();
                buildEngine.DumpErrorEvents();
                Assert.That(result, Is.True);

                Assert.That(task.RevisionControlType, Is.EqualTo("git"));
                Assert.That(task.RevisionControlBranch, Is.EqualTo("master"));
                Assert.That(task.RevisionControlCommit, Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));
                Assert.That(task.RevisionControlCommitShort, Is.EqualTo("563b794"));
                Assert.That(task.RevisionControlDateTime, Is.EqualTo("20160614T131346"));
                Assert.That(task.RevisionControlDirty, Is.EqualTo("False"));
                Assert.That(task.RevisionControlTagged, Is.EqualTo("False"));
                Assert.That(task.RevisionControlHost, Is.EqualTo(Environment.MachineName));
                Assert.That(task.RevisionControlUser, Is.EqualTo(Environment.UserName));
            }
        }

        [Test]
        public void GitRepositoryNonUtc()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            using (ScratchPad scratch = GitProviderTest.GetRepo("normal-eu", out string repo)) {
                RevisionControl task = new RevisionControl {
                    BuildEngine = buildEngine.BuildEngine,
                    Type = "git",
                    Path = repo,
                };

                bool result = task.Execute();
                buildEngine.DumpErrorEvents();
                Assert.That(result, Is.True);

                Assert.That(task.RevisionControlType, Is.EqualTo("git"));
                Assert.That(task.RevisionControlBranch, Is.EqualTo("master"));
                Assert.That(task.RevisionControlCommit, Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7e"));
                Assert.That(task.RevisionControlCommitShort, Is.EqualTo("563b794"));
                Assert.That(task.RevisionControlDateTime, Is.EqualTo("20160614T131346"));
                Assert.That(task.RevisionControlDirty, Is.EqualTo("False"));
                Assert.That(task.RevisionControlTagged, Is.EqualTo("False"));
                Assert.That(task.RevisionControlHost, Is.EqualTo(Environment.MachineName));
                Assert.That(task.RevisionControlUser, Is.EqualTo(Environment.UserName));
            }
        }

        [TestCase(null, TestName = "InvalidPath_Null")]
        [TestCase("", TestName = "InvalidPath_Empty")]
        [TestCase("  ", TestName = "InvalidPath_WhiteSpace")]
        public void InvalidPath(string path)
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            RevisionControl task = new RevisionControl {
                BuildEngine = buildEngine.BuildEngine,
                Type = "git",
                Path = path,
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }

        [TestCase(null, TestName = "InvalidType_Null")]
        [TestCase("", TestName = "InvalidType_Empty")]
        [TestCase("  ", TestName = "InvalidType_WhiteSpace")]
        [TestCase("foo", TestName = "InvalidType_Unknown")]
        public void InvalidType(string revisionType)
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            using (ScratchPad scratch = GitProviderTest.GetRepo("normal-utc", out string repo)) {
                RevisionControl task = new RevisionControl {
                    BuildEngine = buildEngine.BuildEngine,
                    Type = revisionType,
                    Path = repo,
                };

                bool result = task.Execute();
                buildEngine.DumpErrorEvents();
                Assert.That(result, Is.False);
            }
        }

        [TestCase("foo", TestName = "InvalidStrictMode_Unknown")]
        public void InvalidStrictMode(string strict)
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            using (ScratchPad scratch = GitProviderTest.GetRepo("normal-utc", out string repo)) {
                RevisionControl task = new RevisionControl {
                    BuildEngine = buildEngine.BuildEngine,
                    Type = "git",
                    Path = repo,
                    Strict = strict
                };

                bool result = task.Execute();
                buildEngine.DumpErrorEvents();
                Assert.That(result, Is.False);
            }
        }

        [TestCase("yes", false, true, TestName = "GitRepositoryStrictMode_Yes")]
        [TestCase("Yes", false, true, TestName = "GitRepositoryStrictMode_Case")]
        [TestCase("YES", false, true, TestName = "GitRepositoryStrictMode_UPPER")]
        [TestCase(" yes ", false, true, TestName = "GitRepositoryStrictMode_Trimmed")]
        [TestCase("True", false, true, TestName = "GitRepositoryStrictMode_True")]
        [TestCase("enabled", false, true, TestName = "GitRepositoryStrictMode_Enabled")]
        [TestCase("disabled", false, false, TestName = "GitRepositoryStrictMode_Disabled")]
        [TestCase("False", false, false, TestName = "GitRepositoryStrictMode_False")]
        [TestCase("no", false, false, TestName = "GitRepositoryStrictMode_No")]
        [TestCase("yes", true, true, TestName = "GitRepositoryStrictMode_WarningInvalidLabel")]
        public void GitRepositoryStrictMode(string strict, bool withLabel, bool warning)
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            using (ScratchPad scratch = GitProviderTest.GetRepo("normal-utc", out string repo)) {
                RevisionControl task = new RevisionControl {
                    BuildEngine = buildEngine.BuildEngine,
                    Type = "git",
                    Path = repo,
                    Strict = strict,
                    Label = withLabel ? "label" : string.Empty
                };

                bool result = task.Execute();
                buildEngine.DumpErrorEvents();
                Assert.That(result, Is.True);

                Assert.That(task.RevisionControlType, Is.EqualTo("git"));
                Assert.That(task.RevisionControlBranch, Is.EqualTo("master"));
                Assert.That(task.RevisionControlCommit, Is.EqualTo("563b794078ffc51b8f0154b09c597abb96645f7d"));
                Assert.That(task.RevisionControlCommitShort, Is.EqualTo("563b794"));
                Assert.That(task.RevisionControlDateTime, Is.EqualTo("20160614T131346"));
                Assert.That(task.RevisionControlDirty, Is.EqualTo("False"));
                Assert.That(task.RevisionControlTagged, Is.EqualTo("False"));
                Assert.That(task.RevisionControlHost, Is.EqualTo(Environment.MachineName));
                Assert.That(task.RevisionControlUser, Is.EqualTo(Environment.UserName));

                // No label was given to check against, so no warning in case strict mode is enabled.
                Assert.That(buildEngine.BuildWarningEventArgs.Count, Is.EqualTo(warning ? 1 : 0));
            }
        }

        [Test]
        public void GitRepositoryNoRepo()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            using (ScratchPad scratch = GitProviderTest.GetRepo("norepo", out string repo)) {
                RevisionControl task = new RevisionControl {
                    BuildEngine = buildEngine.BuildEngine,
                    Type = "git",
                    Path = repo
                };

                bool result = task.Execute();
                buildEngine.DumpErrorEvents();
                Assert.That(result, Is.False);
            }
        }

        [Test]
        public void GitNotAvailable()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            TestToolFactory factory = TestToolFactory.InitToolFactory();
            factory.GitToolAvailable = false;

            RevisionControl task = new RevisionControl {
                BuildEngine = buildEngine.BuildEngine,
                Type = "git",
                Path = Deploy.WorkDirectory
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }
    }
}
