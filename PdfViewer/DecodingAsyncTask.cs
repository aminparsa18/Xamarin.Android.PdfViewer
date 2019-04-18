using System.Threading;
using System.Threading.Tasks;
using Java.Lang;
using PdfViewer.PDFium;
using PdfViewer.Source;
using PdfViewer.PDFium.Utils;

namespace PdfViewer
{
    public class DecodingAsyncTask
    {
        private readonly bool cancelled;
        private readonly PdfView pdfView;
        private readonly PdfiumCore pdfiumCore;
        private readonly string password;
        private readonly DocumentSource docSource;
        private readonly int[] userPages;
        private PdfFile pdfFile;
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly CancellationToken token;

        public DecodingAsyncTask(DocumentSource docSource, string password, int[] userPages, PdfView pdfView,
            PdfiumCore pdfiumCore)
        {
            this.docSource = docSource;
            this.userPages = userPages;
            this.cancelled = false;
            this.pdfView = pdfView;
            this.password = password;
            this.pdfiumCore = pdfiumCore;
            token = tokenSource.Token;
        }

        public async void Run()
        {
            var throwable = await Task<Throwable>.Factory.StartNew(() =>
            {
                try
                {
                    var pdfDocument = docSource.CreateDocument(pdfView.Context, pdfiumCore, password);
                    // We assume all the pages are the same size
                    pdfFile = new PdfFile(pdfiumCore, pdfDocument, pdfView.PageFitPolicy, ViewSize,
                        userPages, pdfView.IsSwipeVertical, pdfView.SpacingPx, pdfView.AutoSpacing);
                    return null;
                }
                catch (Throwable t)
                {
                    return t;
                }
            }, token);
            if (throwable != null)
            pdfView.LoadError(throwable);

            if (!cancelled)
            {
                pdfView.LoadComplete(pdfFile);
            }
        }

        public void Cancel()
        {
            tokenSource.Cancel();
        }

        private Size ViewSize => new Size
        {
            Width = pdfView.Width,
            Height = pdfView.Height
        };
    }
}