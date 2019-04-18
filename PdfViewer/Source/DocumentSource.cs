using Android.Content;
using PdfViewer.PDFium;

namespace PdfViewer.Source
{
   public abstract class DocumentSource
    {
       public abstract PdfDocument CreateDocument(Context context, PdfiumCore core, string password);
    }
}