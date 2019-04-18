using PdfViewer.Model;

namespace PdfViewer.Link
{
    public interface ILinkHandler
    {

        void HandleLinkEvent(LinkTapEvent e);
        }
    }