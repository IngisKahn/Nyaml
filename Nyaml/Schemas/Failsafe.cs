namespace Nyaml.Schemas
{
    public class Failsafe : Base
    {
        public Failsafe()
        {
            this.AddTag(new Tags.String());
            this.AddTag(new Tags.Mapping());
            this.AddTag(new Tags.Sequence());
        }

        protected override Tags.Base ResolveSpecific(Nodes.Base node)
        {
            return null;
        }
    }
}
