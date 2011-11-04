namespace Nyaml
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class Serializer
    {
        [Serializable]
        public class Error : Exception
        {
            public Error(string message)
                : base(message)
            {
            }
        }

        public Encoding Encoding { get; set; }
        public bool IsExplicitStart { get; set; }
        public bool IsExplicitEnd { get; set; }
        public Tuple<string, string> Version { get; set; }
        public IDictionary<string, string> Tags { get; set; }
        public Schemas.Base Schema { get; private set; }

        private readonly Action<Events.Base> emit;

        private readonly HashSet<Nodes.Base> serializedNodes = 
            new HashSet<Nodes.Base>();
        private readonly Dictionary<Nodes.Base, string> anchors =
            new Dictionary<Nodes.Base, string>();

        private int lastAnchorId;

        private bool? isClosed;

        public Serializer(Action<Events.Base> emit, Schemas.Base schema = null)
        {
            this.emit = emit;
            if (schema == null)
                schema = new Schemas.Full();
            this.Schema = schema;
        }

        public void Open()
        {
            if (!this.isClosed.HasValue)
            {
                this.emit(new Events.StreamStart { Encoding = this.Encoding });
                this.isClosed = false;
            }
            else if (this.isClosed.Value)
                throw new Error("serializer is closed");
            else
                throw new Error("serializer is already open");
        }

        public void Close()
        {
            if (!this.isClosed.HasValue)
                throw new Error("serializer is not open");
            if (this.isClosed.Value) 
                return;
            this.emit(new Events.StreamEnd());
            this.isClosed = true;
        }

        public void Serialize(Nodes.Base node)
        {
            if (!this.isClosed.HasValue)
                throw new Error("serializer is not open");
            if (this.isClosed.Value)
                throw new Error("serializer is closed");
            this.emit(new Events.DocumentStart { IsExplicit = this.IsExplicitStart,
                Version = this.Version, Tags = this.Tags});
            this.AnchorNode(node);
            this.SerializeNode(node);
            this.emit(new Events.DocumentEnd { IsExplicit = this.IsExplicitEnd });
            this.serializedNodes.Clear();
            this.anchors.Clear();
            this.lastAnchorId = 0;
        }

        private void AnchorNode(Nodes.Base node)
        {
            string anchor;
            if (this.anchors.TryGetValue(node, out anchor))
            {
                if (anchor == null)
                    this.anchors[node] = this.GenerateAnchor();
            }
            else
            {
                this.anchors[node] = null;
                var seqNode = node as Nodes.Sequence;
                if (seqNode != null)
                    foreach (var item in seqNode.Content)
                        this.AnchorNode(item);
                else
                {
                    var mapNode = node as Nodes.Mapping;
                    if (mapNode != null)
                        foreach (var pair in mapNode.Content)
                        {
                            this.AnchorNode(pair.Key);
                            this.AnchorNode(pair.Value);
                        }
                }
            }
        }

        private string GenerateAnchor()
        {
            return "id" + (++this.lastAnchorId).ToString("G3");
        }

        private void SerializeNode(Nodes.Base node, Action descend)
        {
            if (this.serializedNodes.Contains(node))
            {
                this.emit(new Events.Alias { Anchor = this.anchors[node] });
                return;
            }

            this.serializedNodes.Add(node);
            descend();

            node.Serialize(this);

            this.Schema.AscendResolver();
        }

        private void SerializeNode(Nodes.Base node)
        {
            this.SerializeNode(node, () => this.Schema.DescendResolver());
        }

        private void SerializeNode(Nodes.Base node, Nodes.Sequence parent, int index)
        {
            this.SerializeNode(node, () => this.Schema.DescendResolver(parent, index));
        }

        private void SerializeNode(Nodes.Base node, Nodes.Mapping parent, Nodes.Base key)
        {
            this.SerializeNode(node, () => this.Schema.DescendResolver(parent, key));
        }

        internal void SerializeScalar(Nodes.Scalar scalar, ScalarImplicitLevel implicitLevel)
        {
            this.emit(new Events.Scalar { Anchor = this.anchors[scalar], Tag = scalar.Tag.Name, Value = scalar.Content, ImplicitLevel = implicitLevel, Style = scalar.Style });
        }

        internal void SerializeSequence(Nodes.Sequence sequence, bool isImplicit)
        {
            this.emit(new Events.SequenceStart { Anchor = this.anchors[sequence], Tag = sequence.Tag.Name, IsImplicit = isImplicit, FlowStyle = sequence.FlowStyle });
            var index = 0;
            foreach (var item in sequence.Content)
                this.SerializeNode(item, sequence, index++);
            this.emit(new Events.SequenceEnd());
        }

        internal void SerializeMapping(Nodes.Mapping mapping, bool isImplicit)
        {
            this.emit(new Events.MappingStart { Anchor = this.anchors[mapping], Tag = mapping.Tag.Name, IsImplicit = isImplicit, FlowStyle = mapping.FlowStyle });
            foreach (var kvp in mapping.Content)
            {
                this.SerializeNode(kvp.Key, mapping, null);
                this.SerializeNode(kvp.Value, mapping, kvp.Key);
            }
            this.emit(new Events.MappingEnd());
        }
    }
}
