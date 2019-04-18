using System;
using System.Collections.Generic;
using Android.Graphics;
using PdfViewer.Exceptions;
using PdfViewer.PDFium;
using PdfViewer.PDFium.Utils;
using PdfViewer.Util;

namespace PdfViewer
{
    public class PdfFile
    {
        private static readonly object Obj = new object();
        private PdfDocument pdfDocument;
        private readonly PdfiumCore pdfiumCore;

        public int PagesCount { get; private set; }

        /** Original page sizes */
        private readonly List<Size> originalPageSizes = new List<Size>();

        /** Scaled page sizes */
        private readonly List<SizeF> pageSizes = new List<SizeF>();

        /** Opened pages with indicator whether opening was successful */
        private readonly Android.Util.SparseBooleanArray openedPages = new Android.Util.SparseBooleanArray();

        /** Page with maximum width */
        private Size originalMaxWidthPageSize = new Size();

        /** Page with maximum height */
        private Size originalMaxHeightPageSize = new Size();

        /** Scaled page with maximum height */
        private SizeF maxHeightPageSize = new SizeF(0, 0);

        /** Scaled page with maximum width */
        private SizeF maxWidthPageSize = new SizeF(0, 0);

        /** True if scrolling is vertical, else it's horizontal */
        private readonly bool isVertical;

        /** Fixed spacing between pages in pixels */
        private readonly int spacingPx;

        /** Calculate spacing automatically so each page fits on it's own in the center of the view */
        private readonly bool autoSpacing;

        /** Calculated offsets for pages */
        private readonly List<float> pageOffsets = new List<float>();

        /** Calculated auto spacing for pages */
        private readonly List<float> pageSpacing = new List<float>();

        /** Calculated document length (width or height, depending on swipe mode) */
        private float documentLength;

        private readonly FitPolicy pageFitPolicy;

        /**
         * The pages the user want to display in order
         * (ex: 0, 2, 2, 8, 8, 1, 1, 1)
         */
        private int[] originalUserPages;

        public PdfFile(PdfiumCore pdfiumCore, PdfDocument pdfDocument, FitPolicy pageFitPolicy, Size viewSize,
            int[] originalUserPages,
            bool isVertical, int spacing, bool autoSpacing)
        {
            this.pdfiumCore = pdfiumCore;
            this.pdfDocument = pdfDocument;
            this.pageFitPolicy = pageFitPolicy;
            this.originalUserPages = originalUserPages;
            this.isVertical = isVertical;
            this.spacingPx = spacing;
            this.autoSpacing = autoSpacing;
            Setup(viewSize);
        }

        private void Setup(Size viewSize)
        {
            PagesCount = originalUserPages?.Length ?? pdfiumCore.GetPageCount(pdfDocument);

            for (var i = 0; i < PagesCount; i++)
            {
                var pageSize = pdfiumCore.GetPageSize(pdfDocument, DocumentPage(i));
                if (pageSize.Width > originalMaxWidthPageSize.Width)
                {
                    originalMaxWidthPageSize = pageSize;
                }

                if (pageSize.Height > originalMaxHeightPageSize.Height)
                {
                    originalMaxHeightPageSize = pageSize;
                }

                originalPageSizes.Add(pageSize);
            }

            RecalculatePageSizes(viewSize);
        }

        public void RecalculatePageSizes(Size viewSize)
        {
            pageSizes.Clear();
            var calculator = new PageSizeCalculator(pageFitPolicy, originalMaxWidthPageSize,
                originalMaxHeightPageSize, viewSize);
            maxWidthPageSize = calculator.GetOptimalMaxWidthPageSize();
            maxHeightPageSize = calculator.GetOptimalMaxHeightPageSize();

            foreach (var size in originalPageSizes)
            {
                pageSizes.Add(calculator.Calculate(size));
            }

            if (autoSpacing)
            {
                PrepareAutoSpacing(viewSize);
            }

            PrepareDocLen();
            PreparePagesOffset();
        }

        public SizeF GetPageSize(int pageIndex)
        {
            var docPage = DocumentPage(pageIndex);
            return docPage < 0 ? new SizeF(0, 0) : pageSizes[pageIndex];
        }

        public SizeF GetScaledPageSize(int pageIndex, float zoom)
        {
            var size = GetPageSize(pageIndex);
            return new SizeF(size.Width * zoom, size.Height * zoom);
        }

        public SizeF MaxPageSize => isVertical ? maxWidthPageSize : maxHeightPageSize;

        public float MaxPageWidth => MaxPageSize.Width;

        public float MaxPageHeight => MaxPageSize.Height;

        private void PrepareAutoSpacing(Size viewSize)
        {
            pageSpacing.Clear();
            for (var i = 0; i < PagesCount; i++)
            {
                var pageSize = pageSizes[i];
                var spacing = Math.Max(0,
                    isVertical ? viewSize.Height - pageSize.Height : viewSize.Width - pageSize.Width);
                if (i < PagesCount - 1)
                {
                    spacing += spacingPx;
                }

                pageSpacing.Add(spacing);
            }
        }

        private void PrepareDocLen()
        {
            float length = 0;
            for (var i = 0; i < PagesCount; i++)
            {
                var pageSize = pageSizes[i];
                length += isVertical ? pageSize.Height : pageSize.Width;
                if (autoSpacing)
                {
                    length += pageSpacing[i];
                }
                else if (i < PagesCount - 1)
                {
                    length += spacingPx;
                }
            }

            documentLength = length;
        }

        private void PreparePagesOffset()
        {
            pageOffsets.Clear();
            float offset = 0;
            for (var i = 0; i < PagesCount; i++)
            {
                var pageSize = pageSizes[i];
                var size = isVertical ? pageSize.Height : pageSize.Width;
                if (autoSpacing)
                {
                    offset += pageSpacing[i] / 2f;
                    if (i == 0)
                    {
                        offset -= spacingPx / 2f;
                    }
                    else if (i == PagesCount - 1)
                    {
                        offset += spacingPx / 2f;
                    }

                    pageOffsets.Add(offset);
                    offset += size + pageSpacing[i] / 2f;
                }
                else
                {
                    pageOffsets.Add(offset);
                    offset += size + spacingPx;
                }
            }
        }

        public float GetDocLen(float zoom)
        {
            return documentLength * zoom;
        }

        /**
         * Get the page's height if swiping vertical, or width if swiping horizontal.
         */
        public float GetPageLength(int pageIndex, float zoom)
        {
            var size = GetPageSize(pageIndex);
            return (isVertical ? size.Height : size.Width) * zoom;
        }

        public float GetPageSpacing(int pageIndex, float zoom)
        {
            var spacing = autoSpacing ? pageSpacing[pageIndex] : spacingPx;
            return spacing * zoom;
        }

        /** Get primary page offset, that is Y for vertical scroll and X for horizontal scroll */
        public float GetPageOffset(int pageIndex, float zoom)
        {
            var docPage = DocumentPage(pageIndex);
            if (docPage < 0)
            {
                return 0;
            }

            return pageOffsets[pageIndex] * zoom;
        }

        /** Get secondary page offset, that is X for vertical scroll and Y for horizontal scroll */
        public float GetSecondaryPageOffset(int pageIndex, float zoom)
        {
            var pageSize = GetPageSize(pageIndex);
            if (isVertical)
            {
                var maxWidth = MaxPageWidth;
                return zoom * (maxWidth - pageSize.Width) / 2; //x
            }

            var maxHeight = MaxPageHeight;
            return zoom * (maxHeight - pageSize.Height) / 2; //y
        }

        public int GetPageAtOffset(float offset, float zoom)
        {
            var currentPage = 0;
            for (var i = 0; i < PagesCount; i++)
            {
                var off = pageOffsets[i] * zoom - GetPageSpacing(i, zoom) / 2f;
                if (off >= offset)
                {
                    break;
                }

                currentPage++;
            }

            return --currentPage >= 0 ? currentPage : 0;
        }

        public bool OpenPage(int pageIndex)
        {
            var docPage = DocumentPage(pageIndex);
            if (docPage < 0)
            {
                return false;
            }

            lock (Obj)
            {
                if (openedPages.IndexOfKey(docPage) >= 0) return false;
                try
                {
                    pdfiumCore.OpenPage(pdfDocument, docPage);
                    openedPages.Put(docPage, true);
                    return true;
                }
                catch (Java.Lang.Exception e)
                {
                    openedPages.Put(docPage, false);
                    throw new PageRenderingException(pageIndex, e);
                }
            }
        }

        public bool PageHasError(int pageIndex)
        {
            var docPage = DocumentPage(pageIndex);
            return !openedPages.Get(docPage, false);
        }

        public void RenderPageBitmap(Bitmap bitmap, int pageIndex, Rect bounds, bool annotationRendering)
        {
            var docPage = DocumentPage(pageIndex);
            pdfiumCore.RenderPageBitmap(pdfDocument, bitmap, docPage,
                bounds.Left, bounds.Top, bounds.Width(), bounds.Height(), annotationRendering);
        }

        public PdfDocument.Meta MetaData => pdfDocument == null ? null : pdfiumCore.GetDocumentMeta(pdfDocument);

        public List<PdfDocument.Bookmark> Bookmarks => pdfDocument == null
            ? new List<PdfDocument.Bookmark>()
            : pdfiumCore.GetTableOfContents(pdfDocument);

        public List<PdfDocument.Link> GetPageLinks(int pageIndex)
        {
            var docPage = DocumentPage(pageIndex);
            return pdfiumCore.GetPageLinks(pdfDocument, docPage);
        }

        public RectF MapRectToDevice(int pageIndex, int startX, int startY, int sizeX, int sizeY,
            RectF rect)
        {
            var docPage = DocumentPage(pageIndex);
            return pdfiumCore.MapRectToDevice(pdfDocument, docPage, startX, startY, sizeX, sizeY, 0, rect);
        }

        public void Dispose()
        {
            if (pdfiumCore != null && pdfDocument != null)
            {
                pdfiumCore.CloseDocument(pdfDocument);
            }

            pdfDocument = null;
            originalUserPages = null;
        }

/**
 * Given the UserPage number, this method restrict it
 * to be sure it's an existing page. It takes care of
 * using the user defined pages if any.
 *
 * @param userPage A page number.
 * @return A restricted valid page number (example : -2 => 0)
 */
        public int DetermineValidPageNumberFrom(int userPage)
        {
            if (userPage <= 0)
            {
                return 0;
            }

            if (originalUserPages != null)
            {
                if (userPage >= originalUserPages.Length)
                {
                    return originalUserPages.Length - 1;
                }
            }
            else
            {
                if (userPage >= PagesCount)
                {
                    return PagesCount - 1;
                }
            }

            return userPage;
        }

        public int DocumentPage(int userPage)
        {
            var documentPage = userPage;
            if (originalUserPages != null)
            {
                if (userPage < 0 || userPage >= originalUserPages.Length)
                {
                    return -1;
                }

                documentPage = originalUserPages[userPage];
            }

            if (documentPage < 0 || userPage >= PagesCount)
            {
                return -1;
            }

            return documentPage;
        }
    }
}