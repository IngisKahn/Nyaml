namespace Nyaml.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

        private void CompareEvents(List<Events.Base> events1, List<Events.Base> events2, bool full = false)
        {
            Assert.That(events1.Count, Is.EqualTo(events2.Count));
            Assert.That(events1.Zip(events2, (e1, e2) => CompareEvent(e1, e2, full)).Aggregate(true, (a, b) => a & b), Is.True);
        }

        private bool CompareEvent(Events.Base event1, Events.Base event2, bool full)
        {
            Assert.That(event1, Is.TypeOf(event2.GetType()));
            var alias = event1 as Events.Alias;
            if (alias != null && full)
                Assert.That(alias.Anchor, Is.EqualTo(((Events.Alias)event2).Anchor));
            var col = event1 as Events.CollectionStart;
            if (col != null)
            {
                var tag1 = col.Tag;
                var tag2 = ((Events.CollectionStart) event2).Tag;
                if ((tag1 != null && tag1 != "!" && tag2 != null && tag2 != "!")
                    || full)
                Assert.That(tag1, Is.EqualTo(tag2));
            }
            else
            {
                var sca = event1 as Events.Scalar;
                if (sca != null)
                {
                    var tag1 = sca.Tag;
                    var tag2 = ((Events.Scalar)event2).Tag;
                    if ((tag1 != null && tag1 != "!" && tag2 != null && tag2 != "!")
                        || full)
                        Assert.That(tag1, Is.EqualTo(tag2));
                    Assert.That(sca.Value, Is.EqualTo(((Events.Scalar)event2).Value));
                }
            }
            return true;
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestDataAndCanonical")]
        public void TestParser(string dataFile, string canonicalFile)
        {
            var events1 = Yaml.Parse(new FileStream(dataFile, FileMode.Open)).ToList();
            var events2 = Yaml.CanonicalParse(new FileStream(canonicalFile, FileMode.Open)).ToList();
            this.CompareEvents(events1, events2);
        }
    }
}
