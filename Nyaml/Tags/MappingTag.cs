namespace Nyaml.Tags
{
    using System.Collections.Generic;

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

    public sealed class Mapping : Mapping<Dictionary<Nodes.Base, Nodes.Base>>
    {
        internal Mapping()
            : base("tag:yaml.org,2002:map")
        {}

        protected override Dictionary<Nodes.Base, Nodes.Base> Construct(Nodes.Base node)
        {
            return ((Nodes.Mapping)node).Content;
        }

        public override Nodes.Base Represent(Dictionary<Nodes.Base, Nodes.Base> value)
        {
            return new Nodes.Mapping(value) { MappingTag = this };
        }
    } 
}
