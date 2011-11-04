namespace Nyaml
{
    using System.Linq;
    using System.Collections.Generic;

    public class OrderedMap<TKey, TValue> : IDictionary<TKey, TValue>
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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.entryList.GetEnumerator();
        }
    }
}
