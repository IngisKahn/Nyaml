namespace Nyaml.Tags
{
    using System.Collections.Generic;

    public abstract class Sequence<T> : Base<T>
    {
        protected Sequence(string name)
        {
            this.Name = name;
        }

        internal override bool Validate(Nodes.Base node)
        {
            return Validate(node as Nodes.Sequence);
        }

        public virtual bool Validate(Nodes.Sequence node)
        {
            return node != null;
        }

        internal override Nodes.Base Compose()
        {
            return new Nodes.Sequence { SequenceTag = this };
        }
    }

    public class Sequence : Sequence<IList<Nodes.Base>>
    {
        protected internal Sequence() : base("tag:yaml.org,2002:seq")
        {
        }

        protected override IList<Nodes.Base> Construct(Nodes.Base node)
        {
            return ((Nodes.Sequence)node).Content;
        }

        public override Nodes.Base Represent(IList<Nodes.Base> value)
        {
            return new Nodes.Sequence(value) { SequenceTag = this };
        }
    }
}
