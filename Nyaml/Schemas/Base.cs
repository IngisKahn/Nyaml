﻿namespace Nyaml.Schemas
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class Error : MarkedYamlError
    {
        public Error(string context = null, Mark contextMark = null,
        string problem = null, Mark problemMark = null, string note = null)
            : base(context, contextMark, problem, problemMark, note)
        { }
    }

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
            if (name != null)
            {
                if (!this.tags.TryGetValue(name, out result))
                {
                 //   throw new Error("could not find definition of tag: " + name);
                }
            }
            else
                result = null;
            return result;
        }

        protected Base()
        {
            this.AddTag(new Tags.Nonspecific(true));
        }

        // TODO: Implement path resolution according to YPath spec

        public void AddTag<TConstruct, TRepresent>(Tags.Base<TConstruct, TRepresent> tag, bool addType = true)
        {
            this.tags[tag.Name] = tag;
            if (addType)
                this.tagsByType[typeof(TRepresent)] = tag;
        }

        public void AddTagType<T>(Tags.Base tag)
        {
            this.tagsByType[typeof(T)] = tag;
        }

        internal bool CanResolve(Nodes.Base node, bool isImplicit)
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
                {
                    // ensure it is valid
                    if (!node.Tag.Validate(node))
                        throw new Error("failed validating a node tag: " + node.Tag.Name,
                            node.StartMark);
                    return node.Tag;
                }

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
                this.tag = tag ?? new Tags.Nonspecific(style != Style.Plain);
                this.Content = content;
                this.Style = Style.Folded;
            }
        }

        public Nodes.Scalar CreateScalarNode(string tagName, string value, Style style)
        {
            var tag = this.Resolve(new SimpleScalar(this.GetTag(tagName), value, style)) ?? new Tags.String();
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
