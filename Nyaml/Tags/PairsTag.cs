namespace Nyaml.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class Pairs : Sequence<List<Tuple<Nodes.Base, Nodes.Base>>>
    {
        internal Pairs() : base("tag:yaml.org,2002:pairs")
        { }

        public override bool Validate(Nodes.Sequence node)
        {
            if (node == null)
                return false;

            return node.Content
                .Select(content => content as Nodes.Mapping)
                .All(map => map != null && map.Content.Count == 1);
        }

        protected override List<Tuple<Nodes.Base, Nodes.Base>> Construct(Nodes.Base node)
        {
            return ((Nodes.Sequence) node).Content
                .Select(item => ((Nodes.Mapping) item).Content.First())
                .Select(kvp => Tuple.Create(kvp.Key, kvp.Value))
                .ToList();
        }

        public override Nodes.Base Represent(List<Tuple<Nodes.Base, Nodes.Base>> value)
        {
            var list = new List<Nodes.Base>(value.Count);
            var result = new Nodes.Sequence(list) { SequenceTag = this };
            foreach (var item in value)
            {
                var d = new Dictionary<Nodes.Base, Nodes.Base>(1);
                list.Add(new Nodes.Mapping(d) { MappingTag = new Mapping() });
                d.Add(item.Item1, item.Item2);
            }
            return result;
        }
    }
}