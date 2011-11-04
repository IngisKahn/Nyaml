namespace Nyaml.Tokens
{
    using System.Text;

    public class StreamStart : Base
    {
        public override string Id
        {
            get { return "<stream start>"; }
        }

        public Encoding Encoding { get; set; }

        protected override string Values
        {
            get
            {
                return string.Format("Id={0}, Encoding={1}", this.Id, this.Encoding);
            }
        }
    }
}