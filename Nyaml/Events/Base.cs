namespace Nyaml.Events
{
    public abstract class Base
    {
        public Mark StartMark { get; set; }
        public Mark EndMark { get; set; }

        protected virtual string Values
        {
            get { return string.Empty; }
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", this.GetType().Name, this.Values);
        }
    }
}
