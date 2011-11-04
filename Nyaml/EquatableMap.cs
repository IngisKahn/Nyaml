namespace Nyaml
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class EquatableMap<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IStructuralEquatable
    {
        private readonly IDictionary<TKey, TValue> map;

        public EquatableMap(IDictionary<TKey, TValue> map)
        {
            this.map = map;
        }

        public bool Equals(object other, IEqualityComparer comparer)
        {
            var otherMap = other as IDictionary<TKey, TValue>;
            if (otherMap == null || this.map.Count != otherMap.Count)
                return false;
            foreach (var kvp in this.map)
            {
                var key1 = kvp.Key;
                TValue val2;
                if (!otherMap.TryGetValue(key1, out val2) ||
                    !comparer.Equals(kvp.Value, val2))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return this.GetHashCode(EqualityComparer<object>.Default);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj, EqualityComparer<object>.Default);
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return this.map.Aggregate(0, (val, kvp) =>
                                         { 
                                             val = val << 5 + val ^ comparer.GetHashCode(kvp.Key);
                                             return val << 5 + val ^ comparer.GetHashCode(kvp.Value);
                                         });
        }

        public void Add(TKey key, TValue value)
        {
            this.map.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return this.map.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return this.map.Keys; }
        }

        public bool Remove(TKey key)
        {
            return this.map.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.map.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return this.map.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.map[key];
            }
            set
            {
                this.map[key] = value;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.map.Add(item);
        }

        public void Clear()
        {
            this.map.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.map.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.map.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.map.Count; }
        }

        public bool IsReadOnly
        {
            get { return this.map.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.map.Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.map.GetEnumerator();
        }

        public void Add(object key, object value)
        {
            ((IDictionary)this.map).Add(key, value);
        }

        public bool Contains(object key)
        {
            return ((IDictionary)this.map).Contains(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)this.map).GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return ((IDictionary)this.map).IsFixedSize; }
        }

        ICollection IDictionary.Keys
        {
            get { return ((IDictionary)this.map).Keys; }
        }

        public void Remove(object key)
        {
            ((IDictionary)this.map).Remove(key);
        }

        ICollection IDictionary.Values
        {
            get { return ((IDictionary)this.map).Values; }
        }

        public object this[object key]
        {
            get
            {
                return ((IDictionary)this.map)[key];
            }
            set
            {
                ((IDictionary)this.map)[key] = value;
            }
        }

        public void CopyTo(System.Array array, int index)
        {
            ((IDictionary)this.map).CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return ((IDictionary)this.map).IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return ((IDictionary)this.map).SyncRoot; }
        }
    }
}
