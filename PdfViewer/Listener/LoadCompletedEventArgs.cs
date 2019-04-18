using System;

namespace PdfViewer.Listener
{
   public class LoadCompletedEventArgs:EventArgs
    {
        public LoadCompletedEventArgs(int nbPages)
        {
            NbPages = nbPages;
        }
        public int NbPages { get; }
    }
}