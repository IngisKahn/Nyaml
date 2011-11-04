namespace Nyaml.Tokens
{
    public class Scalar : SimpleValue
    {
        public override string Id
        {
            get { return "<scalar>"; }
        }

        public bool IsPlain { get; set; }
        public Style Style { get; set; }

        protected override string Values
        {
            get
            {
                return string.Format("Id={0}, Value={1}, IsPlain={2}, Style={3}", this.Id, this.Value, this.IsPlain, this.Style);
            }
        }
    }
}