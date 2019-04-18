using Java.IO;

namespace PdfViewer.PDFium
{
    public class PdfPasswordException : IOException
    {
        public PdfPasswordException() : base()
        {
        }

        public PdfPasswordException(string detailMessage) : base(detailMessage)
        {
        }
    }
}