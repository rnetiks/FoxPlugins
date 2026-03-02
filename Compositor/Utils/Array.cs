namespace Compositor.KK.Compositor
{
    public class Array
    {
        public static unsafe T[] FastFill<T>(int size, T value) where T : unmanaged
        {
            var array = new T[size];
            fixed (T* ptr = array)
            {
                for (int i = 0; i < size; i++)
                {
                    ptr[i] = value;
                }
            }
            return array;
        }
        
        public static unsafe T[] FastFill<T>(T[] arr,int size, T value) where T : unmanaged
        {
            fixed (T* ptr = arr)
            {
                for (int i = 0; i < size; i++)
                    ptr[i] = value;
            }
            return arr;
        }
    }
}