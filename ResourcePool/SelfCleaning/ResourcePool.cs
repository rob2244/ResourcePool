using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResourcePool.SelfCleaning
{
    public class ResourcePool<T>
        where T : class
    {
        readonly SemaphoreSlim _sem;
        readonly ConcurrentBag<PoolItem<T>> _store;
        readonly Action<T> _onDispose;
        readonly Func<T> _factory;
        readonly Timer _cleanTimer;

        public TimeSpan CleantInterval { get; }
        public TimeSpan ItemLifetime { get; set; }


        public ResourcePool(int cap,
                                        TimeSpan cleanInterval,
                                        TimeSpan lifetime,
                                        Func<T> factory,
                                        Action<T> onDispose = null)
        {
            _store = new ConcurrentBag<PoolItem<T>>(new PoolItem<T>[cap]);
            _sem = new SemaphoreSlim(cap);
            _onDispose = onDispose;
            _factory = factory;
            CleantInterval = cleanInterval;
            ItemLifetime = lifetime;
            _cleanTimer = new Timer(CleanResourcePool, null, CleantInterval, CleantInterval);
        }

        public async Task<T> CheckOutAsync()
        {
            await _sem.WaitAsync();
            return Remove();
        }

        public T CheckOut()
        {
            _sem.Wait();
            return Remove();
        }

        public void CheckIn(T item)
        {
            Add(item);
            _sem.Release();
        }

        private T Remove()
        {
            var ok = _store.TryPeek(out PoolItem<T> result);

            if (result == null || result.IsInUse)
            {
                var item = new PoolItem<T>(_factory())
                {
                    IsInUse = true
                };
                _store.Add(item);
                return item.Item;
            }


            result.IsInUse = true;
            return result.Item;
        }

        private void Add(T item)
        {
            var pi = _store.FirstOrDefault(c => c.Item == item);
            if (pi == null)
                throw new InvalidOperationException("Item not found in pool");

            pi.IsInUse = false;
        }

        // No good way to remove from ConcurrentBag so have to remove and re add
        private void CleanResourcePool(object state = null)
        {
            if (_store.IsEmpty) return;

            while (_store.TryTake(out PoolItem<T> pi))
            {
                if (!pi.IsInUse && pi.IsExpired(ItemLifetime))
                {
                    pi.Dispose();
                }
                else
                {
                    _store.Add(pi);
                }
            }
        }

        public void Dispose()
        {
            _sem.Dispose();
            if (_onDispose != null)
            {
                while (_store.TryTake(out PoolItem<T> pi))
                    pi.Dispose();
            }
        }
    }
}
