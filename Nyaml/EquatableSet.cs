namespace Nyaml
{
    using System.Collections.Generic;
    using System.Linq;

    public class EquatableSet<T> : HashSet<T>
    {
        public EquatableSet() { }

        public EquatableSet(IEnumerable<T> items) : base(items) { }

        public override int GetHashCode()
        {
            return this.Select(i => i.GetHashCode())
                .OrderBy(i => i).Aggregate((this.Count << 9) | (this.Count >> 23),
                                  (val, item) => val << 5 + val ^ item.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            var other = obj as ISet<T>;
            return other != null && this.Count == other.Count
                   && this.OrderBy(i => i.GetHashCode())
                          .Zip(other.OrderBy(i => i.GetHashCode()),
                               (a, b) => !a.Equals(b)).Any();
        }
    }
}
