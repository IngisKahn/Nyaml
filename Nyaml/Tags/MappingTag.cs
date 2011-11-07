namespace Nyaml.Tags
{
    using System;
    using System.Collections;

    public class Mapping<TConstruct, TRepresent> : Base<TConstruct, TRepresent> 
        where TConstruct :  new()
    {
        public Mapping() : this("tag:yaml.org,2002:map") { }

        protected Mapping(string name)
        {
            this.Name = name;
        }

        internal override bool Validate(Nodes.Base node)
        {
            return Validate(node as Nodes.Mapping);
        }

        public virtual bool Validate(Nodes.Mapping node)
        {
            return node != null;
        }

        internal override Nodes.Base Compose()
        {
            return new Nodes.Mapping { MappingTag = this };
        }

        protected override TConstruct Construct(Nodes.Base node, Constructor constructor)
        {
            var mnode = (Nodes.Mapping) node;
            var result = new TConstruct();
            var dict = result as IDictionary;
            if (dict != null)
                foreach (var kvp in mnode.Content)
                {
                    var key = constructor.ConstructObject(kvp.Key);
                    var value = constructor.ConstructObject(kvp.Value);
                    dict.Add(key, value);
                }
            return result;
        }

        public override Nodes.Base Represent(TRepresent value, Representer representer)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class Mapping : Mapping<Hashtable, IDictionary> { } 
}
