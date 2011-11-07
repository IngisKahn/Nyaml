namespace Nyaml.Schemas
{
    using System.Text.RegularExpressions;

    public class Json : Failsafe
    {
        public Json()
        {
            this.AddTag(new Tags.Null());
            this.AddTag(new Tags.Boolean());
            var intTag = new Tags.Integer();
            this.AddTag(intTag);
            this.AddTagType<byte>(intTag);
            this.AddTagType<sbyte>(intTag);
            this.AddTagType<short>(intTag);
            this.AddTagType<ushort>(intTag);
            this.AddTagType<long>(intTag);
            this.AddTagType<ulong>(intTag);
            this.AddTagType<int>(intTag);
            this.AddTagType<uint>(intTag);
            var floatTag = new Tags.FloatingPoint();
            this.AddTag(floatTag);
            this.AddTagType<float>(floatTag);
        }

        static readonly Regex intExpression = 
            new Regex("^-?(0|[1-9])[0-9]*$", RegexOptions.Compiled);
        static readonly Regex floatExpression = 
            new Regex("^-?(0|[1-9])[0-9]*(\\.[0-9]*)?([eE][-+]?[0-9]+)?$", RegexOptions.Compiled);

        protected override Tags.Base ResolveSpecific(Nodes.Base node)
        {
            if (node is Nodes.Mapping)
                return new Tags.Mapping();
            if (node is Nodes.Sequence)
                return new Tags.Sequence();

            var scalarNode = (Nodes.Scalar)node;

            var value = scalarNode.Content;

            switch (value)
            {
                case "null":
                    return new Tags.Null();
                case "true":
                case "false":
                    return new Tags.Boolean();
            }

            if (intExpression.IsMatch(value))
                return new Tags.Integer();
            if (floatExpression.IsMatch(value))
                return new Tags.FloatingPoint();

            return null;
        }
    }
}
