namespace Nyaml.Tokens
{
    public abstract class Base
    {
        public Mark StartMark { get; set; }
        public Mark EndMark { get; set; }
        public abstract string Id { get; }

        protected virtual string Values { get { return "Id=" + this.Id; } }

        public override string ToString()
        {
            return string.Format("{0}({1})", this.GetType().Name, this.Values);
        }
    }
}
