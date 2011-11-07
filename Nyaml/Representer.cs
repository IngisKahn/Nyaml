namespace Nyaml
{
    using System;
    using System.Collections.Generic;

    public class Representer
    {
        [Serializable]
        public class Error : YamlError
        {
            public Error(string message) : base(message) { }
        }

        private readonly Dictionary<object, Nodes.Base> representedObjects =
            new Dictionary<object, Nodes.Base>();

        private readonly Style defaultStyle;
        private readonly FlowStyle defaultFlowStyle;

        private readonly Serializer serializer;
        private readonly Schemas.Base schema;

        private object aliasKey;

        public Representer(Serializer serializer, Style defaultStyle = Style.None,
            FlowStyle defaultFlowStyle = FlowStyle.None, Schemas.Base schema = null)
        {
            this.serializer = serializer;
            this.defaultStyle = defaultStyle;
            this.defaultFlowStyle = defaultFlowStyle;
            this.schema = schema ?? new Schemas.Full();
        }

        public void Represent<T>(T data)
        {
            var node = this.RepresentData(data);
            this.serializer.Serialize(node);
            this.representedObjects.Clear();
            this.aliasKey = null;
        }

        private Nodes.Base RepresentData<T>(T data)
        {
            this.aliasKey = schema.IgnoreAliases(data) ? null : (object)data;
            if (this.aliasKey != null)
            {
                Nodes.Base keyNode;
                if (this.representedObjects.TryGetValue(this.aliasKey, out keyNode))
                    return keyNode;
            }
            return this.schema.Represent(data, this);
        }


    }
}
