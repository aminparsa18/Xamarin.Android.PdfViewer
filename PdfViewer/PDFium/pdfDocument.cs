using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.OS;

namespace PdfViewer.PDFium
{
    public class PdfDocument
    {
        public class Meta
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public string Subject { get; set; }
            public string Keywords { get; set; }
            public string Creator { get; set; }
            public string Producer { get; set; }
            public string CreationDate { get; set; }
            public string ModDate { get; set; }
        }

        public class Bookmark
        {
            public List<Bookmark> Children = new List<Bookmark>();
            public string Title;
            public long PageIdx;
            public IntPtr MNativePtr;
            public bool HasChildren=> Children.Any();
        }

        public class Link
        {
            public RectF Bounds { get; }
            public int DestPageIdx { get; }
            public string Uri { get; }

            public Link(RectF bounds, int destPageIdx, string uri)
            {
                this.Bounds = bounds;
                this.DestPageIdx = destPageIdx;
                this.Uri = uri;
            }
        }

        public readonly Dictionary<int, IntPtr> MNativePagesPtr = new Dictionary<int, IntPtr>();

        public IntPtr MNativeDocPtr { get; set; }

        public ParcelFileDescriptor ParcelFileDescriptor { get; set; }

        public bool HasPage(int index)
        {
            return MNativePagesPtr.ContainsKey(index);
        }
    }
}