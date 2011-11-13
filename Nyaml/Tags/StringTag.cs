namespace Nyaml.Tags
{
    public sealed class String : Scalar<string, string>
    {
        public String()
        {
            this.Name = "tag:yaml.org,2002:str";
        }

        public override System.Func<string, string> CanonicalFormatter
        {
            get { return v => v; }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null;
        }

        protected override string Construct(Nodes.Base node, Constructor constructor)
        {
            return ((Nodes.Scalar)node).Content;
        }

        public override Nodes.Base Represent(string value, Representer representer)
        {
            return new Nodes.Scalar { ScalarTag = this, Content = value };
        }
    }
}
