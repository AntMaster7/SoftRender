using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace SoftRender
{
    // The MemoryPool<T> class from the .NET Framework didn't behave as expected and is an overkill
    public sealed class MemoryPoolSlim
    {
        private readonly object syncRoot = new object();

        private readonly Dictionary<IntPtr, int> lent = new();
        private readonly Dictionary<int, ConcurrentBag<IntPtr>> pool = new();

        public static MemoryPoolSlim Shared { get; } = new MemoryPoolSlim();

        private MemoryPoolSlim() { }

        public IntPtr Rent(int cb)
        {
            if (!pool.ContainsKey(cb))
            {
                lock (syncRoot)
                {
                    if (!pool.ContainsKey(cb))
                    {
                        pool[cb] = new ConcurrentBag<IntPtr>();
                    }
                }
            }

            var bag = pool[cb];
            var ptr = bag.TryTake(out IntPtr p) ? p : Marshal.AllocHGlobal(cb);
            lent.Add(ptr, cb);

            return ptr;
        }

        public void Return(IntPtr ptr)
        {
            if (lent.ContainsKey(ptr))
            {
                var cb = lent[ptr];
                lent.Remove(ptr);
                pool[cb].Add(ptr);
            }
        }

        // not thread-safe
        public void Free()
        {
            if (lent.Any())
            {
                throw new InvalidOperationException("Not all pointers have been returned.");
            }

            foreach (var item in pool)
            {
                while (item.Value.TryTake(out IntPtr ptr))
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            pool.Clear();
        }
    }
}
