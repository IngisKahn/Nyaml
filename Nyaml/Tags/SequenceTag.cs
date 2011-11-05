namespace Nyaml.Tags
{
    using System.Collections;

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

    public class Sequence : Sequence<IList>
    {
        protected internal Sequence() : base("tag:yaml.org,2002:seq")
        {
        }

        protected override IList Construct(Nodes.Base node, Constructor constructor)
        {
            var snode = (Nodes.Sequence) node;
            var list = new ArrayList(snode.Content.Count);
            foreach (var subnode in snode.Content)
                list.Add(constructor.ConstructObject(subnode));
            return list;
        }

        public override Nodes.Base Represent(IList value)
        {
            var s = new Nodes.Sequence { SequenceTag = this };
            foreach (var n in value)
                s.Content.Add((Nodes.Base)n);
            return s;
        }
    }
}
