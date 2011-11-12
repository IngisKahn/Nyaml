namespace Nyaml.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class Pairs : Sequence<EquatableList<Tuple<object, object>>, IEnumerable<Tuple<object, object>>>
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

        protected override EquatableList<Tuple<object, object>> Construct(Nodes.Base node, Constructor constructor)
        {
            return new EquatableList<Tuple<object, object>>(((Nodes.Sequence)node).Content
                .Select(item => ((Nodes.Mapping) item).Content.First())
                .Select(kvp => Tuple.Create(constructor.ConstructObject(kvp.Key), constructor.ConstructObject(kvp.Value)))
                .ToList());
        }

        public override Nodes.Base Represent(IEnumerable<Tuple<object, object>> value, Representer representer)
        {
            var result = new Nodes.Sequence { SequenceTag = this };
            var list = result.Content;   
            foreach (var item in value)
            {
                //var d = new Dictionary<Nodes.Base, Nodes.Base>(1);
                var m = new Nodes.Mapping { MappingTag = new Mapping() };
                list.Add(m);
                m.Content.Add((Nodes.Base)item.Item1, (Nodes.Base)item.Item2);
            }
            return result;
        }
    }
}