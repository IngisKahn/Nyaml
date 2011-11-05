namespace Nyaml
{
    using System.Collections;
    using System.Linq;
    using System.Collections.Generic;

    public interface IOrderedMap : IDictionary { }

    public class OrderedMap<TKey, TValue> : IDictionary<TKey, TValue>, IOrderedMap, IEnumerable<IDictionary>
    {
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> keyMap;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> entryList;

        public OrderedMap()
        {
            this.keyMap = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            this.entryList = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public void Add(TKey key, TValue value)
        {
            this.keyMap.Add(key, this.entryList.AddLast(new KeyValuePair<TKey, TValue>(key, value)));
        }

        public bool ContainsKey(TKey key)
        {
            return this.keyMap.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return this.keyMap.Keys; }
        }

        public bool Remove(TKey key)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (!this.keyMap.TryGetValue(key, out node))
                return false;

            this.keyMap.Remove(key);

            this.entryList.Remove(node);

            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (this.keyMap.TryGetValue(key, out node))
            {
                value = node.Value.Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public ICollection<TValue> Values
        {
            get { return (from n in this.entryList select n.Value).ToList(); }
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.keyMap[key].Value.Value;
            }
            set
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> node;
                if (this.keyMap.TryGetValue(key, out node))
                    node.Value = new KeyValuePair<TKey, TValue>(key, value);
                else
                    this.keyMap[key] = this.entryList.AddLast(new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            var node = new LinkedListNode<KeyValuePair<TKey, TValue>>(item);
            this.keyMap.Add(item.Key, node);
            this.entryList.AddLast(node);
        }

        public void Clear()
        {
            this.entryList.Clear();
            this.keyMap.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.entryList.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.entryList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.keyMap.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (!this.keyMap.TryGetValue(item.Key, out node))
                return false;
            this.keyMap.Remove(item.Key);
            this.entryList.Remove(node);
            return true;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.entryList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.entryList.GetEnumerator();
        }

        public void Add(object key, object value)
        {
            this.Add((TKey)key, (TValue)value);
        }

        public bool Contains(object key)
        {
            return this.ContainsKey((TKey)key);
        }

        private class DictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

            public DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
            {
                this.enumerator = enumerator;
            }

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(this.enumerator.Current.Key, this.enumerator.Current.Value); }
            }

            public object Key
            {
                get { return this.enumerator.Current.Key; }
            }

            public object Value
            {
                get { return this.enumerator.Current.Value; }
            }

            public object Current
            {
                get { return this.enumerator.Current; }
            }

            public bool MoveNext()
            {
                return this.enumerator.MoveNext();
            }

            public void Reset()
            {
                this.enumerator.Reset();
            }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(this.GetEnumerator());
        }

        public bool IsFixedSize
        {
            get { return ((IDictionary)this.keyMap).IsFixedSize; }
        }

        ICollection IDictionary.Keys
        {
            get { return this.Keys.ToList(); }
        }

        public void Remove(object key)
        {
            this.Remove((TKey)key);
        }

        ICollection IDictionary.Values
        {
            get { return (from n in this.entryList select n.Value).ToList(); }
        }

        public object this[object key]
        {
            get
            {
                return this[(TKey)key];
            }
            set
            {
                this[(TKey)key] = (TValue)value;
            }
        }

        public void CopyTo(System.Array array, int index)
        {
            throw new System.InvalidOperationException();
        }

        public bool IsSynchronized
        {
            get { return ((IDictionary)this.keyMap).IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return ((IDictionary)this.keyMap).SyncRoot; }
        }

        IEnumerator<IDictionary> IEnumerable<IDictionary>.GetEnumerator()
        {
            return (IEnumerator<IDictionary>) this.entryList.Select(kvp =>
                                                       {
                                                           IDictionary h = new Hashtable(1);
                                                           h.Add(kvp.Key, kvp.Value);
                                                           return h;
                                                       });
        }
    }
}
