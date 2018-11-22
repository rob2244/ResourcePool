using System;
using System.Collections.Generic;
using System.Text;

namespace ResourcePool.SelfCleaning
{
    internal class PoolItem<T> : IDisposable
        where T : class
    {
        bool _isInUse = false;

        public T Item { get; }
        public DateTime LastUsed { get; private set; }
        public bool IsInUse
        {
            get { return _isInUse; }
            set
            {
                if (value == true)
                {
                    LastUsed = DateTime.UtcNow;
                }
                _isInUse = value;
            }
        }

        public PoolItem(T item)
        {
            Item = item;
            LastUsed = DateTime.UtcNow;
        }

        public bool IsExpired(TimeSpan time) => DateTime.UtcNow - time >= LastUsed;

        public void Dispose()
        {
            if (Item != null && Item is IDisposable)
                ((IDisposable)Item).Dispose();
        }
    }
}
