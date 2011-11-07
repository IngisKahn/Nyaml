namespace Nyaml.Tags
{
    using System.Collections;

    public abstract class Sequence<TConstruct, TRepresent> : Base<TConstruct, TRepresent> 
        where TConstruct : IList, new()
        where TRepresent : IEnumerable
    {
        public Sequence() : this("tag:yaml.org,2002:seq") { }

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

        protected override TConstruct Construct(Nodes.Base node, Constructor constructor)
        {
            var snode = (Nodes.Sequence)node;
            var list = new TConstruct();
            foreach (var subnode in snode.Content)
                list.Add(constructor.ConstructObject(subnode));
            return list;
        }

        public override Nodes.Base Represent(TRepresent value, Representer representer)
        {
            var s = new Nodes.Sequence { SequenceTag = this };
            foreach (var n in value)
                s.Content.Add((Nodes.Base)n);
            return s;
        }
    }

    public sealed class Sequence : Sequence<ArrayList, IEnumerable> { }
}
