namespace Nyaml.Tags
{
    using System.Text;
    using System.Text.RegularExpressions;

    public class FloatingPoint : Scalar<double, double>
    {
        internal FloatingPoint()
        {
            this.Name = "tag:yaml.org,2002:float";
        }

        static readonly Regex floatExpression = new Regex(@"^(([-+]?((([0-9][0-9_]*)?\.[0-9.]*([eE][-+][0-9]+)?)|([0-9][0-9_]*(:[0-5]?[0-9])+\.[0-9_]*))|\.([iI]nf|INF)))|\.([nN]an|NAN)$", RegexOptions.Compiled);

        public override System.Func<string, string> CanonicalFormatter
        {
            get 
            {
                return v =>
                    {
                        var u = v.ToUpperInvariant();

                        if (u == ".NAN")
                            return ".nan";

                        if (u.EndsWith(".INF", System.StringComparison.Ordinal))
                            return (u[0] == '-') ? "-.inf" : ".inf";

                        var sb = new StringBuilder(u.Length);

                        var isNegative = false;
                        var isBase60 = false;
                        
                        int pos;
                        switch (u[0])
                        {
                            case '-':
                                isNegative = true;
                                pos = 1;
                                break;
                            case '+':
                                pos = 1;
                                break;
                            default:
                                pos = 0;
                                break;
                        }

                        var hasHitDot = false;
                        for (; pos < u.Length; pos++)
                        {
                            switch (u[pos])
                            {
                                case '_':
                                    break;
                                case '.':
                                    if (!hasHitDot)
                                    {
                                        hasHitDot = true;
                                        sb.Append('.');
                                    }
                                    break;
                                case ':':
                                    isBase60 = true;
                                    sb.Append(':');
                                    break;
                                default:
                                    sb.Append(u[pos]);
                                    break;
                            }
                        }
                        u = sb.ToString();

                        // if it's a sexagesimal then we need to do some math
                        double d;
                        if (isBase60)
                        {
                            pos = u.IndexOf(':');
                            var lastPos = pos;
                            d = long.Parse(u.Substring(0, pos), System.Globalization.CultureInfo.InvariantCulture);
                            while ((pos = u.IndexOf(':', pos + 1)) != -1)
                            {
                                d *= 60d;
                                var segment = u.Substring(lastPos + 1, pos - lastPos - 1);
                                d += double.Parse(segment, System.Globalization.CultureInfo.InvariantCulture);
                                lastPos = pos;
                            }
                        }
                        else
                            d = double.Parse(u, System.Globalization.CultureInfo.InvariantCulture);

                        if (d < double.Epsilon && d > -double.Epsilon)
                            return "0";

                        if (isNegative)
                            d = -d;

                        u = d.ToString("e15", System.Globalization.CultureInfo.InvariantCulture);

                        // pluck extra zeros

                        //var dotPos = u.IndexOf('.');
                        var ePos = u.LastIndexOf('e');

                        var preEZeros = 0;
                        var postEZeros = 0;

                        for (var i = ePos + 2; i < ePos + 4; i++)
                            if (u[i] == '0')
                                postEZeros++;

                        for (var i = ePos - 1; u[i] == '0'; i--)
                            preEZeros++;

                        return u.Substring(0, ePos - preEZeros) + u.Substring(ePos, 2) + u.Substring(ePos + 2 + postEZeros);
                    }; 
            }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && floatExpression.IsMatch(node.Content);
        }

        protected override double Construct(Nodes.Base node, Constructor constructor)
        {
            var content = this.CanonicalFormatter(((Nodes.Scalar)node).Content);
            switch (content)
            {
                case ".nan":
                    return double.NaN;
                case ".inf":
                    return double.PositiveInfinity;
                case "-.inf":
                    return double.NegativeInfinity;
                default:
                    return double.Parse(content);
            }
        }

        public override Nodes.Base Represent(double value, Representer representer)
        {
            string content;
            if (double.IsNaN(value))
                content = ".nan";
            else if (double.IsPositiveInfinity(value))
                content = ".inf";
            else if (double.IsNegativeInfinity(value))
                content = "-.inf";
            else
                content = this.CanonicalFormatter(value.ToString());
            return new Nodes.Scalar { ScalarTag = this, Content = content };
        }
    }
}
