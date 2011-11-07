namespace Nyaml.Tags
{
    using System.Globalization;
    using System.Numerics;
    using System.Text;
    using System.Text.RegularExpressions;

    public class Integer : Scalar<BigInteger, BigInteger>
    {
        internal Integer()
        {
            this.Name = "tag:yaml.org,2002:int";
        }
        
        static readonly Regex intExpression = new Regex(@"^[-+]?(0b[01_]+|0[0-7_]+|[1-9][0-9_]*|0x[0-9a-fA-F_]+|[1-9][0-9_]*(:[0-5]?[0-9])+)$", RegexOptions.Compiled);

        public override System.Func<string, string> CanonicalFormatter
        {
            get
            {
                return v =>
                {
                    var u = v.ToUpperInvariant();

                    var sb = new StringBuilder(u.Length);

                    var isNegative = false;
                    var isHex = false;
                    var isBinary = false;
                    var isFirstNonZero = false;

                    int pos;
                    if (u[0] == '-')
                    {
                        isNegative = true;
                        pos = 1;
                    }
                    else if (u[0] == '+')
                        pos = 1;
                    else
                        pos = 0;

                    var readANumber = false;
                    for (; pos < u.Length; pos++)
                    {
                        switch (u[pos])
                        {
                            case '_':
                                break;
                            case '0':
                                if (readANumber)
                                    sb.Append(u[pos]);
                                else
                                    readANumber = true;
                                break;
                            case 'B':
                                isBinary = true;
                                break;
                            case 'X':
                                isHex = true;
                                break;
                            default:
                                if (!readANumber)
                                {
                                    readANumber = true;
                                    isFirstNonZero = true;
                                }
                                sb.Append(u[pos]);
                                break;
                        }
                    }
                    u = sb.ToString();

                    // if it's a sexagesimal then we need to do some math
                    BigInteger b;
                    pos = u.IndexOf(':');
                    if (pos != -1)
                    {
                        var lastPos = pos;
                        b = BigInteger.Parse(u.Substring(0, pos), CultureInfo.InvariantCulture);
                        while ((pos = u.IndexOf(':', pos + 1)) != -1)
                        {
                            b *= 60;
                            var segment = u.Substring(lastPos + 1, pos - lastPos - 1);
                            b += BigInteger.Parse(segment, CultureInfo.InvariantCulture);
                            lastPos = pos;
                        }
                    }
                    else if (isBinary)
                    {
                        b = 0;
                        pos = 0;
                        while (pos < u.Length)
                        {
                            b *= 2;
                            if (u[pos] == '1')
                                b += 1;
                            pos++;
                        }
                    }
                    else if (isHex)
                        b = BigInteger.Parse(u, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    else if (!isFirstNonZero)
                    {
                        b = 0;
                        pos = 0;
                        while (pos < u.Length)
                        {
                            b *= 8;
                            b += u[pos] - '0';
                            pos++;
                        }
                    }
                    else
                        b = BigInteger.Parse(u, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                    if (isNegative)
                        b = -b;

                    return b.ToString();
                };
            }
        }

        public override bool Validate(Nodes.Scalar node)
        {
            return node != null && intExpression.IsMatch(node.Content);
        }

        protected override BigInteger Construct(Nodes.Base node, Constructor constructor)
        {
            return BigInteger.Parse(this.CanonicalFormatter(((Nodes.Scalar)node).Content));
        }

        public override Nodes.Base Represent(BigInteger value, Representer representer)
        {
            return new Nodes.Scalar { ScalarTag = this, Content = value.ToString() };
        }
    }
}
