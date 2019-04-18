using System;
using Java.Lang;

namespace PdfViewer.Listener
{
    public class ErrorEventArgs:EventArgs
    {
        public ErrorEventArgs(Throwable throwable)
        {
            Throwable = throwable;
        }
        public Throwable Throwable { get; }
    }
}