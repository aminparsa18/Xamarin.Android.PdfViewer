using Android.Content;
using PdfViewer.PDFium;

namespace PdfViewer.Source
{
    public class UriSource:DocumentSource
    {
        private readonly Android.Net.Uri uri;

        public UriSource(Android.Net.Uri uri)
        {
            this.uri = uri;
        }
        public override PdfDocument CreateDocument(Context context, PdfiumCore core, string password)
        {
            return core.NewDocument(context.ContentResolver.OpenFileDescriptor(uri, "r"), password);
        }
    }
}