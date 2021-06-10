using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenActive.Server.NET.OpenBookingHelper.Async
{
    public sealed class AsyncDuplicateLock<TKey>
    {
        private sealed class RefCounted<T>
        {
            public RefCounted(T value)
            {
                RefCount = 1;
                Value = value;
            }

            public int RefCount { get; set; }
            public T Value { get; }
        }

        private static readonly Dictionary<TKey, RefCounted<SemaphoreSlim>> SemaphoreSlims
            = new Dictionary<TKey, RefCounted<SemaphoreSlim>>();

        private SemaphoreSlim GetOrCreate(TKey key)
        {
            RefCounted<SemaphoreSlim> item;
            lock (SemaphoreSlims)
            {
                if (SemaphoreSlims.TryGetValue(key, out item))
                {
                    ++item.RefCount;
                }
                else
                {
                    item = new RefCounted<SemaphoreSlim>(new SemaphoreSlim(1, 1));
                    SemaphoreSlims[key] = item;
                }
            }
            return item.Value;
        }

        public async Task<IDisposable> LockAsync(TKey key)
        {
            await GetOrCreate(key).WaitAsync().ConfigureAwait(false);
            return new Releaser { Key = key };
        }

        private sealed class Releaser : IDisposable
        {
            public TKey Key { get; set; }

            public void Dispose()
            {
                RefCounted<SemaphoreSlim> item;
                lock (SemaphoreSlims)
                {
                    item = SemaphoreSlims[Key];
                    --item.RefCount;
                    if (item.RefCount == 0)
                        SemaphoreSlims.Remove(Key);
                }
                item.Value.Release();
            }
        }
    }
}