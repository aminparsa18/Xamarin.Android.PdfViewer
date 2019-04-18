using System;
using Android.Graphics;


namespace PdfViewer.Listener
{
    public class DrawEventArgs:EventArgs
    {
        public DrawEventArgs(Canvas canvas, float pageWidth, float pageHeight, int displayedPage)
        {
            Canvas = canvas;
            PageWidth = pageWidth;
            PageHeight = pageHeight;
            DisplayPage = displayedPage;
        }
        public Canvas Canvas { get; }
        public float PageWidth { get; }
        public float PageHeight { get; }
        public int DisplayPage { get; }
    }
}