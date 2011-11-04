namespace Nyaml.Tokens
{
    public class Anchor : SimpleValue
    {
        public override string Id
        {
            get { return "<anchor>"; }
        }

        protected override string Values
        {
            get
            {
                return string.Format("Id={0}, Value={1}", this.Id, this.Value);
            }
        }
    }
}