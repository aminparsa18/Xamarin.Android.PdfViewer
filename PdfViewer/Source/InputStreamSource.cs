using Android.Content;
using Java.IO;
using PdfViewer.PDFium;

namespace PdfViewer.Source
{
    public class InputStreamSource:DocumentSource
    {
        private readonly InputStream inputStream;

        public InputStreamSource(InputStream inputStream)
        {
            this.inputStream = inputStream;
        }
        public override PdfDocument CreateDocument(Context context, PdfiumCore core, string password)
        {
            return core.NewDocument(Util.Util.ToByteArray(inputStream), password);
        }
    }
}