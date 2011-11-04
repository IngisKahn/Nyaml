namespace Nyaml.Tags
{
    using System.Collections.Generic;
    using System.Linq;

    public sealed class Set : Mapping<HashSet<Nodes.Base>>
    {
        internal Set() : base("tag:yaml.org,2002:set")
        { }

        public override bool Validate(Nodes.Mapping node)
        {
            if (node == null)
                return false;

            return node.Content.Values.All(v => v == null);
        }

        protected override HashSet<Nodes.Base> Construct(Nodes.Base node)
        {
            return new HashSet<Nodes.Base>(((Nodes.Mapping)node).Content.Keys);
        }

        public override Nodes.Base Represent(HashSet<Nodes.Base> value)
        {
            var d = new Dictionary<Nodes.Base, Nodes.Base>(value.Count);
            foreach (var item in value)
                d.Add(item, new Nodes.Scalar<object> { Content = null, ScalarTag = new Null() } );
            return new Nodes.Mapping(d) { MappingTag = this };
        }
    }
}