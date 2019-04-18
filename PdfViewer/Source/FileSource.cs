using Android.Content;
using Android.OS;
using Java.IO;
using PdfViewer.PDFium;

namespace PdfViewer.Source
{
    public class FileSource:DocumentSource
    {
        private readonly File file;

        public FileSource(File file)
        {
            this.file = file;
        }
        public override PdfDocument CreateDocument(Context context, PdfiumCore core, string password)
        {
            return core.NewDocument(ParcelFileDescriptor.Open(file, ParcelFileMode.ReadOnly), password);
        }
    }
}