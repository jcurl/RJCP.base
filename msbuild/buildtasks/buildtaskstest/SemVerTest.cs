namespace RJCP.MSBuildTasks
{
    using NUnit.Framework;

    [TestFixture]
    public class SemVerTest
    {
        [TestCase(null)]         // Must not be null
        [TestCase("")]           // Must not be empty
        [TestCase("a.b.c.d")]    // Must be ditits
        [TestCase("1")]
        [TestCase("1.2")]        // Must have a build number
        [TestCase("65536.65536.65536")]
        [TestCase("0.0.0")]
        [TestCase("0.0.0.0")]
        public void InvalidVersion(string version)
        {
            BuildEngineMock buildEngine = new();
            SemVer task = new() {
                BuildEngine = buildEngine.BuildEngine,
                Version = version
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetSemVerConstituents()
        {
            BuildEngineMock buildEngine = new();
            SemVer task = new() {
                BuildEngine = buildEngine.BuildEngine,
                Version = "1.2.3"
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);

            Assert.That(task.VersionBase, Is.EqualTo("1.2.3"));
            Assert.That(task.VersionSuffix, Is.Empty);
            Assert.That(task.VersionMeta, Is.Empty);
        }

        [Test]
        public void GetSemVerConstituentsWithBuildZero()
        {
            BuildEngineMock buildEngine = new();
            SemVer task = new() {
                BuildEngine = buildEngine.BuildEngine,
                Version = "1.2.3.0"
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);

            Assert.That(task.VersionBase, Is.EqualTo("1.2.3"));
            Assert.That(task.VersionSuffix, Is.Empty);
            Assert.That(task.VersionMeta, Is.Empty);
        }

        [Test]
        public void GetSemVerConstituentsWithPreRelease()
        {
            BuildEngineMock buildEngine = new();
            SemVer task = new() {
                BuildEngine = buildEngine.BuildEngine,
                Version = "1.2.3-Preview.20210903"
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);

            Assert.That(task.VersionBase, Is.EqualTo("1.2.3"));
            Assert.That(task.VersionSuffix, Is.EqualTo("Preview.20210903"));
            Assert.That(task.VersionMeta, Is.Empty);
        }

        [Test]
        public void GetSemVerConstituentsWithPreReleaseAndMeta()
        {
            BuildEngineMock buildEngine = new();
            SemVer task = new() {
                BuildEngine = buildEngine.BuildEngine,
                Version = "1.2.3-Preview.20210903+g1234567"
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);

            Assert.That(task.VersionBase, Is.EqualTo("1.2.3"));
            Assert.That(task.VersionSuffix, Is.EqualTo("Preview.20210903"));
            Assert.That(task.VersionMeta, Is.EqualTo("g1234567"));
        }

        [Test]
        public void GetSemVerConstituentsWithMeta()
        {
            BuildEngineMock buildEngine = new();
            SemVer task = new() {
                BuildEngine = buildEngine.BuildEngine,
                Version = "1.2.3+g1234567"
            };

            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);

            Assert.That(task.VersionBase, Is.EqualTo("1.2.3"));
            Assert.That(task.VersionSuffix, Is.Empty);
            Assert.That(task.VersionMeta, Is.EqualTo("g1234567"));
        }
    }
}
