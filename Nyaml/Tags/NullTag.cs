namespace Nyaml.Tags
{
    using System.Text.RegularExpressions;

    public class Null : Scalar<object>
    {
        internal Null()
        {
            this.Name = "tag:yaml.org,2002:null";
        }

        static readonly Regex nullExpression = new Regex("^(~|[nN]ull|NULL)?$", RegexOptions.Compiled);

        public override System.Func<string, string> CanonicalFormatter
        {
            get { return v => "~"; }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && nullExpression.IsMatch(node.Content);
        }

        protected override object Construct(Nodes.Base node, Constructor constructor)
        {
            return null;
        }

        public override Nodes.Base Represent(object value)
        {
            return new Nodes.Scalar<object> { ScalarTag = this, Content = "~" };
        }
    }
}
