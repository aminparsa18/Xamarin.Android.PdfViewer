using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfViewer.Util.OrderedSet
{
    [Serializable]
    public abstract class CollectionBase<T> : ICollection<T>, ICollection
    {
        public override string ToString()
        {
            return Algorithms.ToString(this);
        }


        public virtual void Add(T item)
        {
            throw new NotImplementedException(Strings.MustOverrideOrReimplement);
        }

        public abstract void Clear();

        public abstract bool Remove(T item);

        public virtual bool Contains(T item)
        {
            IEqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
            foreach (T i in this)
            {
                if (equalityComparer.Equals(i, item))
                    return true;
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            int count = Count;

            if (count == 0)
                return;

            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, Strings.ArgMustNotBeNegative);
            if (index >= array.Length || count > array.Length - index)
                throw new ArgumentException("index", Strings.ArrayTooSmall);

            int i = 0;
            //TODO: Look into this
            foreach (object o in (ICollection)this)
            {
                if (i >= count)
                    break;

                array.SetValue(o, index);
                ++index;
                ++i;
            }
        }

        public abstract int Count { get; }

        bool ICollection<T>.IsReadOnly => false;

        public abstract IEnumerator<T> GetEnumerator();

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (T item in this)
            {
                yield return item;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int count = Count;

            if (count == 0)
                return;

            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, Strings.ArgMustNotBeNegative);
            if (arrayIndex >= array.Length || count > array.Length - arrayIndex)
                throw new ArgumentException("index", Strings.ArrayTooSmall);

            int i = 0;
            //TODO: Look into this
            foreach (object o in (ICollection)this)
            {
                if (i >= count)
                    break;

                array.SetValue(o, arrayIndex);
                ++arrayIndex;
                ++i;
            }
        }
    }
}
