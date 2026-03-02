using System.Collections.Generic;

namespace System.Collections.Generic
{
    /// <summary>
    /// Polyfill for IReadOnlyDictionary&lt;TKey,TValue&gt; (not available in .NET 3.5).
    /// </summary>
    public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        TValue this[TKey key] { get; }
        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);
    }
}
