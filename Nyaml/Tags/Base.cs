namespace Nyaml.Tags
{
    public abstract class Base
    {
        public string Name { get; protected set; }

        public override bool Equals(object obj)
        {
            var other = obj as Base;
            return other != null && other.Name == this.Name;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }

        internal abstract bool Validate(Nodes.Base node);

        internal abstract Nodes.Base Compose();
    }

    public abstract class Base<T> : Base
    {

        public T AsValue(Nodes.Base node)
        {
            return this.Validate(node) ? this.Construct(node) : default(T);
        }

        protected abstract T Construct(Nodes.Base node);

        public abstract Nodes.Base Represent(T value);
    }
}
