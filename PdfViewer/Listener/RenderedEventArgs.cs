using System;

namespace PdfViewer.Listener
{
   public class RenderedEventArgs: EventArgs
    {
        public RenderedEventArgs(int nbPages)
        {
            NbPages = nbPages;
        }

        public int NbPages { get;}
    }
}