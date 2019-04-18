using System;
using System.Collections.Generic;


namespace PdfViewer.Util.OrderedSet
{
    [Serializable]
    public class OrderedSet<T> : CollectionBase<T>, ICollection<T>
    {
        // The red-black tree that actually does the work of storing the items.
        private RedBlackTree<T> tree;

        public OrderedSet(IComparer<T> comparer)
        {
            Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            tree = new RedBlackTree<T>(comparer);
        }

        public IComparer<T> Comparer { get; }
        public sealed override int Count => tree.ElementCount;

        public sealed override IEnumerator<T> GetEnumerator()
        {
            return tree.GetEnumerator();
        }

        public sealed override bool Contains(T item)
        {
            T dummy;
            return tree.Find(item, false, false, out dummy);
        }

        public new bool Add(T item)
        {
            T dummy;
            return !tree.Insert(item, DuplicatePolicy.ReplaceFirst, out dummy);
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void AddMany(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            // If we're adding ourselves, then there is nothing to do.
            if (ReferenceEquals(collection, this))
                return;

            foreach (T item in collection)
                Add(item);
        }


        public sealed override bool Remove(T item)
        {
            T dummy;
            return tree.Delete(item, true, out dummy);
        }

        public sealed override void Clear()
        {
            tree.StopEnumerations(); // Invalidate any enumerations.

            // The simplest and fastest way is simply to throw away the old tree and create a new one.
            tree = new RedBlackTree<T>(Comparer);
        }

        private void CheckEmpty()
        {
            if (Count == 0)
                throw new InvalidOperationException(Strings.CollectionIsEmpty);
        }

        public T RemoveFirst()
        {
            CheckEmpty();
            tree.DeleteItemFromRange(tree.EntireRangeTester, true, out var item);
            return item;
        }
    }
}