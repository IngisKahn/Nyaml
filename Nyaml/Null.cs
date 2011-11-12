namespace Nyaml
{
    public class Null
    {
        private static readonly Null value = new Null();

        internal static Null Value { get { return value; }}

        private Null() { }

        public override bool Equals(object obj)
        {
            return obj is Null || obj == null;
        }

        public override int GetHashCode()
        {
            return 0x48A4DD83;
        }
    }
}
