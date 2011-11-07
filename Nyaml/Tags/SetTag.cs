namespace Nyaml.Tags
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class Set : Mapping<HashSet<object>, ISet<object>>
    {
        internal Set() : base("tag:yaml.org,2002:set")
        { }

        public override bool Validate(Nodes.Mapping node)
        {
            if (node == null)
                return false;

            return node.Content.Values.All(v => v == null);
        }

        protected override HashSet<object> Construct(Nodes.Base node, Constructor constructor)
        {
            var mnode = (Nodes.Mapping) node;
            var set = new HashSet<object>();
            foreach (var key in mnode.Content.Keys)
                set.Add(constructor.ConstructObject(key));
            return set;
        }

        public override Nodes.Base Represent(ISet<object> value, Representer representer)
        {
            var m = new Nodes.Mapping { MappingTag = this };
            var d = m.Content;
            foreach (var item in value)
                d.Add((Nodes.Base)item, new Nodes.Scalar { Content = null, ScalarTag = new Null() } );
            return m;
        }
    }
}