namespace Nyaml.Tags
{
    public class Value : Scalar<object>
    {
        internal Value()
        {
            this.Name = "tag:yaml.org,2002:value";
        }

        public override System.Func<string, string> CanonicalFormatter
        {
            get { return v => "="; }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && node.Content == "=";
        }

        protected override object Construct(Nodes.Base node)
        {
            throw new System.InvalidOperationException();
        }

        public override Nodes.Base Represent(object value)
        {
            throw new System.InvalidOperationException();
        }
    }
}
