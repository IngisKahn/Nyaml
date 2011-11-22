namespace Nyaml.Tests
{
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    public class TagTests
    {
        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestDataAndDetect")]
        public void TestImplicitResolver(string dataFile, string detectFile)
        {
            var reader = new StreamReader(detectFile);
            var correctTag = reader.ReadToEnd().Trim();
            reader.Dispose();
            var file = new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var node = Yaml.Compose(file);
            file.Dispose();
            Assert.That(node, Is.TypeOf<Nodes.Sequence>());
            foreach (var sub in ((Nodes.Sequence)node).Content)
            {
                Assert.That(sub, Is.TypeOf<Nodes.Scalar>());
                Assert.That(((Nodes.Scalar)sub).Tag.Name, Is.EqualTo(correctTag));
            }
        }
    }
}
