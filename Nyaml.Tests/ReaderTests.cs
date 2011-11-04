namespace Nyaml.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using NUnit.Framework;

    [TestFixture]
    public class ReaderTests
    {
        private static void RunReader(dynamic data)
        {
            Assert.Throws<Reader.Error>(() =>
                                       {
                                           var reader = new Reader(data);
                                           while (reader.Peek() != '\0')
                                               reader.Forward();
                                       });
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestStreamError")]
        public void TestStreamError(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                RunReader(stream);
                stream.Seek(0, SeekOrigin.Begin);
                RunReader(new BinaryReader(stream).ReadBytes((int)stream.Length));
                stream.Seek(0, SeekOrigin.Begin);
                //Encoding encoding = Encoding.ASCII;
                //string data = null;
                //foreach (var e in new[] {Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode})
                //{
                //    encoding = e;
                //    data = e.GetString(new BinaryReader(stream).ReadBytes((int) stream.Length));
                //    if (!string.IsNullOrEmpty(data))
                //        break;
                //}
                //this.RunReader(data);
                //stream.Seek(0, SeekOrigin.Begin);
                //this.RunReader(new StreamReader(stream, encoding));
            }
        }
    }
}
