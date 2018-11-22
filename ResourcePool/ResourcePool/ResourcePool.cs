using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ResourcePool
{
    public class ResourcePool<T> : IDisposable
        where T : class
    {
        readonly SemaphoreSlim _sem;
        readonly ConcurrentStack<T> _store;
        readonly Action<T> _onDispose;
        readonly Func<T> _factory;


        public ResourcePool(int cap, Func<T> factory, Action<T> onDispose = null)
        {
            _store = new ConcurrentStack<T>(new T[cap]);
            _sem = new SemaphoreSlim(cap);
            _onDispose = onDispose;
            _factory = factory;
        }

        public async Task<T> CheckOutAsync()
        {
            await _sem.WaitAsync();
            return Pop();
        }

        public T CheckOut()
        {
            _sem.Wait();
            return Pop();
        }

        public void CheckIn(T item)
        {
            Push(item);
            _sem.Release();
        }

        private T Pop()
        {
            var ok = _store.TryPop(out T result);
            return result ?? _factory();
        }

        private void Push(T item) => _store.Push(item);

        public void Dispose()
        {
            _sem.Dispose();
            if (_onDispose != null)
            {
                while (_store.TryPop(out T item))
                {
                    if (item != null)
                        _onDispose(item);
                }
            }
        }
    }
}
