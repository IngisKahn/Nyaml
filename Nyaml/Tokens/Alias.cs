namespace Nyaml.Tokens
{
    public class Alias : SimpleValue
    {
        public override string Id
        {
            get { return "<alias>"; }
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