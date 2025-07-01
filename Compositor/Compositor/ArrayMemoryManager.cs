using System;
using System.Collections.Generic;
using DefaultNamespace;

namespace Compositor.KK.Compositor
{
    public class ArrayMemoryManager
    {
        private static readonly Dictionary<int, Queue<float[]>> _floatArrayPools = new Dictionary<int, Queue<float[]>>();
        private static readonly object _lock = new object();
        private static long _totalAllocatedBytes = 0;
        private static readonly long MAX_MEMORY_BYTES = 512 * 1024 * 1024;
        
        public static long TotalAllocatedBytes => _totalAllocatedBytes;
        public static long MaxMemoryBytes => MAX_MEMORY_BYTES;

        public static float[] Rent(int size)
        {
            lock (_lock)
            {
                if (_floatArrayPools.TryGetValue(size, out var pool) && pool.Count > 0)
                {
                    return pool.Dequeue();
                }

                long newArrayBytes = size * sizeof(float);
                if (_totalAllocatedBytes + newArrayBytes > MAX_MEMORY_BYTES)
                {
                    ForceCleanup();
                    if (_totalAllocatedBytes + newArrayBytes > MAX_MEMORY_BYTES)
                    {
                        throw new OutOfMemoryException();
                    }
                }
                
                _totalAllocatedBytes += newArrayBytes;
                Entry.Logger.LogDebug($"Allocated new float array: {size} elements, {newArrayBytes} bytes. Total: {_totalAllocatedBytes}");
                return new float[size];
            }
        }

        public static void Return(float[] array)
        {
            if (array == null) return;

            lock (_lock)
            {
                int size = array.Length;
                if (!_floatArrayPools.ContainsKey(size))
                {
                    _floatArrayPools[size] = new Queue<float[]>();
                }

                if (_floatArrayPools[size].Count < 20)
                {
                    System.Array.Clear(array, 0, array.Length);
                    _floatArrayPools[size].Enqueue(array);
                }
                else
                {
                    _totalAllocatedBytes -= size * sizeof(float);
                }
            }
        }

        public static void ForceCleanup()
        {
            lock (_lock)
            {
                Entry.Logger.LogWarning("Force cleanup triggered due to high memory pressure");

                foreach (var pool in _floatArrayPools.Values)
                {
                    while (pool.Count > 0)
                    {
                        var array = pool.Dequeue();
                        _totalAllocatedBytes -= array.Length * sizeof(float);
                    }
                }
                
                _floatArrayPools.Clear();
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public static MemoryStats GetMemoryStats()
        {
            lock (_lock)
            {
                int totalPooledArrays = 0;
                foreach (var pool in _floatArrayPools.Values)
                {
                    totalPooledArrays += pool.Count;
                }

                return new MemoryStats
                {
                    TotalAllocatedBytes = _totalAllocatedBytes,
                    MaxMemoryBytes = MAX_MEMORY_BYTES,
                    PooledArrayCount = totalPooledArrays,
                    FloatPoolCount = _floatArrayPools.Count,
                    MemoryPressure = (float)_totalAllocatedBytes / MAX_MEMORY_BYTES
                };
            }
        }
    }

    public struct MemoryStats
    {
        public long TotalAllocatedBytes;
        public long MaxMemoryBytes;
        public int PooledArrayCount;
        public int FloatPoolCount;
        public float MemoryPressure;
    }

    public class ManagedArrayData : IDisposable
    {
        private float[] _data;
        private bool _isDisposed = false;
        private readonly int _originalSize;
        
        public float[] Data => _isDisposed ? null : _data;
        public int Length => _isDisposed ? 0 : _data.Length;
        public bool IsValid => _isDisposed ? false : _data != null;

        public ManagedArrayData(int size)
        {
            _originalSize = size;
            _data = ArrayMemoryManager.Rent(size);
        }

        public ManagedArrayData(float[] data)
        {
            _originalSize = data?.Length ?? 0;
            _data = data;
        }

        public void Dispose()
        {
            if (!_isDisposed && _data != null)
            {
                ArrayMemoryManager.Return(_data);
                _data = null;
                _isDisposed = true;
            }
        }

        ~ManagedArrayData()
        {
            Dispose();
        }

        public static implicit operator float[] (ManagedArrayData managed)
        {
            return managed?.Data;
        }
    }

    public class SharedArrayData : IDisposable
    {
        private float[] _data;
        private int _refCount = 1;
        private readonly object _lock = new object();
        private bool _isDisposed = false;

        public float[] Data
        {
            get
            {
                lock (_lock)
                {
                    return _isDisposed ? null : _data;
                }
            }
        }

        public int RefCount
        {
            get
            {
                lock (_lock)
                {
                    return _refCount;
                }
            }
        }

        public SharedArrayData(float[] data)
        {
            _data = data;
        }

        public SharedArrayData AddRef()
        {
            lock (_lock)
            {
                if(_isDisposed)
                    throw new ObjectDisposedException(nameof(SharedArrayData));

                _refCount++;
                return this;
            }
        }

        public void Release()
        {
            lock (_lock)
            {
                if (_isDisposed) return;

                _refCount--;
                if (_refCount <= 0)
                    Dispose();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_isDisposed && _data != null)
                {
                    ArrayMemoryManager.Return(_data);
                    _data = null;
                    _isDisposed = true;
                }
            }
        }
    }
}