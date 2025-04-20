namespace RJCP.MSBuildTasks.Infrastructure.Config
{
    using System.IO;
    using NUnit.Framework;
    using RJCP.CodeQuality.NUnitExtensions;

    [TestFixture]
    public class IniFileTest
    {
        [Test]
        public void LoadValidIniFile()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            IniFile iniFile = new(path);
            Assert.That(iniFile, Has.Count.EqualTo(7));

            Assert.That(iniFile.ContainsKey("section"));
            Assert.That(iniFile["section"], Has.Count.EqualTo(1));
            Assert.That(iniFile["section"]["key"], Is.EqualTo("Value"));

            Assert.That(iniFile.ContainsKey("section2"));
            Assert.That(iniFile["section2"], Has.Count.EqualTo(1));
            Assert.That(iniFile["section2"]["key"], Is.EqualTo("Value"));

            Assert.That(iniFile.ContainsKey("section3"));
            Assert.That(iniFile["section3"], Has.Count.EqualTo(2));
            Assert.That(iniFile["section3"]["key1"], Is.EqualTo("Value1"));
            Assert.That(iniFile["section3"]["key2"], Is.EqualTo("Value2"));

            Assert.That(iniFile.ContainsKey("sectioncomment"));
            Assert.That(iniFile["sectioncomment"], Has.Count.EqualTo(2));
            Assert.That(iniFile["sectioncomment"]["key"], Is.EqualTo("Value"));
            Assert.That(iniFile["sectioncomment"]["key2"], Is.EqualTo("value2"));

            Assert.That(iniFile.ContainsKey("sectioncomment2"));
            Assert.That(iniFile["sectioncomment2"], Has.Count.EqualTo(4));
            Assert.That(iniFile["sectioncomment2"]["key"], Is.EqualTo("Value2"));
            Assert.That(iniFile["sectioncomment2"]["key2"], Is.EqualTo("Value3"));
            Assert.That(iniFile["sectioncomment2"]["EmptyValueKey"], Is.Empty);
            Assert.That(iniFile["sectioncomment2"]["Key3"], Is.Empty);

            Assert.That(iniFile.ContainsKey("sectionquotes"));
            Assert.That(iniFile["sectionquotes"], Has.Count.EqualTo(2));
            Assert.That(iniFile["sectionquotes"]["#key"], Is.EqualTo("#value"));
            Assert.That(iniFile["sectionquotes"]["key2"], Is.EqualTo("\"Value\""));

            Assert.That(iniFile.ContainsKey("SectionType"));
            Assert.That(iniFile["SectionType"], Has.Count.EqualTo(11));
            Assert.That(iniFile["SectionType"]["int"], Is.EqualTo("1234"));
            Assert.That(iniFile["SectionType"]["intneg"], Is.EqualTo("-1234"));
            Assert.That(iniFile["SectionType"]["intrange"], Is.EqualTo("9999999999999999"));
            Assert.That(iniFile["SectionType"]["intbad"], Is.EqualTo("string"));
            Assert.That(iniFile["SectionType"]["long"], Is.EqualTo("1234"));
            Assert.That(iniFile["SectionType"]["longneg"], Is.EqualTo("-1234"));
            Assert.That(iniFile["SectionType"]["longrange"], Is.EqualTo("-9999999999999999999999999999999999999"));
            Assert.That(iniFile["SectionType"]["longbad"], Is.EqualTo("string"));
            Assert.That(iniFile["SectionType"]["bool"], Is.EqualTo("true"));
            Assert.That(iniFile["SectionType"]["bool2"], Is.EqualTo("false"));
            Assert.That(iniFile["SectionType"]["boolbad"], Is.EqualTo("foobar"));
        }

        [Test]
        public void GetKeyExisting()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "section3", "key1", "default"), Is.EqualTo("Value1"));
        }

        [Test]
        public void GetKeyMissingExistingSection()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "section3", "keyfoo", "bar"), Is.EqualTo("bar"));
        }

        [Test]
        public void GetKeyMissingSection()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "sectionfoo", "key", "bar"), Is.EqualTo("bar"));
        }

        [Test]
        public void GetKeyMissingFile()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "TestFooMissing.ini");
            Assert.That(IniFile.GetKey(path, "section", "key", "foobar"), Is.EqualTo("foobar"));
        }

        [Test]
        public void GetSection()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            IniSection section = IniFile.GetSection(path, "section");
            Assert.That(section, Is.Not.Null);
            Assert.That(section, Has.Count.EqualTo(1));
            Assert.That(section["key"], Is.EqualTo("Value"));
        }

        [Test]
        public void GetMissingSection()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            IniSection section = IniFile.GetSection(path, "sectionmissing");
            Assert.That(section, Is.Not.Null);
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void GetSectionMissingFile()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "TestFooMissing.ini");
            IniSection section = IniFile.GetSection(path, "section");
            Assert.That(section, Is.Not.Null);
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void DuplicateSection()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "TestDuplicate.ini");
            IniFile iniFile = new(path);
            Assert.That(iniFile, Has.Count.EqualTo(2));

            Assert.That(iniFile.ContainsKey("Section1"));
            Assert.That(iniFile["Section1"], Has.Count.EqualTo(1));
            Assert.That(iniFile["Section1"]["Key"], Is.EqualTo("Value"));
            Assert.That(iniFile["Section1"].ContainsKey("Key2"), Is.False);

            Assert.That(iniFile.ContainsKey("Section2"));
        }

        [Test]
        public void DuplicateKeys()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "TestDuplicate.ini");
            IniFile iniFile = new(path);
            Assert.That(iniFile, Has.Count.EqualTo(2));

            Assert.That(iniFile.ContainsKey("Section2"));
            Assert.That(iniFile["Section2"], Has.Count.EqualTo(2));
            Assert.That(iniFile["Section2"]["Key1"], Is.EqualTo("Value1"));
            Assert.That(iniFile["Section2"].ContainsKey("Key2"), Is.True);
            Assert.That(iniFile["Section2"]["Key2"], Is.EqualTo("Value2"));
        }

        [Test]
        public void GetKeyInt()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "int", 42), Is.EqualTo(1234));
        }

        [Test]
        public void GetKeyIntNeg()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "intneg", 42), Is.EqualTo(-1234));
        }

        [Test]
        public void GetKeyIntRange()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "intrange", 42), Is.EqualTo(42));
        }

        [Test]
        public void GetKeyIntBad()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "intbad", 42), Is.EqualTo(42));
        }

        [Test]
        public void GetKeyIntMissingKey()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "intmissing", 42), Is.EqualTo(42));
        }

        [Test]
        public void GetKeyLong()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "long", 42), Is.EqualTo(1234));
        }

        [Test]
        public void GetKeyLongNeg()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "longneg", 42), Is.EqualTo(-1234));
        }

        [Test]
        public void GetKeyLongRange()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "longrange", 42), Is.EqualTo(42));
        }

        [Test]
        public void GetKeyLongBad()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "longbad", 42), Is.EqualTo(42));
        }

        [Test]
        public void GetKeyLongMissingKey()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "longmissing", 42), Is.EqualTo(42));
        }

        [Test]
        public void GetKeyBoolTrue()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "bool", false), Is.EqualTo(true));
        }

        [Test]
        public void GetKeyBoolFalse()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "bool2", true), Is.EqualTo(false));
        }

        [Test]
        public void GetKeyBoolDefault()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "boolbad", true), Is.EqualTo(true));
        }

        [Test]
        public void GetKeyBoolMissing()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "Test.ini");
            Assert.That(IniFile.GetKey(path, "SectionType", "boolmissing", true), Is.EqualTo(true));
        }

        [Test]
        public void EmptySection()
        {
            string path = Path.Combine(Deploy.TestDirectory, "TestResource", "Config", "TestEmptySection.ini");
            IniFile iniFile = new(path);
            Assert.That(iniFile, Has.Count.EqualTo(3));

            Assert.That(iniFile.ContainsKey("section"));
            Assert.That(iniFile["section"], Has.Count.EqualTo(1));
            Assert.That(iniFile["section"]["key"], Is.EqualTo("Value"));

            Assert.That(iniFile.ContainsKey(""));
            Assert.That(iniFile[""], Has.Count.EqualTo(1));
            Assert.That(iniFile[""]["key"], Is.EqualTo("Value"));

            Assert.That(iniFile.ContainsKey("section3"));
            Assert.That(iniFile["section3"], Has.Count.EqualTo(3));
            Assert.That(iniFile["section3"]["key1"], Is.EqualTo("Value1"));
            Assert.That(iniFile["section3"]["key2"], Is.EqualTo("Value2"));
            Assert.That(iniFile["section3"][""], Is.Empty);
        }
    }
}
