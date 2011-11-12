namespace Nyaml.Tests
{
    using System;
    using System.Collections;
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
            var loader = new Loader(new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.Read));
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
            var events1 = Yaml.Parse(new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();
            var events2 = Yaml.CanonicalParse(new FileStream(canonicalFile, FileMode.Open)).ToList();
            this.CompareEvents(events1, events2);
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestCanonical")]
        public void TestParserOnCanonical(string canonicalFile)
        {
            var events1 = Yaml.Parse(new FileStream(canonicalFile, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();
            var events2 = Yaml.CanonicalParse(new FileStream(canonicalFile, FileMode.Open)).ToList();
            this.CompareEvents(events1, events2);
        }

        private void CompareNodes(Nodes.Base node1, Nodes.Base node2)
        {
            Assert.That(node1, Is.TypeOf(node2.GetType()));
            Assert.That(node1.Tag, Is.EqualTo(node2.Tag));
            var sca = node1 as Nodes.Scalar;
            if (sca != null)
            {
                Assert.That(sca.Content, Is.EqualTo(((Nodes.Scalar) node2).Content));
                return;
            }
            var seq = node1 as Nodes.Sequence;
            if (seq != null)
            {
                foreach (var p in seq.Content.Zip(((Nodes.Sequence)node2).Content, Tuple.Create))
                    this.CompareNodes(p.Item1, p.Item2);
                return;
            }
            var map = node1 as Nodes.Mapping;
            if (map == null) 
                return;
            foreach (var p in map.Content.Zip(((Nodes.Mapping)node2).Content, Tuple.Create))
            {
                this.CompareNodes(p.Item1.Key, p.Item2.Key);
                this.CompareNodes(p.Item1.Value, p.Item2.Value);
            }
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestDataAndCanonical")]
        public void TestComposer(string dataFile, string canonicalFile)
        {
            var file1 = new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var file2 = new FileStream(canonicalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var nodes1 = Yaml.ComposeAll(file1).ToList();
            var nodes2 = Yaml.CanonicalComposeAll(file2).ToList();

            Assert.That(nodes1.Count, Is.EqualTo(nodes2.Count));
            foreach (var pair in nodes1.Zip(nodes2, Tuple.Create))
                this.CompareNodes(pair.Item1, pair.Item2);

            file1.Close();
            file2.Close();
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestDataAndCanonical")]
        public void TestConstructor(string dataFile, string canonicalFile)
        {
            var file1 = new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var file2 = new FileStream(canonicalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var obj1 = Yaml.LoadAll(file1);
            var obj2 = Yaml.LoadAll(file2);

            Assert.That(obj1, Is.EqualTo(obj2));

            file1.Close();
            file2.Close();
        }
    }
}
