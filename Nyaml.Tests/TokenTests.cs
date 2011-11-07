namespace Nyaml.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    class TokenTests
    {
        private static readonly Dictionary<Type, string> replaces = 
            new Dictionary<Type, string>
            {
                { typeof(Tokens.Directive), "%" },
                { typeof(Tokens.DocumentStart), "---" },
                { typeof(Tokens.DocumentEnd), "..." },
                { typeof(Tokens.Alias), "*" },
                { typeof(Tokens.Anchor), "&" },
                { typeof(Tokens.Tag), "!" },
                { typeof(Tokens.Scalar), "_" },
                { typeof(Tokens.BlockSequenceStart), "[[" },
                { typeof(Tokens.BlockMappingStart), "{{" },
                { typeof(Tokens.BlockEnd), "]}" },
                { typeof(Tokens.FlowSequenceStart), "[" },
                { typeof(Tokens.FlowMappingStart), "{" },
                { typeof(Tokens.FlowSequenceEnd), "]" },
                { typeof(Tokens.FlowMappingEnd), "}" },
                { typeof(Tokens.BlockEntry), "," },
                { typeof(Tokens.FlowEntry), "," },
                { typeof(Tokens.Key), "?" },
                { typeof(Tokens.Value), ":" },
            };
        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestDataAndCanonical")]
        public void TestScanner(string dataFile, string canonFile)
        {
            var s = new Scanner(new Reader(new FileStream(dataFile, FileMode.Open)));
            var tokens1 = new List<Tokens.Base>();
            while(s.CheckToken()) 
                tokens1.Add(s.GetToken());
            s = new Scanner(new Reader(new FileStream(canonFile, FileMode.Open)));
            var tokens2 = new List<Tokens.Base>();
            while (s.CheckToken()) 
                tokens2.Add(s.GetToken());
        }
    }
}
