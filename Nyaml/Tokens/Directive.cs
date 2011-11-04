namespace Nyaml.Tokens
{
    using System;

    public class Directive : Base
    {
        public override string Id
        {
            get { return "<directive>"; }
        }

        public string Name { get; set; }
        public Tuple<string, string> Value { get; set; }

        protected override string Values
        {
            get
            {
                return string.Format("Id={0}, Name={1}, Value={2}", this.Id, this.Name, this.Value);
            }
        }
    }
}