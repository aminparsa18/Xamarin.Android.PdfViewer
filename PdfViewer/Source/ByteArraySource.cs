using Android.Content;
using PdfViewer.PDFium;

namespace PdfViewer.Source
{
    public class ByteArraySource : DocumentSource
    {
        private readonly byte[] data;

        public ByteArraySource(byte[] data)
        {
            this.data = data;
        }

        public override PdfDocument CreateDocument(Context context, PdfiumCore core, string password)
        {
            return core.NewDocument(data, password);
        }
    }
}