using Android.Graphics;

namespace PdfViewer.Model
{
    public class PagePart
    {
        public int Page { get; set; }
        public Bitmap RenderedBitmap { get; set; }
        public RectF PageRelativeBounds { get; set; }
        public bool Thumbnail { get; set; }
        public int CacheOrder { get; set; }

        public PagePart(int page, Bitmap renderedBitmap, RectF pageRelativeBounds, bool thumbnail,
            int cacheOrder)
        {
            this.Page = page;
            this.RenderedBitmap = renderedBitmap;
            this.PageRelativeBounds = pageRelativeBounds;
            this.Thumbnail = thumbnail;
            this.CacheOrder = cacheOrder;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PagePart))
            {
                return false;
            }

            var part = (PagePart) obj;
            return part.Page == Page
                   && part.PageRelativeBounds.Left == PageRelativeBounds.Left
                   && part.PageRelativeBounds.Right == PageRelativeBounds.Right
                   && part.PageRelativeBounds.Top == PageRelativeBounds.Top
                   && part.PageRelativeBounds.Bottom == PageRelativeBounds.Bottom;
        }
    }
}