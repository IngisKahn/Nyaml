namespace Nyaml.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
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
                Assert.Catch<Exception>(() => Yaml.LoadAll(new StreamReader(file, Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback())).ReadToEnd()).ToArray());
            }
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestLoaderErrorSingle")]
        public void TestLoaderErrorSingle(string errorFile)
        {
            using (var file = new FileStream(errorFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Assert.Catch<YamlError>(() => Yaml.Load(new StreamReader(file).ReadToEnd()));
            }
        }
    }
}
