namespace Nyaml.Tests
{
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class ErrorTests
    {
        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestLoaderError")]
        public void TestLoaderError(string errorFile)
        {
            using (var file = new FileStream(errorFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Assert.Catch<YamlError>(() => Yaml.LoadAll(file).ToArray());
            }
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestLoaderError")]
        public void TestLoaderErrorString(string errorFile)
        {
            using (var file = new FileStream(errorFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Assert.Catch<YamlError>(() => Yaml.LoadAll(new StreamReader(file).ReadToEnd()).ToArray());
            }
        }
    }
}
