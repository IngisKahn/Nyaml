namespace Nyaml
{
    using System;
    using System.Collections.Generic;

    public class Composer
    {
        [Serializable]
        public class Error : MarkedYamlError
        {
            public Error(string context = null, Mark contextMark = null,
            string problem = null, Mark problemMark = null, string note = null)
                : base(context, contextMark, problem, problemMark, note)
            { }
        }

        private readonly Dictionary<string, Nodes.Base> anchors =
            new Dictionary<string, Nodes.Base>();

        private readonly Parser parser;
        private readonly Schemas.Base schema;

        public Composer(Parser parser, Schemas.Base schema = null)
        {
            this.parser = parser;
            this.schema = schema ?? new Schemas.Full();
        }

        private Events.Base GetEvent()
        {
            return this.parser.GetEvent();
        }

        private bool CheckEvent<T>() where T : Events.Base
        {
            return this.parser.CheckEvent<T>();
        }

        private Events.Base PeekEvent()
        {
            return this.parser.PeekEvent();
        }

        public bool CheckNode()
        {
            if (this.CheckEvent<Events.StreamStart>())
                this.GetEvent();
            return !this.CheckEvent<Events.StreamEnd>();
        }

        public Nodes.Base GetNode()
        {
            return !this.CheckEvent<Events.StreamEnd>()
                       ? this.ComposeDocument()
                       : null;
        }

        public Nodes.Base GetSingleNode()
        {
            this.GetEvent();

            Nodes.Base document = null;
            if (!this.CheckEvent<Events.StreamEnd>())
            {
                document = this.ComposeDocument();

                if (!this.CheckEvent<Events.StreamEnd>())
                {
                    var @event = this.GetEvent();
                    throw new Error("expected a single document in the stream",
                                    document.StartMark, "but found another document", @event.StartMark);
                }
            }

            this.GetEvent();

            return document;
        }

        private Nodes.Base ComposeDocument()
        {
            this.GetEvent();

            var node = this.ComposeNode();

            this.GetEvent();

            this.anchors.Clear();
            return node;
        }

        private Nodes.Base ComposeNode(Action descender = null)
        {
            Events.Node @event;
            string anchor;
            if (this.CheckEvent<Events.Alias>())
            {
                @event = (Events.Alias)this.GetEvent();
                anchor = @event.Anchor;
                if (!this.anchors.ContainsKey(anchor))
                    throw new Error(null, null, "found undefined alias " + anchor, @event.StartMark);
                return this.anchors[anchor];
            }
            @event = this.PeekEvent() as Events.Node;
            anchor = @event != null ? @event.Anchor : null;
            if (anchor != null && this.anchors.ContainsKey(anchor))
                throw new Error(string.Format("found duplicate anchor {0}; first occurrence",
                    anchor), this.anchors[anchor].StartMark,
                    "second occurrence", @event.StartMark);
            if (descender != null)
                descender();
            Nodes.Base node = null;
            if (this.CheckEvent<Events.Scalar>())
                node = this.ComposeScalarNode(anchor);
            else if (this.CheckEvent<Events.SequenceStart>())
                node = this.ComposeSequenceNode(anchor);
            else if (this.CheckEvent<Events.MappingStart>())
                node = this.ComposeMappingNode(anchor);
            this.schema.AscendResolver();
            return node;
        }

        private Nodes.Scalar ComposeScalarNode(string anchor)
        {
            var @event = (Events.Scalar)this.GetEvent();
            var node = this.schema.CreateScalarNode(@event.Tag, @event.Value, @event.Style);
            node.StartMark = @event.StartMark;
            node.EndMark = @event.EndMark;
            if (anchor != null)
                this.anchors[anchor] = node;
            return node;
        }

        private Nodes.Sequence ComposeSequenceNode(string anchor)
        {
            var startEvent = (Events.SequenceStart) this.GetEvent();
            var node = new Nodes.Sequence
                       {
                           StartMark = startEvent.StartMark,
                           FlowStyle = startEvent.FlowStyle
                       };
            if (anchor != null)
                this.anchors[anchor] = node;
            var index = 0;
            while (!this.CheckEvent<Events.SequenceEnd>())
            {
                int index1 = index;
                node.Content.Add(this.ComposeNode(() => this.schema.DescendResolver(node, index1)));
                index++;
            }
            var endEvent = (Events.SequenceEnd) this.GetEvent();
            node.EndMark = endEvent.EndMark;

            node.SequenceTag = schema.Resolve(node);
            return node;
        }

        private Nodes.Mapping ComposeMappingNode(string anchor)
        {
            var startEvent = (Events.MappingStart)this.GetEvent();
            var node = new Nodes.Mapping
                       {
                StartMark = startEvent.StartMark,
                FlowStyle = startEvent.FlowStyle
            };
            if (anchor != null)
                this.anchors[anchor] = node;
            while (!this.CheckEvent<Events.MappingEnd>())
            {
                var key = this.ComposeNode(() => this.schema.DescendResolver(node, null));
                var value = this.ComposeNode(() => this.schema.DescendResolver(node, key));
                node.Content.Add(key, value);
            }
            var endEvent = (Events.MappingEnd)this.GetEvent();
            node.EndMark = endEvent.EndMark;

            node.MappingTag = schema.Resolve(node);
            return node;
        }
    }
}
