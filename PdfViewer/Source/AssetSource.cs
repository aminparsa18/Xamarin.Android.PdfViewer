using Android.Content;
using Android.OS;
using PdfViewer.PDFium;
using PdfViewer.Util;

namespace PdfViewer.Source
{
   public class AssetSource : DocumentSource
    {

        private readonly string assetName;
        public AssetSource(string assetName)
        {
            this.assetName = assetName;
        }

        public override PdfDocument CreateDocument(Context context, PdfiumCore core, string password)
        {
            return core.NewDocument(ParcelFileDescriptor.Open(FileUtils.FileFromAssetAsync(context, assetName),
                ParcelFileMode.ReadOnly));
        }
    }
}