using System.Collections.Generic;

namespace System.Collections.Generic
{
    /// <summary>
    /// Polyfill for IReadOnlyList&lt;T&gt; (not available in .NET 3.5).
    /// </summary>
    public interface IReadOnlyList<T> : IReadOnlyCollection<T>
    {
        T this[int index] { get; }
    }
}
