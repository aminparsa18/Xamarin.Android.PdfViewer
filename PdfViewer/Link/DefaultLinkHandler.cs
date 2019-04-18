using Android.Content;
using Android.Util;
using PdfViewer.Model;

namespace PdfViewer.Link
{
    public class DefaultLinkHandler : ILinkHandler
    {
        private static readonly string Tag = typeof(DefaultLinkHandler).Name;

        private readonly PdfView pdfView;

        public DefaultLinkHandler(PdfView pdfView)
        {
            this.pdfView = pdfView;
        }

        private void HandleUri(string uri)
        {
            var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(uri));
            var context = pdfView.Context;
            if (intent.ResolveActivity(context.PackageManager) != null)
            {
                context.StartActivity(intent);
            }
            else
            {
                Log.Warn(Tag, "No activity found for URI: " + uri);
            }
        }

        private void HandlePage(int page)
        {
            pdfView.JumpTo(page);
        }

        public void HandleLinkEvent(LinkTapEvent e)
        {
            var uri = e.Link.Uri;
            var page = e.Link.DestPageIdx;
            if (uri != null && !string.IsNullOrEmpty(uri))
            {
                HandleUri(uri);
            }
            else
            {
                HandlePage(page);
            }
        }
    }
}