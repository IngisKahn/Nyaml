namespace Nyaml.Tests
{
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class CanonicalTests
    {
        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestCanonical")]
        public void TestCanonicalScanner(string canonicalFile)
        {
            var file = new FileStream(canonicalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var data = new StreamReader(file).ReadToEnd();
            var tokens = Yaml.CanonicalScan(data).ToList();
            Assert.That(tokens, Is.Not.Null);
            file.Close();
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestCanonical")]
        public void TestCanonicalParser(string canonicalFile)
        {
            var file = new FileStream(canonicalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var data = new StreamReader(file).ReadToEnd();
            var events = Yaml.CanonicalParse(data).ToList();
            Assert.That(events, Is.Not.Null);
            file.Close();
        }

        //[Test]
        //[TestCaseSource(typeof(TestFileProvider), "TestDataAndCanonicalNotEmpty")]
        //public void TestCanonicalError(string dataFile, string canonicalFile)
        //{
        //    var file = new FileStream(canonicalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //    var data = new StreamReader(file).ReadToEnd();
        //    var tokens = Yaml.CanonicalScan(data).ToList();
        //    Assert.That(tokens, Is.Not.Null);
        //    file.Close();
        //}
    }
}
