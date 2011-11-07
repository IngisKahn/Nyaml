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
        internal abstract Nodes.Base RepresentObject(object data, Representer representer);
    }

    public abstract class Base<TConstruct, TRepresent> : Base
    {

        public TConstruct AsValue(Nodes.Base node, Constructor constructor)
        {
            return this.Validate(node) ? this.Construct(node, constructor) : default(TConstruct);
        }

        internal override object ConstructObject(Nodes.Base node, Constructor constructor)
        {
            return this.Construct(node, constructor);
        }

        protected abstract TConstruct Construct(Nodes.Base node, Constructor constructor);

        internal override Nodes.Base RepresentObject(object data, Representer representer)
        {
            return this.Represent((TRepresent)data, representer);
        }

        public abstract Nodes.Base Represent(TRepresent value, Representer representer);

        internal virtual bool IgnoreAliases(TRepresent data)
        {
            return false;
        }
    }
}
