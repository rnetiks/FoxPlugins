namespace System
{
    internal static class ArraySegmentExtensions
    {
        public static ArraySegment<T> Slice<T>(this ArraySegment<T> segment, int offset)
        {
            return new ArraySegment<T>(segment.Array, segment.Offset + offset, segment.Count - offset);
        }

        public static ArraySegment<T> Slice<T>(this ArraySegment<T> segment, int offset, int count)
        {
            return new ArraySegment<T>(segment.Array, segment.Offset + offset, count);
        }

        public static void CopyTo<T>(this ArraySegment<T> source, ArraySegment<T> destination)
        {
            Array.Copy(source.Array, source.Offset, destination.Array, destination.Offset, source.Count);
        }
    }
}
