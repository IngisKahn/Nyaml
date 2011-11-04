namespace Nyaml.Events
{
    public class Scalar : Node
    {
        public string Tag { get; set; }
        public ScalarImplicitLevel ImplicitLevel { get; set; }
        public string Value { get; set; }
        public Style Style { get; set; }

        protected override string Values
        {
            get
            {
                return base.Values + string.Format(", Tag={0}, Implicit={1}, Value={2}", this.Tag, this.ImplicitLevel, this.Value);
            }
        }
    }
}