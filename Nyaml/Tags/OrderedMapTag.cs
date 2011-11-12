namespace Nyaml.Tags
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class OrderedMap : Sequence<EquatableList<IDictionary>, IDictionary>
    {
        internal OrderedMap() : base("tag:yaml.org,2002:omap")
        { }

        public override bool Validate(Nodes.Sequence node)
        {
            if (node == null)
                return false;

            var set = new HashSet<int>();
            foreach (var map in node.Content.Select(content => content as Nodes.Mapping))
            {
                if (map == null || map.Content.Count != 1)
                    return false;
                var hash = map.Content.First().Key.GetHashCode();
                if (!set.Add(hash))
                    return false;
            }

            return true;
        }

        protected override EquatableList<IDictionary> Construct(Nodes.Base node, Constructor constructor)
        {
            var content = ((Nodes.Sequence)node).Content;
            var om = new EquatableList<IDictionary>();
            om.AddRange(content.OfType<Nodes.Mapping>()
                .Select(m => m.Content.First())
                .Select(e => new Hashtable { { constructor.ConstructObject(e.Key), constructor.ConstructObject(e.Value) } }));
            return om;
        }

        public override Nodes.Base Represent(IDictionary value, Representer representer)
        {
            var content = new List<Nodes.Base>();
            var result = new Nodes.Sequence { SequenceTag = this };
            foreach (var n in content)
                result.Content.Add(n);
            foreach (var key in value.Keys)
            {
                var m = new Nodes.Mapping { MappingTag = new Mapping() };
                //m.Content.Add((Nodes.Base)e.Key, (Nodes.Base)e.Value);
                content.Add(m);
            }
            return result;
        }
    }
}
