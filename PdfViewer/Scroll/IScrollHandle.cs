

namespace PdfViewer.Scroll
{
    public interface IScrollHandle
    {
        /**
    * Used to move the handle, called internally by PDFView
    *
    * @param position current scroll ratio between 0 and 1
    */
        void SetScroll(float position);

        /**
         * Method called by PDFView after setting scroll handle.
         * Do not call this method manually.
         * For usage sample see {@link DefaultScrollHandle}
         *
         * @param pdfView PDFView instance
         */
        void SetupLayout(PdfView pdfView);

        /**
         * Method called by PDFView when handle should be removed from layout
         * Do not call this method manually.
         */
        void DestroyLayout();

        /**
         * Set page number displayed on handle
         *
         * @param pageNum page number
         */
        void SetPageNum(int pageNum);

        /**
         * Get handle visibility
         *
         * @return true if handle is visible, false otherwise
         */
        bool Shown();

        /**
         * Show handle
         */
        void Show();

        /**
         * Hide handle immediately
         */
        void Hide();

        /**
         * Hide handle after some time (defined by implementation)
         */
        void HideDelayed();
    }
}