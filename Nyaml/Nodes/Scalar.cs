namespace Nyaml.Nodes
{
    public class Scalar : Base
    {
        // public string Value { get; set; }
        public Style Style { get; set; }

        public string Content { get; set; }

        public override string Id
        {
            get { return "scalar"; }
        }

        protected override string ValueString
        {
            get { return this.Content; }
        }

        internal override void Serialize(Serializer serializer)
        {
            var implicitLevel = serializer.Schema.CanResolve(this, true)
                                    ? ScalarImplicitLevel.Plain
                                    : serializer.Schema.CanResolve(this, false)
                                          ? ScalarImplicitLevel.NonPlain
                                          : ScalarImplicitLevel.None;
            serializer.SerializeScalar(this, implicitLevel);
        }
    //}

    //public sealed class Scalar<T> : Scalar
    //{
        public Tags.Base ScalarTag { get; set; }

        public override Tags.Base Tag
        {
            get { return this.ScalarTag; }
        }

        public override bool Equals(object obj)
        {
            var cannon = ((Tags.IScalar)this.ScalarTag).CanonicalFormatter;
            var other = obj as Scalar;
            return other != null
                && base.Equals(obj)
                && cannon(this.Content) == cannon(other.Content);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ ((Tags.IScalar)this.ScalarTag).CanonicalFormatter(this.Content).GetHashCode();
        }
    }
}