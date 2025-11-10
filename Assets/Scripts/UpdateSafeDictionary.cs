using System;
using System.Collections.Generic;

#nullable enable

namespace Orbits
{
    public interface IReadOnlyUpdateSafeDictionary<TKey, TValue>
    {
        int Count { get; }
        void GetItems(Action<IReadOnlyDictionary<TKey, TValue>> callback);
        IReadOnlyDictionary<TKey, TValue> UnsafeItems { get; }
    }

    public class UpdateSafeDictionary<TKey, TValue> : IReadOnlyUpdateSafeDictionary<TKey, TValue>
    {
        #region Fields
        private readonly Dictionary<TKey, TValue> items = new ();
        private readonly Dictionary<TKey, TValue> toAdd = new ();
        private readonly List<TKey> toRemove = new ();
        private int lockItems = 0;
        private bool clearFlag = false;

        public int Count => this.items.Count;

        public IReadOnlyDictionary<TKey, TValue> UnsafeItems => this.items;
        #endregion

        #region Methods
        public void Add(TKey key, TValue value)
        {
            if (this.lockItems > 0)
            {
                this.toAdd[key] = value;
            }
            else
            {
                this.items[key] = value;
            }
        }

        public void Remove(TKey key)
        {
            if (this.lockItems > 0)
            {
                this.toRemove.Add(key);
            }
            else
            {
                this.items.Remove(key);
            }
        }

        public void Clear()
        {
            if (this.lockItems > 0)
            {
                this.clearFlag = true;
            }
            else
            {
                this.DoClear();
            }
        }

        public void AddLock()
        {
            this.lockItems++;
        }

        public void RemoveLock()
        {
            this.lockItems--;
            this.CheckForDiffs();
        }

        public void GetItems(Action<IReadOnlyDictionary<TKey, TValue>> callback)
        {
            this.lockItems++;
            try
            {
                callback(this.items);
            }
            finally
            {
                this.lockItems--;
            }

            this.CheckForDiffs();
        }

        private void CheckForDiffs()
        {
            if (this.lockItems == 0)
            {
                if (this.clearFlag)
                {
                    this.DoClear();
                }
                else
                {
                    this.HandleDiffs();
                }
            }
        }

        private void DoClear()
        {
            this.clearFlag = false;
            this.items.Clear();
            this.toAdd.Clear();
            this.toRemove.Clear();
        }

        private void HandleDiffs()
        {
            if (this.toRemove.Count > 0)
            {
                foreach (var item in this.toRemove)
                {
                    this.items.Remove(item);
                }
                this.toRemove.Clear();
            }

            if (this.toAdd.Count > 0)
            {
                foreach (var kvp in this.toAdd)
                {
                    this.items[kvp.Key] = kvp.Value;
                }
                this.toAdd.Clear();
            }
        }
        #endregion
    }
}