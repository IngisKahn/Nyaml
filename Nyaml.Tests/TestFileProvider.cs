namespace Nyaml.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    
    public static class TestFileProvider
    {
        public static IEnumerable<string> TestDocuments
        {
            get
            {
                return Directory.EnumerateFiles("Data");
            }
        }

        public static IEnumerable<string> TestMarks
        {
            get
            {
                return Directory.EnumerateFiles("Data").Where(f => f.EndsWith(".marks"));
            }
        }

        public static IEnumerable<string> TestStreamError
        {
            get
            {
                return Directory.EnumerateFiles("Data").Where(f => f.EndsWith(".stream-error"));
            }
        }
    }
}
