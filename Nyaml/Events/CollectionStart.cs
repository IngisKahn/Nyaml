namespace Nyaml.Events
{
    public abstract class CollectionStart : Node
    {
        public string Tag { get; set; }
        public bool IsImplicit { get; set; }
        public FlowStyle FlowStyle { get; set; }

        protected override string Values
        {
            get { return string.Format("Anchor={0}, Tag={1}, Implicit={2}", this.Anchor, this.Tag, this.IsImplicit); }
        }
    }
}