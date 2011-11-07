namespace Nyaml.Schemas
{
    using System;
    using System.Collections.Generic;

    public abstract class Base
    {
        private readonly Dictionary<Type, Tags.Base> tagsByType =
            new Dictionary<Type, Tags.Base>();
        private readonly Dictionary<string, Tags.Base> tags = 
            new Dictionary<string, Tags.Base>();
        public IEnumerable<Tags.Base> Tags { get { return this.tags.Values; } }
        
        public Tags.Base GetTag(string name)
        {
            Tags.Base result;
            this.tags.TryGetValue(name, out result);
            return result;
        }

        protected Base()
        {
            this.AddTag(new Tags.Nonspecific(true));
        }

        // TODO: Implement path resolution according to YPath spec

        public void AddTag<TConstruct, TRepresent>(Tags.Base<TConstruct, TRepresent> tag, bool addType = true)
        {
            this.tags.Add(tag.Name, tag);
            if (addType)
                this.tagsByType.Add(typeof(TRepresent), tag);
        }

        public void AddTagType<T>(Tags.Base tag)
        {
            this.tagsByType.Add(typeof(T), tag);
        }

        public bool CanResolve(Nodes.Base node, bool isImplicit)
        {
            return node.Tag.Equals(this.ResolveTag(node, new Tags.Nonspecific(isImplicit)));
        }

        public Tags.Base Resolve<T>()
        {
            throw new NotImplementedException();
        }
        
        public Tags.Base Resolve(Nodes.Base node)
        {
            return node == null ? null : this.ResolveTag(node, node.Tag);
        }

        private Tags.Base ResolveTag(Nodes.Base node, Tags.Base tag)
        {
            if (tag != null)
            {
                tag = node.Tag as Tags.Nonspecific;
                if (tag == null) // if we already have specific tag...
                    return node.Tag;

                if (tag.Name == "!")
                {
                    if (node is Nodes.Mapping)
                        return new Tags.Mapping();
                    if (node is Nodes.Sequence)
                        return new Tags.Sequence();
                    if (node is Nodes.Scalar)
                        return new Tags.String();
                }
            }

            return this.ResolveSpecific(node);
        }

        protected abstract Tags.Base ResolveSpecific(Nodes.Base node);

#region Scalar Node Creation
        private class SimpleScalar : Nodes.Scalar
        {
            private readonly Tags.Base tag;
            public override Tags.Base Tag
            {
                get { return this.tag; }
            }

            public SimpleScalar(Tags.Base tag, string content, Style style)
            {
                this.tag = tag ?? new Tags.Nonspecific(style == Style.Literal);
                this.Content = content;
                this.Style = Style.Folded;
            }
        }

        public Nodes.Scalar CreateScalarNode(string tagName, string value, Style style)
        {
            var tag = this.Resolve(new SimpleScalar(this.GetTag(tagName), value, style));
            var node = (Nodes.Scalar)tag.Compose();
            node.Content = value;
            return node;
        }
#endregion

        public void AscendResolver()
        {
            // TODO: path resolution
        }

        public void DescendResolver(Nodes.Sequence currentSequence, int currentIndex)
        {
            // TODO: path resolution
        }

        public void DescendResolver(Nodes.Mapping currentMap, Nodes.Base currentKey)
        {
            // TODO: path resolution
        }

        public void DescendResolver()
        {
            // TODO: path resolution
        }

        internal Nodes.Base Represent<T>(T data, Representer representer)
        {
            Tags.Base tag;
            return !this.tagsByType.TryGetValue(typeof(T), out tag) 
                ? null : tag.RepresentObject(data, representer);
        }

        internal bool IgnoreAliases<T>(T data)
        {
            return typeof(T).IsPrimitive;
        }
    }
}
