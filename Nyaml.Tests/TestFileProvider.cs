﻿namespace Nyaml.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    
    public static class TestFileProvider
    {
        private static IEnumerable<KeyValuePair<string, HashSet<string>>> TestDocuments
        {
            get
            {
                var fileNames = Directory.EnumerateFiles("Data")
                    .Select(f => Tuple.Create(Path.GetFileNameWithoutExtension(f),
                                              Path.GetExtension(f)));
                var result = new Dictionary<string, HashSet<string>>();
                foreach (var fileName in fileNames)
                {
                    HashSet<string> list;
                    if (!result.TryGetValue(fileName.Item1, out list))
                        result[fileName.Item1] = list = new HashSet<string>();
                    list.Add(fileName.Item2);
                }
                return result;
            }
        }

        private static IEnumerable<object[]> GetFilesPerTest(IList<string> tests, IList<string> skips = null)
        {
            var files = TestDocuments;
            if (tests == null)
            {
                yield return new object[0];
                yield break;
            }
            if (skips == null)
                skips = new string[0];
            foreach (var file in files)
            {
                var filenames = new List<string>();
                var ok = false;
                foreach (var ext in tests)
                {
                    if (!file.Value.Contains(ext))
                    {
                        ok = false;
                        break;
                    }
                    filenames.Add(Path.Combine("Data", file.Key + ext));
                    ok = true;
                }
                if (ok && !skips.Any(skip => file.Value.Contains(skip)))
                    yield return filenames.Cast<object>().ToArray();
            }
        }

        public static IEnumerable<object[]> TestMarks
        {
            get
            {
                return GetFilesPerTest(new[]{".marks"});
            }
        }

        public static IEnumerable<object[]> TestStreamError
        {
            get
            {
                return GetFilesPerTest(new[] { ".stream-error" });
            }
        }

        public static IEnumerable<object[]> TestLoaderError
        {
            get
            {
                return GetFilesPerTest(new[] { ".loader-error" });
            }
        }

        public static IEnumerable<object[]> TestLoaderErrorSingle
        {
            get
            {
                return GetFilesPerTest(new[] { ".single-loader-error" });
            }
        }

        public static IEnumerable<object[]> TestDataAndDetect
        {
            get { return GetFilesPerTest(new[] { ".data", ".detect" }); }
        }

        public static IEnumerable<object[]> TestDataAndCanonical
        {
            get { return GetFilesPerTest(new[] { ".data", ".canonical" }); }
        }

        public static IEnumerable<object[]> TestDataAndCanonicalNotEmpty
        {
            get { return GetFilesPerTest(new[] { ".data", ".canonical" }, new[] { ".empty" }); }
        }

        public static IEnumerable<object[]> TestCanonical
        {
            get { return GetFilesPerTest(new[] { ".canonical" }); }
        }

        public static IEnumerable<object[]> TestDataAndTokens
        {
            get { return GetFilesPerTest(new[] { ".data", ".tokens" }); }
        }

        public static IEnumerable<object[]> TestDataAndStructure
        {
            get { return GetFilesPerTest(new[] { ".data", ".structure" }); }
        }
    }
}
