﻿namespace Nyaml.Schemas
{
    using System.Collections.Generic;

    public abstract class Base
    {
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

        public void AddTag<T>(T tag) where T : Tags.Base { this.tags.Add(tag.Name, tag); }

        public bool CanResolve(Nodes.Base node, bool isImplicit)
        {
            return node.Tag.Equals(this.ResolveTag(node, new Tags.Nonspecific(isImplicit)));
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
            private Tags.Base tag;
            public override Tags.Base Tag
            {
                get { throw new System.NotImplementedException(); }
            }

            public SimpleScalar(Tags.Base tag, string content)
            {
                this.tag = tag;
                this.Content = content;
            }
        }

        public Nodes.Scalar CreateScalarNode(string tagName, string value)
        {
            var tag = this.Resolve(new SimpleScalar(this.GetTag(tagName), value));
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
    }
}
