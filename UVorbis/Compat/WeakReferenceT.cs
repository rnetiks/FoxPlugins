namespace System
{
    /// <summary>
    /// Polyfill for WeakReference&lt;T&gt; (not available in .NET 3.5).
    /// </summary>
    internal class WeakReference<T> where T : class
    {
        private readonly WeakReference _inner;

        public WeakReference(T target)
        {
            _inner = new WeakReference(target);
        }

        public bool TryGetTarget(out T target)
        {
            var obj = _inner.Target;
            if (obj is T t)
            {
                target = t;
                return true;
            }
            target = default(T);
            return false;
        }
    }
}
