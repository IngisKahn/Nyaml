namespace Nyaml.Tags
{
    using System.Text.RegularExpressions;

    public sealed class Boolean : Scalar<bool, bool>
    {
        internal Boolean()
        {
            this.Name = "tag:yaml.org,2002:bool";
        }

        static readonly Regex booleanExpression = new Regex("^([yY](es)?|YES|[nN]o?|NO|[tT]rue|TRUE|[fF]alse|FALSE|[oO]n|ON|[oO]ff|OFF)$", RegexOptions.Compiled);
        static readonly Regex trueExpression = new Regex("^([yY](es)?|YES|[tT]rue|TRUE|[oO]n|ON)$", RegexOptions.Compiled);

        public override System.Func<string, string> CanonicalFormatter
        {
            get { return v => trueExpression.IsMatch(v) ? "y" : "n"; }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && booleanExpression.IsMatch(node.Content);
        }

        protected override bool Construct(Nodes.Base node, Constructor constructor)
        {
            return this.CanonicalFormatter(((Nodes.Scalar)node).Content) == "y";
        }

        public override Nodes.Base Represent(bool value, Representer representer)
        {
            return new Nodes.Scalar { ScalarTag = this, Content = value ? "y" : "n" };
        }
    }
}
