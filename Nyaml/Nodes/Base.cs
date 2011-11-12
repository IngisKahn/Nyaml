namespace Nyaml.Nodes
{
    public abstract class Base 
    {
        public Mark StartMark { get; set; }
        public Mark EndMark { get; set; }
        public abstract Tags.Base Tag { get; }

        public abstract string Id { get; }

        protected abstract string ValueString { get; }

        public override string ToString()
        {
            return string.Format("{0}(tag={1}, value={2})", this.GetType().Name, this.Tag, this.ValueString);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Base;
            return other != null && other.Tag.Equals(this.Tag);
        }

        public override int GetHashCode()
        {
            return this.Tag.GetHashCode();
        }

        internal abstract void Serialize(Serializer serializer);
    }
}
