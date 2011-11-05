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

        internal abstract object ConstructObject(Nodes.Base node, Constructor constructor);
    }

    public abstract class Base<T> : Base
    {

        public T AsValue(Nodes.Base node, Constructor constructor)
        {
            return this.Validate(node) ? (T)this.Construct(node, constructor) : default(T);
        }

        internal override object ConstructObject(Nodes.Base node, Constructor constructor)
        {
            return this.Construct(node, constructor);
        }

        protected abstract T Construct(Nodes.Base node, Constructor constructor);

        public abstract Nodes.Base Represent(T value);
    }
}
