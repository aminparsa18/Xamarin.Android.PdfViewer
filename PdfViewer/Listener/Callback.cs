using System;
using Android.Views;
using PdfViewer.Link;
using PdfViewer.Model;

namespace PdfViewer.Listener
{
    public class Callbacks
    {
        /**
         * Call back object to call when the PDF is loaded
         */
        public EventHandler<LoadCompletedEventArgs> OnLoadCompleted { get; set; }

        /**
         * Call back object to call when document loading error occurs
         */
        public EventHandler<ErrorEventArgs> OnError { get; set; }

        /**
         * Call back object to call when the page load error occurs
         */
        public EventHandler<PageErrorEventArgs> OnPageError { get; set; }

        /**
         * Call back object to call when the document is initially rendered
         */
        public EventHandler<RenderedEventArgs> OnRendered { get; set; }

        /**
         * Call back object to call when the page has changed
         */
        public EventHandler<PageChangeEventArgs> OnPageChanged { get; set; }

        /**
         * Call back object to call when the page is scrolled
         */
        public EventHandler<PageScrolledEventArgs> OnPageScrolled { get; set; }

        /**
         * Call back object to call when the above layer is to drawn
         */
        public EventHandler<DrawEventArgs> OnDraw { get; set; }

        public EventHandler<DrawEventArgs> OnDrawAll { get; set; }

        /**
         * Call back object to call when the user does a tap gesture
         */
        public EventHandler<TapEventArgs> OnTap { get; set; }
    
        /**
         * Call back object to call when the user does a long tap gesture
         */
        public EventHandler<LongPressEventArgs> OnLongPress { get; set; }

        /**
         * Call back object to call when clicking link
         */
        private ILinkHandler linkHandler;

        public void CallOnLoadComplete(object sender,LoadCompletedEventArgs args)
        {
            OnLoadCompleted?.Invoke(sender,args);
        }

        public bool CallOnPageError(object sender,PageErrorEventArgs args)
        {
            if (OnPageError == null) return false;
            OnPageError.Invoke(sender,args);
            return true;
        }

        public void CallOnRender(object sender,RenderedEventArgs args)
        {
            OnRendered?.Invoke(sender,args);
        }


        public void CallOnPageChange(object sender, PageChangeEventArgs e)
        {
            OnPageChanged?.Invoke(sender, e);
        }

        public void CallOnPageScroll(object sender,PageScrolledEventArgs args)
        {
            OnPageScrolled?.Invoke( sender,args);
        }

        public bool CallOnTap(object sender,TapEventArgs args)
        {
            return OnTap != null && args.Handled;
        }

        public void CallOnLongPress(MotionEvent e)
        {
            OnLongPress?.Invoke(this, new LongPressEventArgs(e));
        }

        public void SetLinkHandler(ILinkHandler linkHandler)
        {
            this.linkHandler = linkHandler;
        }

        public void CallLinkHandler(LinkTapEvent e)
        {
            linkHandler?.HandleLinkEvent(e);
        }
    }
}