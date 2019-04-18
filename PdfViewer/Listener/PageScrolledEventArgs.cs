using System;

namespace PdfViewer.Listener
{
    public class PageScrolledEventArgs : EventArgs
    {
        public PageScrolledEventArgs(int page, float positionOffset)
        {
            Page = page;
            PositionOffset = positionOffset;
        }

        public int Page { get; }
        public float PositionOffset { get; }
    }
}