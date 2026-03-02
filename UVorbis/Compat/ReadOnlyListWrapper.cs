using System.Collections;
using System.Collections.Generic;

namespace NVorbis
{
    internal class ReadOnlyListWrapper<T> : IReadOnlyList<T>
    {
        private readonly IList<T> _list;

        public ReadOnlyListWrapper(IList<T> list)
        {
            _list = list;
        }

        public T this[int index] => _list[index];
        public int Count => _list.Count;

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
