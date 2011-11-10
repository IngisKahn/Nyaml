namespace Nyaml.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    public class StructureTests
    {
        private static object ConvertStructure(IParser parser)
        {
            if (parser.CheckEvent<Events.Scalar>())
            {
                var scalarEvent = (Events.Scalar)parser.GetEvent();
                return !string.IsNullOrEmpty(scalarEvent.Tag) ||
                       !string.IsNullOrEmpty(scalarEvent.Anchor) ||
                       !string.IsNullOrEmpty(scalarEvent.Value)
                           ? (object) true
                           : null;
            }
            if (parser.CheckEvent<Events.SequenceStart>())
            {
                parser.GetEvent();
                var list = new List<object>();
                while (!parser.CheckEvent<Events.SequenceEnd>())
                    list.Add(ConvertStructure(parser));
                parser.GetEvent();
                return new EquatableList<object>(list);
            }
            if (parser.CheckEvent<Events.MappingStart>())
            {
                parser.GetEvent();
                var map = new List<object>();
                while (!parser.CheckEvent<Events.MappingEnd>())
                {
                    var key = ConvertStructure(parser);
                    var value = ConvertStructure(parser);
                    map.Add(Tuple.Create(key, value));
                }
                parser.GetEvent();
                return new EquatableList<object>(map);
            }
            if (parser.CheckEvent<Events.Alias>())
            {
                parser.GetEvent();
                return "*";
            }
            parser.CheckEvent();
            return "?";
        }
        
        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestDataAndStructure")]
        public void TestStructure(string dataFile, string structureFile)
        {
            var subNodes1 = new List<object>();
            var nodes2 = StructureParser.Parse(new StreamReader(structureFile).ReadToEnd());
            var loader = new Loader(new FileStream(dataFile, FileMode.Open));
            while (loader.CheckEvent())
            {
                if (loader.CheckEvent<Events.StreamStart, Events.StreamEnd,
                    Events.DocumentStart, Events.DocumentEnd>())
                {
                    loader.GetEvent();
                    continue;
                }
                subNodes1.Add(ConvertStructure(loader));
            }
            var nodes1 = subNodes1.Count == 1 
                                ? subNodes1[0] 
                                : new EquatableList<object>(subNodes1);
            Assert.That(nodes1, Is.EqualTo(nodes2));
        }
    }
}
