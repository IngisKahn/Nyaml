namespace Nyaml.Nodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Mapping : Collection, IEquatable<Mapping>
    {
        public Tags.Base MappingTag { get; set; }

        public override Tags.Base Tag
        {
            get { return this.MappingTag; }
        }

        private readonly Dictionary<Base, Base> content = new Dictionary<Base, Base>();
        public Dictionary<Base, Base> Content { get { return this.content; } }

        public Mapping() { this.content = new Dictionary<Base, Base>(); }

        //internal Mapping(Dictionary<Base, Base> content) { this.content = content; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Mapping);
        }

        public bool Equals(Mapping other)
        {
            return base.Equals(other) && other != null && this.content.Count == other.content.Count
                && this.content.Zip(
                    other.content,
                    (kvp1, kvp2) => kvp1.Key.Equals(kvp2.Key) && kvp1.Value.Equals(kvp2.Value)
                    ).All(b => b);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            return this.content.Aggregate(
                                 hash,
                                 (val, kvp) => 
                                     { 
                                         var val2 = val << 5 + val ^ kvp.Key.GetHashCode();
                                         return val2 << 3 + val2 ^ kvp.Value.GetHashCode(); 
                                     }
                                 );
        }

        public override string Id
        {
            get { return "Mapping"; }
        }

        protected override string ValueString
        {
            get { return string.Join(", ", this.Content.Select(kvp => string.Format("{0}: {1}", kvp.Key, kvp.Value))); }
        }

        internal override void Serialize(Serializer serializer)
        {
            serializer.SerializeMapping(this, serializer.Schema.CanResolve(this, true));
        }
    }
}