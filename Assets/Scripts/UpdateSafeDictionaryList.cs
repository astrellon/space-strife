using System;
using System.Collections.Generic;
using Orbits.Extensions;

#nullable enable

namespace Orbits
{
    public interface IReadOnlyUpdateSafeDictionaryList<TKey, TValue>
    {
        int Count { get; }
        void GetItems(Action<Dictionary<TKey, List<TValue>>> callback);
    }

    public class UpdateSafeDictionaryList<TKey, TValue> : IReadOnlyUpdateSafeDictionaryList<TKey, TValue>
    {
        #region Fields
        private readonly Dictionary<TKey, List<TValue>> items = new ();
        private readonly List<KeyValuePair<TKey, TValue>> toAdd = new ();
        private readonly List<KeyValuePair<TKey, TValue>> toRemove = new ();
        private readonly List<TKey> toRemoveKey = new ();
        private int lockItems = 0;
        private bool clearFlag = false;

        public int Count => this.items.Count;

        public Dictionary<TKey, List<TValue>> UnsafeItems => this.items;
        #endregion

        #region Methods
        public void Add(TKey key, TValue value)
        {
            if (this.lockItems > 0)
            {
                this.toAdd.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
            else
            {
                if (!this.items.TryGetValue(key, out var list))
                {
                    list = new List<TValue>();
                    this.items[key] = list;
                }
                list.AddDistinct(value);
            }
        }

        public void Remove(TKey key)
        {
            if (this.lockItems > 0)
            {
                this.toRemoveKey.Add(key);
            }
            else
            {
                this.items.Remove(key);
            }
        }

        public void Remove(TKey key, TValue value)
        {
            if (this.lockItems > 0)
            {
                this.toRemove.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
            else
            {
                if (this.items.TryGetValue(key, out var list))
                {
                    list.Remove(value);
                }
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

        public void GetItems(Action<Dictionary<TKey, List<TValue>>> callback)
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
                    if (this.items.TryGetValue(item.Key, out var list))
                    {
                        list.Remove(item.Value);
                    }

                    if (list.Count == 0)
                    {
                        this.items.Remove(item.Key);
                    }
                }
                this.toRemove.Clear();
            }

            if (this.toRemoveKey.Count > 0)
            {
                foreach (var key in this.toRemoveKey)
                {
                    this.items.Remove(key);
                }

                this.toRemoveKey.Clear();
            }

            if (this.toAdd.Count > 0)
            {
                foreach (var kvp in this.toAdd)
                {
                    if (!this.items.TryGetValue(kvp.Key, out var list))
                    {
                        list = new List<TValue>();
                        this.items[kvp.Key] = list;
                    }
                    list.AddDistinct(kvp.Value);
                }
                this.toAdd.Clear();
            }
        }
        #endregion
    }
}