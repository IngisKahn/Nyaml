namespace Nyaml.Tokens
{
    using System;

    public class Tag : Base
    {
        public override string Id
        {
            get { return "<tag>"; }
        }

        public Tuple<string, string> Value { get; set; }

        protected override string Values
        {
            get
            {
                return string.Format("Id={0}, Value={1}", this.Id, this.Value);
            }
        }
    }
}