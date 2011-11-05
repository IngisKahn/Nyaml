namespace Nyaml.Tags
{
    using System;
    using System.Collections;

    public abstract class Mapping<T> : Base<T>
    {
        protected Mapping(string name)
        {
            this.Name = name;
        }

        internal override bool Validate(Nodes.Base node)
        {
            return Validate(node as Nodes.Mapping);
        }

        public virtual bool Validate(Nodes.Mapping node)
        {
            return node != null;
        }

        internal override Nodes.Base Compose()
        {
            return new Nodes.Mapping { MappingTag = this };
        }
    }

    public sealed class Mapping : Mapping<IDictionary>
    {
        internal Mapping()
            : base("tag:yaml.org,2002:map")
        {}

        protected override IDictionary Construct(Nodes.Base node, Constructor constructor)
        {
            var mnode = (Nodes.Mapping) node;
            var result = new Hashtable(mnode.Content.Count);
            foreach (var kvp in mnode.Content)
            {
                var key = constructor.ConstructObject(kvp.Key);
                var value = constructor.ConstructObject(kvp.Value);
                result.Add(key, value);
            }
            return result;
        }

        public override Nodes.Base Represent(IDictionary value)
        {
            throw new NotImplementedException();
            //return new Nodes.Mapping(value) { MappingTag = this };
        }
    } 
}
