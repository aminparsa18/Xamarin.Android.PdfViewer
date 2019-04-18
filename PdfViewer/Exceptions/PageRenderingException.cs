using Java.Lang;

namespace PdfViewer.Exceptions
{
   public class PageRenderingException:Exception
    {
        public PageRenderingException(int page, Throwable cause) : base(cause)
        {
            this.Page = page;
        }

        public int Page { get; }
    }
}