namespace RJCP.MSBuildTasks
{
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using Infrastructure.Tools;
    using NUnit.Framework;
    using RJCP.CodeQuality.NUnitExtensions;

    [TestFixture]
    public class X509SignAuthenticodeTest
    {
        private static readonly string TestCert = Path.Combine(Deploy.TestDirectory, "TestResource", "02EAAE_CodeSign.crt");
        private static readonly string SignArtifact = Path.Combine(Deploy.TestDirectory, "TestResource", "InputSignArtifact.txt");
        private static readonly string InexistentArtifact = Path.Combine(Deploy.TestDirectory, "TestResource", "FileNotFound.txt");

        private static TestToolFactory InitToolFactory()
        {
            TestToolFactory factory = new TestToolFactory();
            ToolFactory.Instance = factory;
            return factory;
        }

        [TestCase(true, TestName = "ExecuteSignDefault")]
        [TestCase(false, TestName = "SignToolNotFound")]
        public void ExecuteSignDefault(bool available)
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.SignToolAvailable = available;
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = SignArtifact
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.EqualTo(available));
        }

        [Test]
        public void ExecuteSignDefineStoreDefault()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = SignArtifact,
                CertificateLocation = "CurrentUser",
                CertificateStoreName = "My"
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ExecuteSignStoreRoot()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedStoreName = StoreName.Root;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = SignArtifact,
                CertificateStoreName = "Root"
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ExecuteSignStoreLocalMachine()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedStoreLocation = StoreLocation.LocalMachine;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = SignArtifact,
                CertificateLocation = "LocalMachine"
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ExecuteSignDefineStoreOther()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedStoreLocation = StoreLocation.LocalMachine;
                signTool.ExpectedStoreName = StoreName.Root;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = SignArtifact,
                CertificateLocation = "LocalMachine",
                CertificateStoreName = "Root"
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ExecuteSignUknownThumbprint()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "SignToolThumbPrintUnknown";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = SignArtifact
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }

        [Test]
        public void CertPathEmpty()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = "",
                InputAssembly = SignArtifact
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }

        [Test]
        public void CertPathNotFound()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = InexistentArtifact,
                InputAssembly = SignArtifact
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }

        [Test]
        public void CertPathInputAssemblyEmpty()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = string.Empty
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }

        [Test]
        public void CertPathInputAssemblyNotFound()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = InexistentArtifact
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.False);
        }

        [Test]
        public void ExecuteSignWithTimeStampUri()
        {
            BuildEngineMock buildEngine = new BuildEngineMock();
            SignToolMock signTool;
            TestToolFactory factory = InitToolFactory();
            factory.ToolCreatedEvent += (s, e) => {
                signTool = (SignToolMock)e.Tool;
                signTool.ExpectedThumbPrint = "2fda16f7adf7153e17d4bf3d36adc514a736cdf4";
                signTool.ExpectedTimeStampUri = "http://localhost/";
            };

            X509SignAuthenticode task = new X509SignAuthenticode {
                BuildEngine = buildEngine.BuildEngine,
                CertPath = TestCert,
                InputAssembly = SignArtifact,
                TimeStampUri = "http://localhost"
            };
            bool result = task.Execute();
            buildEngine.DumpErrorEvents();
            Assert.That(result, Is.True);
        }
    }
}
