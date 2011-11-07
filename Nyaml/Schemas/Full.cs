namespace Nyaml.Schemas
{
    using System.Linq;

    class Full : Core
    {
        public Full()
        {
            this.AddTag(new Tags.Binary());
            this.AddTag(new Tags.Merge());
            var omTag = new Tags.OrderedMap();
            this.AddTag(omTag, false);
            this.AddTagType<IOrderedMap>(omTag);
            this.AddTag(new Tags.Pairs());
            this.AddTag(new Tags.Set());
            this.AddTag(new Tags.Timestamp());
            this.AddTag(new Tags.Value(), false);
        }

        protected override Tags.Base ResolveSpecific(Nodes.Base node)
        {
            return this
                   .Tags
                   .Reverse()
                   .Where(t => t.Validate(node))
                   .First();
        }
    }
}
