namespace Nyaml.Tags
{
    internal sealed class Nonspecific : Base<object, object>
    {
        internal Nonspecific(bool isNonPlainScalar)
        {
            this.Name = isNonPlainScalar ? "!" : "?";
        }

        internal override bool Validate(Nodes.Base node)
        {
            return true;
        }

        protected override object Construct(Nodes.Base node, Constructor constructor)
        {
            throw new System.InvalidOperationException();
        }

        public override Nodes.Base Represent(object value, Representer representer)
        {
            throw new System.InvalidOperationException();
        }

        internal override Nodes.Base Compose()
        {
            throw new System.InvalidOperationException();
        }
    }
}
