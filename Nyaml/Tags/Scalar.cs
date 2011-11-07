namespace Nyaml.Tags
{
    public interface IScalar
    {
        System.Func<string, string> CanonicalFormatter { get; }
    }

    public abstract class Scalar<TConstruct, TRepresent> 
        : Base<TConstruct, TRepresent>, IScalar
    {
        public abstract System.Func<string, string> CanonicalFormatter { get; }

        internal override bool Validate(Nodes.Base node)
        {
            return this.Validate(node as Nodes.Scalar);
        }

        public abstract bool Validate(Nodes.Scalar node);

        internal override Nodes.Base Compose()
        {
            return new Nodes.Scalar { ScalarTag = this };
        }
    }
}
