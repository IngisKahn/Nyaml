namespace Nyaml
{
    using System;
    using System.Collections.Generic;

    public interface IConstructor
    {
        bool CheckData();

        object GetData();

        object GetSingleData();
    }

    public class Constructor : IConstructor
    {
        [Serializable]
        public class Error : MarkedYamlError
        {
            public Error(string context = null, Mark contextMark = null,
            string problem = null, Mark problemMark = null, string note = null)
                : base(context, contextMark, problem, problemMark, note)
            { }
        }

        private readonly Dictionary<Nodes.Base, object> constructedObjects = new Dictionary<Nodes.Base, object>();
        private readonly HashSet<Nodes.Base> recursiveObjects = new HashSet<Nodes.Base>();

        private readonly IComposer composer;

        public Constructor(IComposer composer)
        {
            this.composer = composer;
        }

        public bool CheckData()
        {
            return this.composer.CheckNode();
        }

        public object GetData()
        {
            return this.composer.CheckNode()
                       ? this.ConstructDocument(this.composer.GetNode())
                       : null;
        }

        public object GetSingleData()
        {
            var node = this.composer.GetSingleNode();
            return node != null ? this.ConstructDocument(node) : null;
        }

        private object ConstructDocument(Nodes.Base node)
        {
            var data = this.ConstructObject(node);
            this.constructedObjects.Clear();
            this.recursiveObjects.Clear();
            return data;
        }

        public object ConstructObject(Nodes.Base node)
        {
            object data;
            if (this.constructedObjects.TryGetValue(node, out data))
                return data;
            if (this.recursiveObjects.Contains(node))
                throw new Error(null, null, "found unconstructable recursive node",
                                node.StartMark);
            this.recursiveObjects.Add(node);

            var mapping = node as Nodes.Mapping;
            if (mapping != null)
                this.FlattenMapping(mapping);
            data = node.Tag.ConstructObject(node, this);

            this.constructedObjects[node] = data;
            this.recursiveObjects.Remove(node);

            return data;
        }

        private void FlattenMapping(Nodes.Mapping mapping)
        {
            var merge = new List<KeyValuePair<Nodes.Base, Nodes.Base>>();
            var copy = new Dictionary<Nodes.Base, Nodes.Base>(mapping.Content);
            foreach (var kvp in mapping.Content)
            {
                var keyNode = kvp.Key;
                var valueNode = kvp.Value;
                if (!(keyNode.Tag is Tags.Merge))
                    continue;

                copy.Remove(keyNode);
                var mnode = valueNode as Nodes.Mapping;
                if (mnode != null)
                {
                    this.FlattenMapping(mnode);
                    merge.AddRange(mnode.Content);
                }
                else
                {
                    var snode = valueNode as Nodes.Sequence;
                    if (snode == null)
                        throw new Error("while constructing a mapping", mapping.StartMark,
                                        "expected a mapping or list of mappings for merging, but found " +
                                        valueNode.Id,
                                        valueNode.StartMark);

                    var submerge = new List<KeyValuePair<Nodes.Base, Nodes.Base>>();
                    foreach (var subnode in snode.Content)
                    {
                        mnode = subnode as Nodes.Mapping;
                        if (mnode == null)
                            throw new Error("while constructing a mapping", mapping.StartMark,
                                            "expected a mapping or merging, but found " + subnode.Id,
                                            subnode.StartMark);
                        this.FlattenMapping(mnode);
                        submerge.AddRange(mnode.Content);
                    }
                    submerge.Reverse();
                    merge.AddRange(submerge);
                }
            }
            if (merge.Count == 0) 
                return;
            mapping.Content.Clear();
            foreach (var kvp in merge)
                mapping.Content[kvp.Key] = kvp.Value;
            foreach (var kvp in copy)
                mapping.Content[kvp.Key] = kvp.Value;
        }
    }
}
