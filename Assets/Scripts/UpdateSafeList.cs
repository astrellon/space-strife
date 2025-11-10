using System;
using System.Collections.Generic;

#nullable enable

namespace Orbits
{
    public interface IReadOnlyUpdateSafeList<T>
    {
        int Count { get; }
        void GetItems(Action<IReadOnlyList<T>> callback);
        IReadOnlyList<T> UnsafeItems { get; }
    }

    public class UpdateSafeList<T> : IReadOnlyUpdateSafeList<T>
    {
        #region Fields
        private readonly List<T> items = new ();
        private readonly List<T> toAdd = new ();
        private readonly List<T> toRemove = new ();
        private int lockItems = 0;
        private bool clearFlag = false;

        public int Count => this.items.Count;

        public IReadOnlyList<T> UnsafeItems => this.items;
        #endregion

        #region Methods
        public void AddDistinct(T item)
        {
            if (this.items.Contains(item) || this.toAdd.Contains(item))
            {
                return;
            }

            this.Add(item);
        }

        public void Add(T item)
        {
            if (this.lockItems > 0)
            {
                this.toAdd.Add(item);
            }
            else
            {
                this.items.Add(item);
            }
        }

        public void Remove(T item)
        {
            if (this.lockItems > 0)
            {
                this.toRemove.Add(item);
            }
            else
            {
                this.items.Remove(item);
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

        public void GetItems(Action<IReadOnlyList<T>> callback)
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
                foreach (var item in this.toAdd)
                {
                    this.items.Add(item);
                }
                this.toAdd.Clear();
            }
        }
        #endregion
    }
}