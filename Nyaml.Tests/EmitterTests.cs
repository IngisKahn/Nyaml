namespace Nyaml.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class EmitterTests
    {
        private static void CompareEvents(IList<Events.Base> events1, IList<Events.Base> events2)
        {
            Assert.That(events1.Count, Is.EqualTo(events2.Count));
            foreach (var pair in events1.Zip(events2, Tuple.Create))
            {
                Assert.That(pair.Item1, Is.TypeOf(pair.Item2.GetType()));
                var nodeEvent1 = pair.Item1 as Events.Node;
                if (nodeEvent1 == null)
                    continue;
                var nodeEvent2 = (Events.Node) pair.Item2;
                Assert.That(nodeEvent1.Anchor, Is.EqualTo(nodeEvent2.Anchor));
                var cstart1 = nodeEvent1 as Events.CollectionStart;
                if (cstart1 != null)
                {
                    Assert.That(cstart1.Tag, Is.EqualTo(((Events.CollectionStart)nodeEvent2).Tag));
                    continue;
                }
                var scalar1 = nodeEvent1 as Events.Scalar;
                if (scalar1 == null)
                    continue;
                var scalar2 = (Events.Scalar) nodeEvent2;
                if (scalar1.ImplicitLevel != ScalarImplicitLevel.Plain &&
                    scalar2.ImplicitLevel != ScalarImplicitLevel.Plain)
                    Assert.That(scalar1.Tag, Is.EqualTo(scalar2.Tag));
                Assert.That(scalar1.Value, Is.EqualTo(scalar2.Value));
            }
        }

        [Test]
        [TestCaseSource(typeof(TestFileProvider), "TestDataAndCanonical")]
        public void TestEmitterOnData(string dataFile, string canonicalFile)
        {
            var file = new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var events = Yaml.Parse(file).ToList();
            var output = Yaml.Emit(events);
            var newEvents = Yaml.Parse(output).ToList();
            CompareEvents(newEvents, events);
            file.Close();
        }
    }
}
