namespace Nyaml.Tags
{
    public class Merge : Scalar<object, object>
    {
        internal Merge()
        {
            this.Name = "tag:yaml.org,2002:merge";
        }

        public override System.Func<string, string> CanonicalFormatter
        {
            get { return v => "<<"; }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && node.Content == "<<";
        }

        protected override object Construct(Nodes.Base node, Constructor constructor)
        {
            throw new System.InvalidOperationException();
        }

        public override Nodes.Base Represent(object value, Representer representer)
        {
            throw new System.InvalidOperationException();
        }
    }
}
