namespace Nyaml.Tags
{
    public class Value : Scalar<string, string>
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

        protected override string Construct(Nodes.Base node, Constructor constructor)
        {
            return ((Nodes.Scalar)node).Content;
        }

        public override Nodes.Base Represent(string value, Representer representer)
        {
            return new Nodes.Scalar { ScalarTag = this, Content = "="};
        }
    }
}
