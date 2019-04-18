using System;
using Java.Lang;

namespace PdfViewer.Listener
{
    public class PageErrorEventArgs:EventArgs
    {
        public PageErrorEventArgs(int page, Throwable throwable)
        {
            Page = page;
            Throwable = throwable;
        }
        public int Page { get; }
        public Throwable Throwable { get; }
    }
}