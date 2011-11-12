namespace Nyaml.Nodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Sequence : Collection, IEquatable<Sequence>
    {
        public Tags.Base SequenceTag { get; set; }

        public override Tags.Base Tag
        {
            get { return this.SequenceTag; }
        }
        
        private readonly IList<Base> content = new List<Base>();
        public IList<Base> Content { get { return this.content; } }

        public Sequence() { this.content = new List<Base>(); }

        //public Sequence(IList<Base> content) { this.content = content; }

        public override string Id
        {
            get { return "sequence"; }
        }

        protected override string ValueString
        {
            get { return string.Join(", ", this.Content); }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Sequence);
        }

        public bool Equals(Sequence other)
        {
            return base.Equals(other) && other != null && this.content.Count == other.content.Count
                && !this.content.Where((n, i) => !n.Equals(other.content[i])).Any();
        }

        public override int GetHashCode()
        {
            var hash = base.GetHashCode();
            return this.content.Aggregate(hash, (val, n) => val << 5 + val ^ n.GetHashCode());
        }

        internal override void Serialize(Serializer serializer)
        {
            serializer.SerializeSequence(this, serializer.Schema.CanResolve(this, true));
        }
    }
}