namespace RJCP.MSBuildTasks.Infrastructure
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NUnit.Framework;

    [SuppressMessage("Assertion", "NUnit2010:Use EqualConstraint for better assertion messages in case of failure", Justification = "Direct testing of operators")]
    [SuppressMessage("Assertion", "NUnit2043:Use ComparisonConstraint for better assertion messages in case of failure", Justification = "Direct testing of operators")]
    [TestFixture]
    public class SemVer2Test
    {
        [TestCase("0.1.0", 0, 1, 0)]
        [TestCase("1.0.0", 1, 0, 0)]
        [TestCase("1.1.2", 1, 1, 2)]

        public void BasicSemVer(string version, int major, int minor, int patch)
        {
            SemVer2 v = new(version);
            Assert.That(v.Major, Is.EqualTo(major));
            Assert.That(v.Minor, Is.EqualTo(minor));
            Assert.That(v.Patch, Is.EqualTo(patch));
            Assert.That(v.HasBuild, Is.False);
            Assert.That(v.PreRelease, Is.EqualTo(string.Empty));
            Assert.That(v.MetaData, Is.EqualTo(string.Empty));
            Assert.That(v.IsPreRelease, Is.False);
            Assert.That(v.HasMetaData, Is.False);
        }

        [TestCase("0.1.0.0", 0, 1, 0, 0)]
        [TestCase("1.0.0.0", 1, 0, 0, 0)]
        [TestCase("1.1.2.234", 1, 1, 2, 234)]
        public void SemVerWithBuild(string version, int major, int minor, int patch, int build)
        {
            SemVer2 v = new(version);
            Assert.That(v.Major, Is.EqualTo(major));
            Assert.That(v.Minor, Is.EqualTo(minor));
            Assert.That(v.Patch, Is.EqualTo(patch));
            Assert.That(v.HasBuild, Is.True);
            Assert.That(v.Build, Is.EqualTo(build));
            Assert.That(v.PreRelease, Is.EqualTo(string.Empty));
            Assert.That(v.MetaData, Is.EqualTo(string.Empty));
            Assert.That(v.IsPreRelease, Is.False);
            Assert.That(v.HasMetaData, Is.False);
        }

        [TestCase("0.1.0-ci-20150505", 0, 1, 0, "ci-20150505")]
        [TestCase("1.0.0-alpha", 1, 0, 0, "alpha")]
        [TestCase("1.1.2-beta", 1, 1, 2, "beta")]
        [TestCase("1.0.123-prex.2", 1, 0, 123, "prex.2")]
        public void SemVerWithPreRelease(string version, int major, int minor, int patch, string preRelease)
        {
            SemVer2 v = new(version);
            Assert.That(v.Major, Is.EqualTo(major));
            Assert.That(v.Minor, Is.EqualTo(minor));
            Assert.That(v.Patch, Is.EqualTo(patch));
            Assert.That(v.HasBuild, Is.False);
            Assert.That(v.PreRelease, Is.EqualTo(preRelease));
            Assert.That(v.MetaData, Is.EqualTo(string.Empty));
            Assert.That(v.IsPreRelease, Is.True);
            Assert.That(v.HasMetaData, Is.False);
        }

        [TestCase("0.1.0.0-ci-20150505", 0, 1, 0, 0, "ci-20150505")]
        [TestCase("1.0.0.0-alpha", 1, 0, 0, 0, "alpha")]
        [TestCase("1.1.2.234-beta", 1, 1, 2, 234, "beta")]
        public void SemVerWithBuildPreRelease(string version, int major, int minor, int patch, int build, string preRelease)
        {
            SemVer2 v = new(version);
            Assert.That(v.Major, Is.EqualTo(major));
            Assert.That(v.Minor, Is.EqualTo(minor));
            Assert.That(v.Patch, Is.EqualTo(patch));
            Assert.That(v.HasBuild, Is.True);
            Assert.That(v.Build, Is.EqualTo(build));
            Assert.That(v.PreRelease, Is.EqualTo(preRelease));
            Assert.That(v.MetaData, Is.EqualTo(string.Empty));
            Assert.That(v.IsPreRelease, Is.True);
            Assert.That(v.HasMetaData, Is.False);
        }

        [Test]
        public void SemVerWithNullPreRelease()
        {
            SemVer2 v = new(1, 0, 0, 0, null);
            Assert.That(v.ToString(), Is.EqualTo("1.0.0.0"));
            Assert.That(v.Major, Is.EqualTo(1));
            Assert.That(v.Minor, Is.EqualTo(0));
            Assert.That(v.Patch, Is.EqualTo(0));
            Assert.That(v.Build, Is.EqualTo(0));
            Assert.That(v.PreRelease, Is.EqualTo(string.Empty));
            Assert.That(v.IsPreRelease, Is.False);
            Assert.That(v.MetaData, Is.EqualTo(string.Empty));
            Assert.That(v.HasMetaData, Is.False);
        }

        [Test]
        public void SemVerWithNullPreReleaseAndMetaData()
        {
            SemVer2 v = new(1, 0, 0, 0, null, "meta");
            Assert.That(v.ToString(), Is.EqualTo("1.0.0.0+meta"));
            Assert.That(v.Major, Is.EqualTo(1));
            Assert.That(v.Minor, Is.EqualTo(0));
            Assert.That(v.Patch, Is.EqualTo(0));
            Assert.That(v.Build, Is.EqualTo(0));
            Assert.That(v.PreRelease, Is.EqualTo(string.Empty));
            Assert.That(v.IsPreRelease, Is.False);
            Assert.That(v.MetaData, Is.EqualTo("meta"));
            Assert.That(v.HasMetaData, Is.True);
        }

        [TestCase("0.1.0+ci-20150505", 0, 1, 0, "ci-20150505")]
        [TestCase("1.0.0+alpha", 1, 0, 0, "alpha")]
        [TestCase("1.1.2+beta.2", 1, 1, 2, "beta.2")]
        public void SemVerWithMetaData(string version, int major, int minor, int patch, string metaData)
        {
            SemVer2 v = new(version);
            Assert.That(v.Major, Is.EqualTo(major));
            Assert.That(v.Minor, Is.EqualTo(minor));
            Assert.That(v.Patch, Is.EqualTo(patch));
            Assert.That(v.HasBuild, Is.False);
            Assert.That(v.PreRelease, Is.EqualTo(string.Empty));
            Assert.That(v.MetaData, Is.EqualTo(metaData));
            Assert.That(v.IsPreRelease, Is.False);
            Assert.That(v.HasMetaData, Is.True);
        }

        [TestCase("0.1.0.0+ci-20150505", 0, 1, 0, 0, "ci-20150505")]
        [TestCase("1.0.0.0+alpha.a5", 1, 0, 0, 0, "alpha.a5")]
        [TestCase("1.1.2.234+beta", 1, 1, 2, 234, "beta")]
        public void SemVerWithBuildMetaData(string version, int major, int minor, int patch, int build, string metaData)
        {
            SemVer2 v = new(version);
            Assert.That(v.Major, Is.EqualTo(major));
            Assert.That(v.Minor, Is.EqualTo(minor));
            Assert.That(v.Patch, Is.EqualTo(patch));
            Assert.That(v.HasBuild, Is.True);
            Assert.That(v.Build, Is.EqualTo(build));
            Assert.That(v.PreRelease, Is.EqualTo(string.Empty));
            Assert.That(v.MetaData, Is.EqualTo(metaData));
            Assert.That(v.IsPreRelease, Is.False);
            Assert.That(v.HasMetaData, Is.True);
        }

        [TestCase("0.1.0.0-ci-20150505+win.7", 0, 1, 0, 0, "ci-20150505", "win.7")]
        [TestCase("1.0.0.0-alpha+win.8", 1, 0, 0, 0, "alpha", "win.8")]
        [TestCase("1.1.2.234-beta+ubuntu", 1, 1, 2, 234, "beta", "ubuntu")]
        public void SemVerWithBuildPreReleaseMetaData(string version, int major, int minor, int patch, int build, string preRelease, string metaData)
        {
            SemVer2 v = new(version);
            Assert.That(v.Major, Is.EqualTo(major));
            Assert.That(v.Minor, Is.EqualTo(minor));
            Assert.That(v.Patch, Is.EqualTo(patch));
            Assert.That(v.HasBuild, Is.True);
            Assert.That(v.Build, Is.EqualTo(build));
            Assert.That(v.PreRelease, Is.EqualTo(preRelease));
            Assert.That(v.MetaData, Is.EqualTo(metaData));
            Assert.That(v.IsPreRelease, Is.True);
            Assert.That(v.HasMetaData, Is.True);
        }

        [TestCase("")]
        [TestCase("1")]                              // Must be X.Y.Z
        [TestCase("1.0")]
        [TestCase("1..123")]
        [TestCase("...123")]
        [TestCase("1.")]
        [TestCase("1..")]
        [TestCase("1...")]
        [TestCase("1.0..")]
        [TestCase("1.0.1.")]
        [TestCase("-1.0.1")]
        [TestCase("1.-0.1")]
        [TestCase("1.0.-1")]
        [TestCase("a1.0.1")]
        [TestCase("1.b.1")]
        [TestCase("1.b1.1")]
        [TestCase("1.1b1.1")]
        [TestCase("1.1.c2")]
        [TestCase("1.1.2.c")]
        [TestCase("1.1.2.99999999999999999999")]
        [TestCase("1.0.1.-65535")]
        [TestCase("1.0-alpha")]                      // Must be X.Y.Z-PRE
        [TestCase("1.0.1.-foo")]
        [TestCase("1.00.0000")]                      // No preceding zeros
        [TestCase("1.01.123")]
        [TestCase("1.0.0123")]
        [TestCase("01.0.123")]
        [TestCase("1.0.123.065535")]
        [TestCase("1.0.123-prex~2")]
        [TestCase("1.0.123-1")]
        public void InvalidVersion(string version)
        {
            Assert.That(() => { _ = new SemVer2(version); }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void InvalidSemVerNull()
        {
            Assert.That(() => { _ = new SemVer2(null); }, Throws.TypeOf<ArgumentNullException>());
        }

        [TestCase("1.0.0.0", "1.0.0.1")]
        [TestCase("1.0.0-alpha", "1.0.0-alpha.1")]
        [TestCase("1.0.0-alpha", "1.0.0-alpha-2")]
        [TestCase("1.0.0-alpha.1", "1.0.0-alpha.beta")]
        [TestCase("1.0.0-alpha.1", "1.0.0-alpha.1beta")]
        [TestCase("1.0.0-alpha.1", "1.0.0-alpha.1-1")]
        [TestCase("1.0.0-alpha.beta", "1.0.0-beta")]
        [TestCase("1.0.0-beta", "1.0.0-beta.2")]
        [TestCase("1.0.0-beta.2", "1.0.0-beta.11")]
        [TestCase("1.0.0-beta.11", "1.0.0-rc.1")]
        [TestCase("1.0.0-rc.1", "1.0.0")]
        [TestCase("1.0.0", "2.0.0")]
        [TestCase("2.0.0", "2.1.0")]
        [TestCase("2.1.0", "2.1.1")]
        public void Compare(string left, string right)
        {
            SemVer2 leftVersion = new(left);
            SemVer2 rightVersion = new(right);

            SemVer2 left2 = leftVersion;
            SemVer2 right2 = rightVersion;

            Assert.That(leftVersion == left2);
            Assert.That(rightVersion == right2);
            Assert.That(leftVersion.CompareTo(leftVersion) == 0);
            Assert.That(rightVersion.CompareTo(rightVersion) == 0);

            Assert.That(leftVersion < rightVersion);
            Assert.That(rightVersion > leftVersion);
            Assert.That(leftVersion != rightVersion);
            Assert.That(leftVersion.GetHashCode() != rightVersion.GetHashCode());

            Assert.That(leftVersion.CompareTo(rightVersion) < 0);
            Assert.That(rightVersion.CompareTo(leftVersion) > 0);
        }

        [Test]
        public void CompareNull()
        {
            SemVer2 vn1 = null;
            SemVer2 vn2 = null;
            SemVer2 vv1 = new("1.0.0");
            SemVer2 vv2 = new("2.0.0");

            Assert.That(vn1 == vn2);
            Assert.That(vn1 <= vn2);
            Assert.That(vn1 >= vn2);

            Assert.That(vn1 < vv1);
            Assert.That(vn1 <= vv1);
            Assert.That(vv2 > vn2);
            Assert.That(vv2 >= vn2);
        }

        [TestCase("1.0.0.0", "1.0.0.0")]
        [TestCase("1.0.0", "1.0.0.0")]
        [TestCase("1.0.0-pre1+foo.1", "1.0.0-pre1+foo.2")]
        public void CompareEqual(string left, string right)
        {
            SemVer2 leftVersion = new(left);
            SemVer2 rightVersion = new(right);

            Assert.That(leftVersion == new SemVer2(left));
            Assert.That(rightVersion == new SemVer2(right));
            Assert.That(leftVersion.CompareTo(leftVersion) == 0);
            Assert.That(rightVersion.CompareTo(rightVersion) == 0);

            Assert.That(leftVersion == rightVersion);
            Assert.That(leftVersion.Equals(rightVersion));
            Assert.That(rightVersion.Equals(leftVersion));
            Assert.That(leftVersion.GetHashCode() == rightVersion.GetHashCode());

            Assert.That(leftVersion.CompareTo(rightVersion) == 0);
            Assert.That(rightVersion.CompareTo(leftVersion) == 0);
        }

        [Test]
        public void CompareSemVer()
        {
            SemVer2 left = new("0.4.0-alpha");
            SemVer2 right = new(0, 4, 0, 0, "alpha");

            Assert.That(left, Is.EqualTo(right));
        }
    }
}
