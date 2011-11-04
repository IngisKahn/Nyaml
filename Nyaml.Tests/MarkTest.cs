namespace Nyaml.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;
    
    [TestFixture]
    public class MarkTest
    {
        [TestCase]
        [TestCaseSource(typeof(TestFileProvider), "TestMarks")]
        public void TestMarks( string fileName)
        {
            var inputs = new StreamReader(fileName).ReadToEnd().Split(new[] {"---\n"}, StringSplitOptions.None);
            foreach (var input in inputs.Skip(1))
            {
                var index = 0;
                var line = 0;
                var column = 0;
                while (index < input.Length && input[index] != '*')
                    if (input[index++] == '\n')
                    {
                        line++;
                        column = 0;
                    }
                    else
                        column++;
                var mark = new Mark
                               {
                                   Name = fileName,
                                   Index = index,
                                   Line = line,
                                   Column = column,
                                   Buffer = new StringBuilder(input),
                                   Pointer = index
                               };
                var snippet = mark.GetSnippet(2, 79);
                var lineCount = snippet.Count(c => c == '\n');
                Assert.AreEqual(lineCount, 1);
                var lines = snippet.Split('\n');
                Assert.Less(lines[0].Length, 82);
                Assert.AreEqual(lines[0][lines[1].Length - 1], '*');
            }
        }
    }
}
