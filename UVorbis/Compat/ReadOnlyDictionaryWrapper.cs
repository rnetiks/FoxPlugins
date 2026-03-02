using System.Collections;
using System.Collections.Generic;

namespace NVorbis
{
    internal class ReadOnlyDictionaryWrapper<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dict;

        public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
        {
            _dict = dictionary;
        }

        public TValue this[TKey key] => _dict[key];
        public IEnumerable<TKey> Keys => _dict.Keys;
        public IEnumerable<TValue> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
