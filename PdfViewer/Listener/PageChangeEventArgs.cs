using System;

namespace PdfViewer.Listener
{
    public class PageChangeEventArgs : EventArgs
    {
        public PageChangeEventArgs(int page, int pageCount)
        {
            Page = page;
            PageCount = pageCount;
        }

        public int Page { get; }
        public int PageCount { get; }
    }
}