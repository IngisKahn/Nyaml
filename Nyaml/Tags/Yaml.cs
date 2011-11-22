namespace Nyaml.Tags
{
    public class Yaml : Scalar<string, string>
    {
        internal Yaml()
        {
            this.Name = "tag:yaml.org,2002:yaml";
        }

        public override System.Func<string, string> CanonicalFormatter
        {
            get { return s => s; }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            if (node != null)
                switch (node.Content)
                {
                    case "!":
                    case "*":
                    case "&":
                        return true;
                }
            return false;
        }

        protected override string Construct(Nodes.Base node, Constructor constructor)
        {
            return ((Nodes.Scalar) node).Content;
        }

        public override Nodes.Base Represent(string value, Representer representer)
        {
            return new Nodes.Scalar { Content = value, Style = Style.Double, ScalarTag = this };
        }
    }
}
