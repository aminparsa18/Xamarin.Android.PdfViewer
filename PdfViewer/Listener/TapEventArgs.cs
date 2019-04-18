using System;
using Android.Views;

namespace PdfViewer.Listener
{
    public class TapEventArgs:EventArgs
    {
        public TapEventArgs(MotionEvent motionEvent)
        {
            MotionEvent = motionEvent;
        }
        public MotionEvent MotionEvent;
        public bool Handled { get; set; }
    }
}