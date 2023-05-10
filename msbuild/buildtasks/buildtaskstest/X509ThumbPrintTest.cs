namespace RJCP.MSBuildTasks
{
    using System.IO;
    using NUnit.Framework;
    using RJCP.CodeQuality.NUnitExtensions;

    [TestFixture]
    public class X509ThumbPrintTest
    {
        private static readonly string TestCert = Path.Combine(Deploy.TestDirectory, "TestResource", "02EAAE_CodeSign.crt");
        private static readonly string InvalidCert = Path.Combine(Deploy.TestDirectory, "TestResource", "InvalidCert.crt");

        [Test]
        public void ExecuteNoCert()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();

            X509ThumbPrint task = new X509ThumbPrint {
                BuildEngine = buildEngine.BuildEngine
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);                                        // It failed
            Assert.That(task.ThumbPrint, Is.Empty);
            Assert.That(buildEngine.BuildErrorEventArgs, Has.Count.EqualTo(1));    // And it logged an error message
            Assert.That(buildEngine.BuildErrorEventArgs[0].Message, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ExecuteEmptyCert()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();

            X509ThumbPrint task = new X509ThumbPrint {
                CertPath = "",
                BuildEngine = buildEngine.BuildEngine
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);                                        // It failed
            Assert.That(task.ThumbPrint, Is.Empty);
            Assert.That(buildEngine.BuildErrorEventArgs, Has.Count.EqualTo(1));    // And it logged an error message
            Assert.That(buildEngine.BuildErrorEventArgs[0].Message, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ExecuteWhiteSpaceCert()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();

            X509ThumbPrint task = new X509ThumbPrint {
                CertPath = " ",
                BuildEngine = buildEngine.BuildEngine
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);                                        // It failed
            Assert.That(task.ThumbPrint, Is.Empty);
            Assert.That(buildEngine.BuildErrorEventArgs, Has.Count.EqualTo(1));    // And it logged an error message
            Assert.That(buildEngine.BuildErrorEventArgs[0].Message, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ExecuteCertNotFound()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();

            X509ThumbPrint task = new X509ThumbPrint {
                CertPath = @"C:\foo\nocert.crt",
                BuildEngine = buildEngine.BuildEngine
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);                                        // It failed
            Assert.That(task.ThumbPrint, Is.Empty);
            Assert.That(buildEngine.BuildErrorEventArgs, Has.Count.EqualTo(1));    // And it logged an error message
            Assert.That(buildEngine.BuildErrorEventArgs[0].Message, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ExecuteInvalidCertificate()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();

            X509ThumbPrint task = new X509ThumbPrint {
                CertPath = InvalidCert,
                BuildEngine = buildEngine.BuildEngine
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);                                        // It failed
            Assert.That(task.ThumbPrint, Is.Empty);
            Assert.That(buildEngine.BuildErrorEventArgs, Has.Count.EqualTo(1));    // And it logged an error message
            Assert.That(buildEngine.BuildErrorEventArgs[0].Message, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void ExecuteValidCertificate()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();

            X509ThumbPrint task = new X509ThumbPrint {
                CertPath = TestCert,
                BuildEngine = buildEngine.BuildEngine
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);                                        // It failed
            Assert.That(task.ThumbPrint, Is.EqualTo("2FDA16F7ADF7153E17D4BF3D36ADC514A736CDF4"));
            Assert.That(buildEngine.BuildErrorEventArgs, Is.Empty);   // And no error was logged
        }
    }
}
