using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfViewer.Util.OrderedSet
{
    public static class Algorithms
    {
        [Serializable]
        private class TypedEnumerator<T> : IEnumerator<T>
        {
            private readonly IEnumerator<T> wrappedEnumerator;
            public TypedEnumerator(IEnumerator<T> wrappedEnumerator)
            {
                this.wrappedEnumerator = wrappedEnumerator;
            }
            void IDisposable.Dispose()
            {
                wrappedEnumerator?.Dispose();
            }

            bool IEnumerator.MoveNext()
            {
                return wrappedEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                wrappedEnumerator.Reset();
            }

            object IEnumerator.Current => wrappedEnumerator.Current;

            public T Current => wrappedEnumerator.Current;
        }

        [Serializable]
        private class TypedEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable wrappedEnumerable;
            public TypedEnumerable(IEnumerable wrappedEnumerable)
            {
                this.wrappedEnumerable = wrappedEnumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new TypedEnumerator<T>(wrappedEnumerable.Cast<T>().GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return wrappedEnumerable.GetEnumerator();
            }
        }

        public static IEnumerable<T> TypedAs<T>(IEnumerable untypedCollection)
        {
            if (untypedCollection == null)
                return null;
            if (untypedCollection is IEnumerable<T>)
                return (IEnumerable<T>) untypedCollection;
            return new TypedEnumerable<T>(untypedCollection);
        }

        public static string ToString<T>(IEnumerable<T> collection)
        {
            return ToString(collection, true, "{", ",", "}");
        }

        public static string ToString<T>(IEnumerable<T> collection, bool recursive, string start, string separator,
            string end)
        {
            if (start == null)
                throw new ArgumentNullException(nameof(start));
            if (separator == null)
                throw new ArgumentNullException(nameof(separator));
            if (end == null)
                throw new ArgumentNullException(nameof(end));

            if (collection == null)
                return "null";

            bool firstItem = true;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append(start);

            // Call ToString on each item and put it in.
            foreach (T item in collection)
            {
                if (!firstItem)
                    builder.Append(separator);

                if (item == null)
                    builder.Append("null");
                else if (recursive && item is IEnumerable && !(item is string))
                    builder.Append(ToString(TypedAs<object>((IEnumerable) item), recursive, start, separator, end));
                else
                    builder.Append(item.ToString());

                firstItem = false;
            }

            builder.Append(end);
            return builder.ToString();
        }
    }
}