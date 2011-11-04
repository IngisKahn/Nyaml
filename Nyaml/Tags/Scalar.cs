namespace Nyaml.Tags
{
    public abstract class Scalar<T> : Base<T>
    {
        public abstract System.Func<string, string> CanonicalFormatter { get; }

        internal override bool Validate(Nodes.Base node)
        {
            return this.Validate(node as Nodes.Scalar);
        }

        public abstract bool Validate(Nodes.Scalar node);

        internal override Nodes.Base Compose()
        {
            return new Nodes.Scalar<T> { ScalarTag = this };
        }
    }
}
