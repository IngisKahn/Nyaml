namespace Nyaml.Schemas
{
    using System.Text.RegularExpressions;

    public class Core : Json
    {
        static readonly Regex nullExpression =
            new Regex("^([nN]ull|NULL|~)?$", RegexOptions.Compiled);
        static readonly Regex boolExpression = 
            new Regex("^([tT]rue|TRUE|[fF]alse|FALSE)?$", RegexOptions.Compiled);
        static readonly Regex intExpression = 
            new Regex("^([-+]?[0-9]+|0o[0-7]+|0x[0-9a-fA-F]+)$", RegexOptions.Compiled);
        static readonly Regex floatExpression = 
            new Regex("^([-+]?((\\.[0-9]+|[0-9]+(\\.[0-9]*)?)([eE][-+]?[0-9]+)?|\\.([iI]nf|INF)))|\\.([nN]an|NAN)$", RegexOptions.Compiled);

        protected override Tags.Base ResolveSpecific(Nodes.Base node)
        {
            if (node is Nodes.Mapping)
                return new Tags.Mapping();
            if (node is Nodes.Sequence)
                return new Tags.Sequence();

            // overkill
            var scalarNode = (Nodes.Scalar)node;
            
            var value = scalarNode.Content;

            if (nullExpression.IsMatch(value))
                return new Tags.Null();

            if (boolExpression.IsMatch(value))
                return new Tags.Boolean();

            if (intExpression.IsMatch(value))
                return new Tags.Integer();

            if (floatExpression.IsMatch(value))
                return new Tags.FloatingPoint();

            return new Tags.String();
        }
    }
}
