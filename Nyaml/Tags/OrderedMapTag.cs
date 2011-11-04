namespace Nyaml.Tags
{
    using System.Collections.Generic;
    using System.Linq;

    public sealed class OrderedMap : Sequence<OrderedMap<Nodes.Base, Nodes.Base>>
    {
        internal OrderedMap() : base("tag:yaml.org,2002:omap")
        { }

        public override bool Validate(Nodes.Sequence node)
        {
            if (node == null)
                return false;

            var set = new HashSet<int>();
            foreach (var content in node.Content)
            {
                var map = content as Nodes.Mapping;
                if (map == null || map.Content.Count != 1)
                    return false;
                var hash = map.Content.First().Key.GetHashCode();
                if (!set.Add(hash))
                    return false;
            }

            return true;
        }

        protected override OrderedMap<Nodes.Base, Nodes.Base> Construct(Nodes.Base node)
        {
            var content = ((Nodes.Sequence)node).Content;
            var om = new OrderedMap<Nodes.Base, Nodes.Base>();
            foreach (var m in content.OfType<Nodes.Mapping>())
                om.Add(m.Content.First());
            return om;
        }

        public override Nodes.Base Represent(OrderedMap<Nodes.Base, Nodes.Base> value)
        {
            var content = new List<Nodes.Base>();
            var result = new Nodes.Sequence(content) { SequenceTag = this };
            foreach (var kvp in value)
            {
                var d = new Dictionary<Nodes.Base, Nodes.Base>(1);
                d[kvp.Key] = kvp.Value;
                content.Add(new Nodes.Mapping(d) { MappingTag = new Mapping() });
            }
            return result;
        }
    }
}
