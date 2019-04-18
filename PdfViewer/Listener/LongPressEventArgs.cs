using System;
using Android.Views;

namespace PdfViewer.Listener
{
   public class LongPressEventArgs:EventArgs
    {
        public LongPressEventArgs(MotionEvent motionEvent)
        {
            MotionEvent = motionEvent;
        }
        public MotionEvent MotionEvent;
    }
}