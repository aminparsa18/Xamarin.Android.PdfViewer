using Android.Graphics;
using PdfViewer.PDFium;

namespace PdfViewer.Model
{
    public class LinkTapEvent
    {
        public LinkTapEvent(float originalX, float originalY, float documentX, float documentY, RectF mappedLinkRect, PdfDocument.Link link)
        {
            this.OriginalX = originalX;
            this.OriginalY = originalY;
            this.DocumentX = documentX;
            this.DocumentY = documentY;
            this.MappedLinkRect = mappedLinkRect;
            this.Link = link;
        }

        public float OriginalX { get; }
        public float OriginalY { get; }
        public float DocumentX { get; }
        public float DocumentY { get; }
        public RectF MappedLinkRect { get; }
        public PdfDocument.Link Link { get; }
    }
}