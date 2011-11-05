namespace Nyaml.Tags
{
    using System.Text;
    using System.Text.RegularExpressions;

    public class Binary : Scalar<byte[]>
    {
        internal Binary()
        {
            this.Name = "tag:yaml.org,2002:binary";
        }

        static readonly Regex base64Expression = new Regex(@"^[a-zA-Z0-9+/\s\r\n](=[\s\r\n]*){0,2}$", RegexOptions.Compiled);
        static readonly Regex separatorExpression = new Regex("\\s", RegexOptions.Compiled);

        public override System.Func<string, string> CanonicalFormatter
        {
            get 
            {
                return v => 
                    {
                        var sb = new StringBuilder(v.Length);
                        foreach (var c in v)
                            if (!separatorExpression.IsMatch(c.ToString()))
                                sb.Append(c);
                        return sb.ToString();
                    };
            }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && base64Expression.IsMatch(node.Content);
        }

        protected override byte[] Construct(Nodes.Base node, Constructor constructor)
        {
            return System.Convert.FromBase64String(this.CanonicalFormatter(((Nodes.Scalar)node).Content));
        }

        public override Nodes.Base Represent(byte[] value)
        {
            return new Nodes.Scalar<byte[]> { ScalarTag = this, Content = System.Convert.ToBase64String(value) };
        }
    }
}
