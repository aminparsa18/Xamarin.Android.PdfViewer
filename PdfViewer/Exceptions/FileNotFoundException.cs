using Java.Lang;

namespace PdfViewer.Exceptions
{
   public class FileNotFoundException: RuntimeException
    {
        public FileNotFoundException(string detailMessage) : base(detailMessage){}

        public FileNotFoundException(string detailMessage, Throwable throwable):base(detailMessage, throwable) {}
    }
}