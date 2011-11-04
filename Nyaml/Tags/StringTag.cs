namespace Nyaml.Tags
{
    public sealed class String : Scalar<string>
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
            return true;
        }

        protected override string Construct(Nodes.Base node)
        {
            return ((Nodes.Scalar)node).Content;
        }

        public override Nodes.Base Represent(string value)
        {
            return new Nodes.Scalar<string> { ScalarTag = this, Content = value };
        }
    }
}
