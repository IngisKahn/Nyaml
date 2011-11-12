namespace Nyaml
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class EquatableList<T> : IList<T>, IList, IStructuralEquatable
    {
        private readonly IList<T> list;

        public EquatableList(IList<T> list)
        {
            this.list = list;
        }

        public EquatableList()
        {
            this.list = new List<T>();
        }

        public override int GetHashCode()
        {
            return this.GetHashCode(EqualityComparer<object>.Default);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj, EqualityComparer<object>.Default);
        }

        public bool Equals(object other, IEqualityComparer comparer)
        {
            var otherList = other as IList<T>;
            if (otherList == null || this.Count != otherList.Count)
                return false;
            for (var i = 0; i < this.Count; i++)
                if (!comparer.Equals(this[i], otherList[i]))
                    return false;
            return true;
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return this.Aggregate(0, (val, item) => val << 5 + val ^ comparer.GetHashCode(item));
        }

        int IList.Add(object value)
        {
            return ((IList)this.list).Add(value);
        }



        bool IList.Contains(object value)
        {
            return ((IList)this.list).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)this.list).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)this.list).Insert(index, value);
        }

        public bool IsFixedSize
        {
            get { return ((IList)this.list).IsFixedSize; }
        }

        void IList.Remove(object value)
        {
            ((IList)this.list).Remove(value);
        }

        object IList.this[int index]
        {
            get
            {
                return this.list[index];
            }
            set
            {
                ((IList)this.list)[index] = value;
            }
        }

        public void CopyTo(System.Array array, int index)
        {
            ((IList)this.list).CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return ((IList)this.list).IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return ((IList)this.list).SyncRoot; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList)this.list).GetEnumerator();
        }



        public int IndexOf(T item)
        {
            return this.list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this.list.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return this.list[index];
            }
            set
            {
                this.list[index] = value;
            }
        }

        public void Add(T item)
        {
            this.list.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                this.Add(item);
        }

        public void Clear()
        {
            this.list.Clear();
        }

        public bool Contains(T item)
        {
            return this.list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.list.Count; }
        }

        public bool IsReadOnly
        {
            get { return this.list.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return this.list.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }
    }
}
