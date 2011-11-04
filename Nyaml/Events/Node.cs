namespace Nyaml.Events
{
    public abstract class Node : Base
    {
        public string Anchor { get; set; }

        protected override string Values
        {
            get { return "Anchor=" + this.Anchor; }
        }
    }
}