using System.Collections.Generic;

namespace System.Collections.Generic
{
    /// <summary>
    /// Polyfill for IReadOnlyCollection&lt;T&gt; (not available in .NET 3.5).
    /// </summary>
    public interface IReadOnlyCollection<T> : IEnumerable<T>
    {
        int Count { get; }
    }
}
