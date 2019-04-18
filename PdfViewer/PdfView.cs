using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using Java.IO;
using Java.Lang;
using PdfViewer.Exceptions;
using PdfViewer.Link;
using PdfViewer.Listener;
using PdfViewer.Model;
using PdfViewer.PDFium;
using PdfViewer.Scroll;
using PdfViewer.Source;
using PdfViewer.Util;
using Size = PdfViewer.PDFium.Utils.Size;
using SizeF = PdfViewer.PDFium.Utils.SizeF;

namespace PdfViewer
{
    public sealed class PdfView : RelativeLayout
    {
        private static readonly string TAG = typeof(PdfView).Name;

        public static float DefaultMaxScale = 3.0f;
        public static float DefaultMidScale = 1.75f;
        public static float DefaultMinScale = 1.0f;

        /**
         * START - scrolling in first page direction
         * END - scrolling in last page direction
         * NONE - not scrolling
         */
        public enum ScrollDir
        {
            None,
            Start,
            End
        }

        private ScrollDir scrollDir = ScrollDir.None;

        /**
         * Rendered parts go to the cache manager
         */
        public CacheManager CacheManager;

        /**
         * Animation manager manage all offset and zoom animation
         */
        private readonly AnimationManager animationManager;

        /**
         * Drag manager manage all touch events
         */
        private readonly DragPinchManager dragPinchManager;

        public PdfFile PdfFile;

        /**
         * Current state of the view
         */
        private State state = State.Default;

        /**
         * Async task used during the loading phase to decode a PDF document
         */
        private DecodingAsyncTask decodingAsyncTask;

        /**
         * The thread {@link #renderingHandler} will run on
         */
        private readonly HandlerThread renderingHandlerThread;

        /**
         * Handler always waiting in the background and rendering tasks
         */
        public RenderingHandler RenderingHandler;

        public PagesLoader PagesLoader;

        public Callbacks Callbacks = new Callbacks();

        /**
         * Paint object for drawing
         */
        private readonly Paint paint;

        /**
         * Paint object for drawing debug stuff
         */
        private readonly Paint debugPaint;

        /** Policy for fitting pages to screen */
        public FitPolicy PageFitPolicy { get; set; } = FitPolicy.Width;
        public bool PageSnap { get; set; } = true;
        public bool PageFling { get; set; } = true;
        public bool IsRecycled { get; private set; } = true;
        public int DefaultPage { get; set; }
        public int CurrentPage { get; set; }
        public float CurrentXOffset { get; set; }
        public float CurrentYOffset { get; set; }
        public float Zoom { get; set; } = 1f;
        public bool IsDoubletapEnabled { get; set; } = true;
        public float MinZoom { get; set; } = DefaultMinScale;


        public float MidZoom { get; set; } = DefaultMidScale;


        public float MaxZoom { get; set; } = DefaultMaxScale;

        public bool IsBestQuality { get; set; }

        public bool IsSwipeVertical { get; set; } = true;

        public bool IsSwipeEnabled { get; private set; } = true;

        public bool IsAnnotationRendering { get; set; }

        public bool IsAntialiasing { get; set; }

        public int SpacingPx { get; set; }
        private bool nightMode;


        /**
         * Pdfium core for loading and rendering PDFs
         */
        private readonly PdfiumCore pdfiumCore;

        public IScrollHandle ScrollHandle { get; set; }

        private bool isScrollHandleInit;


        /**
         * True if the view should render during scaling<br/>
         * Can not be forced on older API versions (< Build.VERSION_CODES.KITKAT) as the GestureDetector does
         * not detect scrolling while scaling.<br/>
         * False otherwise
         */
        public bool RenderDuringScale { get; set; }

        private readonly PaintFlagsDrawFilter antialiasFilter =
            new PaintFlagsDrawFilter(0, PaintFlags.AntiAlias | PaintFlags.FilterBitmap);

        /** Add dynamic spacing to fit each page separately on the screen. */
        public bool AutoSpacing { get; set; }

        /**
         * pages numbers used when calling onDrawAllListener
         */
        private readonly List<int> onDrawPagesNums = new List<int>(10);
        private bool hasSize;

        /** Holds last used Configurator that should be loaded when view has size */
        private Configurator waitingDocumentConfigurator;

        public PdfView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public PdfView(Context context) : base(context)
        {
        }


        public PdfView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            renderingHandlerThread = new HandlerThread("PDF renderer");

            if (IsInEditMode)
            {
                return;
            }

            CacheManager = new CacheManager();
            animationManager = new AnimationManager(this);
            dragPinchManager = new DragPinchManager(this, animationManager);
            PagesLoader = new PagesLoader(this);
            paint = new Paint();
            debugPaint = new Paint();
            debugPaint.SetStyle(Paint.Style.Stroke);

            pdfiumCore = new PdfiumCore(context);
            SetWillNotDraw(false);
        }

        public PdfView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public PdfView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs,
            defStyleAttr, defStyleRes)
        {
        }

        public void Load(DocumentSource docSource, string password)
        {
            Load(docSource, password, null);
        }

        public void Load(DocumentSource docSource, string password, int[] userPages)
        {
            if (!IsRecycled)
            {
                throw new IllegalStateException("Don't call load on a PDF View without recycling it first.");
            }

            IsRecycled = false;
            // Start decoding document
            decodingAsyncTask = new DecodingAsyncTask(docSource, password, userPages, this, pdfiumCore);
            decodingAsyncTask.Run();
        }

        public void JumpTo(int page, bool withAnimation)
        {
            if (PdfFile == null)
            {
                return;
            }

            page = PdfFile.DetermineValidPageNumberFrom(page);
            var offset = page == 0 ? 0 : -PdfFile.GetPageOffset(page, Zoom);
            if (IsSwipeVertical)
            {
                if (withAnimation)
                {
                    animationManager.StartYAnimation(CurrentYOffset, offset);
                }
                else
                {
                    MoveTo(CurrentXOffset, offset);
                }
            }
            else
            {
                if (withAnimation)
                {
                    animationManager.StartXAnimation(CurrentXOffset, offset);
                }
                else
                {
                    MoveTo(offset, CurrentYOffset);
                }
            }

            ShowPage(page);
        }

        public void JumpTo(int page)
        {
            JumpTo(page, false);
        }

        private void ShowPage(int pageNb)
        {
            if (IsRecycled)
            {
                return;
            }

            pageNb = PdfFile.DetermineValidPageNumberFrom(pageNb);
            CurrentPage = pageNb;

            LoadPages();

            if (ScrollHandle != null && !DocumentFitsView)
            {
                ScrollHandle.SetPageNum(CurrentPage + 1);
            }

            Callbacks.CallOnPageChange(this, new PageChangeEventArgs(CurrentPage, PdfFile.PagesCount));
        }

        public float PositionOffset
        {
            get
            {
                float offset;
                if (IsSwipeVertical)
                {
                    offset = -CurrentYOffset / (PdfFile.GetDocLen(Zoom) - Height);
                }
                else
                {
                    offset = -CurrentXOffset / (PdfFile.GetDocLen(Zoom) - Width);
                }

                return MathUtils.Limit(offset, 0, 1);
            }
        }

        /**
         * @param progress   must be between 0 and 1
         * @param moveHandle whether to move scroll handle
         * @see PDFView#getPositionOffset()
         */
        public void SetPositionOffset(float progress, bool moveHandle)
        {
            if (IsSwipeVertical)
            {
                MoveTo(CurrentXOffset, (-PdfFile.GetDocLen(Zoom) + Height) * progress, moveHandle);
            }
            else
            {
                MoveTo((-PdfFile.GetDocLen(Zoom) + Width) * progress, CurrentYOffset, moveHandle);
            }

            LoadPageByOffset();
        }

        public void StopFling()
        {
            animationManager.StopFling();
        }

        public int PageCount => PdfFile?.PagesCount ?? 0;

        public void SetNightMode(bool nightMode)
        {
            this.nightMode = nightMode;
            if (nightMode)
            {
                var colorMatrixInverted =
                    new ColorMatrix(new float[]
                    {
                        -1, 0, 0, 0, 255,
                        0, -1, 0, 0, 255,
                        0, 0, -1, 0, 255,
                        0, 0, 0, 1, 0
                    });

                var filter = new ColorMatrixColorFilter(colorMatrixInverted);
                paint.SetColorFilter(filter);
            }
            else
            {
                paint.SetColorFilter(null);
            }
        }

        

        public void SetOnPageError(PageRenderingException ex)
        {
            if (!Callbacks.CallOnPageError(this, new PageErrorEventArgs(ex.Page, ex.Cause)))
            {
                Log.Error(TAG, "Cannot open page " + ex.Page, ex.Cause);
            }
        }

        public void Recycle()
        {
            waitingDocumentConfigurator = null;
            animationManager.StopAll();
            dragPinchManager.Disable();
            // Stop tasks
            if (RenderingHandler != null)
            {
                RenderingHandler.Stop();
                RenderingHandler.RemoveMessages(RenderingHandler.MsgRenderTask);
            }

            decodingAsyncTask?.Cancel();

            // Clear caches
            CacheManager.Recycle();

            if (ScrollHandle != null && isScrollHandleInit)
            {
                ScrollHandle.DestroyLayout();
            }

            if (PdfFile != null)
            {
                PdfFile.Dispose();
                PdfFile = null;
            }

            RenderingHandler = null;
            ScrollHandle = null;
            isScrollHandleInit = false;
            CurrentXOffset = CurrentYOffset = 0;
            Zoom = 1f;
            IsRecycled = true;
            Callbacks = new Callbacks();
            state = State.Default;
        }

      

        public override void ComputeScroll()
        {
            base.ComputeScroll();
            if (IsInEditMode)
            {
                return;
            }

            animationManager.ComputeFling();
        }

        protected override void OnDetachedFromWindow()
        {
            Recycle();
            base.OnDetachedFromWindow();
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            hasSize = true;
            waitingDocumentConfigurator?.Load();

            if (IsInEditMode || state != State.Shown)
            {
                return;
            }

            animationManager.StopAll();
            PdfFile.RecalculatePageSizes(new Size() {Width = w, Height = h});
            if (IsSwipeVertical)
            {
                MoveTo(CurrentXOffset, -PdfFile.GetPageOffset(CurrentPage, Zoom));
            }
            else
            {
                MoveTo(-PdfFile.GetPageOffset(CurrentPage, Zoom), CurrentYOffset);
            }

            LoadPageByOffset();
        }

        public override bool CanScrollHorizontally(int direction)
        {
            if (PdfFile == null)
            {
                return true;
            }

            if (IsSwipeVertical)
            {
                if (direction < 0 && CurrentXOffset < 0)
                {
                    return true;
                }

                if (direction > 0 && CurrentXOffset + ToCurrentScale(PdfFile.MaxPageWidth) > Width)
                {
                    return true;
                }
            }
            else
            {
                if (direction < 0 && CurrentXOffset < 0)
                {
                    return true;
                }

                if (direction > 0 && CurrentXOffset + PdfFile.GetDocLen(Zoom) > Width)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool CanScrollVertically(int direction)
        {
            if (PdfFile == null)
            {
                return true;
            }

            if (IsSwipeVertical)
            {
                if (direction < 0 && CurrentYOffset < 0)
                {
                    return true;
                }

                if (direction > 0 && CurrentYOffset + PdfFile.GetDocLen(Zoom) > Height)
                {
                    return true;
                }
            }
            else
            {
                if (direction < 0 && CurrentYOffset < 0)
                {
                    return true;
                }

                if (direction > 0 && CurrentYOffset + ToCurrentScale(PdfFile.MaxPageHeight) > Height)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (IsInEditMode)
            {
                return;
            }

            if (IsAntialiasing)
            {
                canvas.DrawFilter = antialiasFilter;
            }

            var bg = Background;
            if (bg == null)
            {
                canvas.DrawColor(Color.White);
            }
            else
            {
                bg.Draw(canvas);
            }

            if (IsRecycled)
            {
                return;
            }

            if (state != State.Shown)
            {
                return;
            }

            // Moves the canvas before drawing any element
            var currentXOffset = this.CurrentXOffset;
            var currentYOffset = this.CurrentYOffset;
            canvas.Translate(currentXOffset, currentYOffset);

            // Draws thumbnails
            foreach (var part in CacheManager.GetThumbnails())
            {
                DrawPart(canvas, part);
            }

            // Draws parts
            foreach (var part in CacheManager.GetPageParts())
            {
                DrawPart(canvas, part);
                if (Callbacks.OnDrawAll != null
                    && !onDrawPagesNums.Contains(part.Page))
                {
                    onDrawPagesNums.Add(part.Page);
                }
            }

            foreach (var page in onDrawPagesNums)
            {
                DrawWithListener(canvas, page, Callbacks.OnDrawAll);
            }

            onDrawPagesNums.Clear();

            DrawWithListener(canvas, CurrentPage, Callbacks.OnDraw);

            // Restores the canvas position
            canvas.Translate(-currentXOffset, -currentYOffset);
        }

        private void DrawWithListener(Canvas canvas, int page, EventHandler<DrawEventArgs> listener)
        {
            if (listener == null) return;
            float translateX, translateY;
            if (IsSwipeVertical)
            {
                translateX = 0;
                translateY = PdfFile.GetPageOffset(page, Zoom);
            }
            else
            {
                translateY = 0;
                translateX = PdfFile.GetPageOffset(page, Zoom);
            }

            canvas.Translate(translateX, translateY);
            var size = PdfFile.GetPageSize(page);
            listener.Invoke(this, new DrawEventArgs(canvas,
                ToCurrentScale(size.Width),
                ToCurrentScale(size.Height),
                page));

            canvas.Translate(-translateX, -translateY);
        }

        private void DrawPart(Canvas canvas, PagePart part)
        {
            // Can seem strange, but avoid lot of calls
            var pageRelativeBounds = part.PageRelativeBounds;
            var renderedBitmap = part.RenderedBitmap;

            if (renderedBitmap.IsRecycled)
            {
                return;
            }

            // Move to the target page
            float localTranslationX = 0;
            float localTranslationY = 0;
            var size = PdfFile.GetPageSize(part.Page);
            if (IsSwipeVertical)
            {
                localTranslationY = PdfFile.GetPageOffset(part.Page, Zoom);
                var maxWidth = PdfFile.MaxPageWidth;
                localTranslationX = ToCurrentScale(maxWidth - size.Width) / 2;
            }
            else
            {
                localTranslationX = PdfFile.GetPageOffset(part.Page, Zoom);
                var maxHeight = PdfFile.MaxPageHeight;
                localTranslationY = ToCurrentScale(maxHeight - size.Height) / 2;
            }

            canvas.Translate(localTranslationX, localTranslationY);

            var srcRect = new Rect(0, 0, renderedBitmap.Width,
                renderedBitmap.Height);

            var offsetX = ToCurrentScale(pageRelativeBounds.Left * size.Width);
            var offsetY = ToCurrentScale(pageRelativeBounds.Top * size.Height);
            var width = ToCurrentScale(pageRelativeBounds.Width() * size.Width);
            var height = ToCurrentScale(pageRelativeBounds.Height() * size.Height);

            // If we use float values for this rectangle, there will be
            // a possible gap between page parts, especially when
            // the zoom level is high.
            var dstRect = new RectF((int) offsetX, (int) offsetY,
                (int) (offsetX + width),
                (int) (offsetY + height));

            // Check if bitmap is in the screen
            var translationX = CurrentXOffset + localTranslationX;
            var translationY = CurrentYOffset + localTranslationY;
            if (translationX + dstRect.Left >= Width || translationX + dstRect.Right <= 0 ||
                translationY + dstRect.Top >= Height || translationY + dstRect.Bottom <= 0)
            {
                canvas.Translate(-localTranslationX, -localTranslationY);
                return;
            }

            canvas.DrawBitmap(renderedBitmap, srcRect, dstRect, paint);

            if (Constants.DebugMode)
            {
                debugPaint.Color = part.Page % 2 == 0 ? Color.Red : Color.Blue;
                canvas.DrawRect(dstRect, debugPaint);
            }

            // Restore the canvas position
            canvas.Translate(-localTranslationX, -localTranslationY);
        }


        public void LoadPages()
        {
            if (PdfFile == null || RenderingHandler == null)
            {
                return;
            }

            // Cancel all current tasks
            RenderingHandler.RemoveMessages(RenderingHandler.MsgRenderTask);
            CacheManager.MakeANewSet();

            PagesLoader.LoadPages();
            Redraw();
        }

        /**
         * Called when the PDF is loaded
         */
        public void LoadComplete(PdfFile pdfFile)
        {
            state = State.Loaded;
            this.PdfFile = pdfFile;

            if (!renderingHandlerThread.IsAlive)
            {
                renderingHandlerThread.Start();
            }

            RenderingHandler = new RenderingHandler(renderingHandlerThread.Looper,
                this);
            RenderingHandler.Start();

            if (ScrollHandle != null)
            {
                ScrollHandle.SetupLayout(this);
                isScrollHandleInit = true;
            }

            dragPinchManager.Enable();

            Callbacks.CallOnLoadComplete(this, new LoadCompletedEventArgs(pdfFile.PagesCount));

            JumpTo(DefaultPage, false);
        }

        public void LoadError(Throwable t)
        {
            state = State.Error;
            var onErrorListener = Callbacks.OnError;
            Recycle();
            Invalidate();
            if (onErrorListener != null)
            {
                onErrorListener?.Invoke(this, new ErrorEventArgs(t));
            }
            else
            {
                Log.Error("PDFView", "load pdf error", t);
            }
        }

        private void Redraw()
        {
            Invalidate();
        }

        public void OnBitmapRendered(PagePart part)
        {
            // when it is first rendered part
            if (state == State.Loaded)
            {
                state = State.Shown;
                Callbacks.CallOnRender(this, new RenderedEventArgs(PdfFile.PagesCount));
            }

            if (part.Thumbnail)
            {
                CacheManager.CacheThumbnail(part);
            }
            else
            {
                CacheManager.CachePart(part);
            }

            Redraw();
        }

        public void MoveTo(float offsetX, float offsetY)
        {
            MoveTo(offsetX, offsetY, true);
        }

        /**
         * Move to the given X and Y offsets, but check them ahead of time
         * to be sure not to go outside the the big strip.
         *
         * @param offsetX    The big strip X offset to use as the left border of the screen.
         * @param offsetY    The big strip Y offset to use as the right border of the screen.
         * @param moveHandle whether to move scroll handle or not
         */
        public void MoveTo(float offsetX, float offsetY, bool moveHandle)
        {
            if (IsSwipeVertical)
            {
                // Check X offset
                var scaledPageWidth = ToCurrentScale(PdfFile.MaxPageWidth);
                if (scaledPageWidth < Width)
                {
                    offsetX = Width / 2 - scaledPageWidth / 2;
                }
                else
                {
                    if (offsetX > 0)
                    {
                        offsetX = 0;
                    }
                    else if (offsetX + scaledPageWidth < Width)
                    {
                        offsetX = Width - scaledPageWidth;
                    }
                }

                // Check Y offset
                var contentHeight = PdfFile.GetDocLen(Zoom);
                if (contentHeight < Height)
                {
                    // whole document height visible on screen
                    offsetY = (Height - contentHeight) / 2;
                }
                else
                {
                    if (offsetY > 0)
                    {
                        // top visible
                        offsetY = 0;
                    }
                    else if (offsetY + contentHeight < Height)
                    {
                        // bottom visible
                        offsetY = -contentHeight + Height;
                    }
                }

                if (offsetY < CurrentYOffset)
                {
                    scrollDir = ScrollDir.End;
                }
                else if (offsetY > CurrentYOffset)
                {
                    scrollDir = ScrollDir.Start;
                }
                else
                {
                    scrollDir = ScrollDir.None;
                }
            }
            else
            {
                // Check Y offset
                var scaledPageHeight = ToCurrentScale(PdfFile.MaxPageHeight);
                if (scaledPageHeight < Height)
                {
                    offsetY = Height / 2 - scaledPageHeight / 2;
                }
                else
                {
                    if (offsetY > 0)
                    {
                        offsetY = 0;
                    }
                    else if (offsetY + scaledPageHeight < Height)
                    {
                        offsetY = Height - scaledPageHeight;
                    }
                }

                // Check X offset
                var contentWidth = PdfFile.GetDocLen(Zoom);
                if (contentWidth < Width)
                {
                    // whole document width visible on screen
                    offsetX = (Width - contentWidth) / 2;
                }
                else
                {
                    if (offsetX > 0)
                    {
                        // left visible
                        offsetX = 0;
                    }
                    else if (offsetX + contentWidth < Width)
                    {
                        // right visible
                        offsetX = -contentWidth + Width;
                    }
                }

                if (offsetX < CurrentXOffset)
                {
                    scrollDir = ScrollDir.End;
                }
                else if (offsetX > CurrentXOffset)
                {
                    scrollDir = ScrollDir.Start;
                }
                else
                {
                    scrollDir = ScrollDir.None;
                }
            }

            CurrentXOffset = offsetX;
            CurrentYOffset = offsetY;
            var positionOffset = PositionOffset;

            if (moveHandle && ScrollHandle != null && !DocumentFitsView)
            {
                ScrollHandle.SetScroll(positionOffset);
            }

            Callbacks.CallOnPageScroll(this, new PageScrolledEventArgs(CurrentPage, positionOffset));

            Redraw();
        }

        public void LoadPageByOffset()
        {
            if (0 == PdfFile.PagesCount)
            {
                return;
            }

            float offset, screenCenter;
            if (IsSwipeVertical)
            {
                offset = CurrentYOffset;
                screenCenter = (float) Height / 2;
            }
            else
            {
                offset = CurrentXOffset;
                screenCenter = (float) Width / 2;
            }

            var page = PdfFile.GetPageAtOffset(-(offset - screenCenter), Zoom);

            if (page >= 0 && page <= PdfFile.PagesCount - 1 && page != CurrentPage)
            {
                ShowPage(page);
            }
            else
            {
                LoadPages();
            }
        }

        /**
         * Animate to the nearest snapping position for the current SnapPolicy
         */
        public void PerformPageSnap()
        {
            if (!PageSnap || PdfFile == null || PdfFile.PagesCount == 0)
            {
                return;
            }

            var centerPage = FindFocusPage(CurrentXOffset, CurrentYOffset);
            var edge = FindSnapEdge(centerPage);
            if (edge == SnapEdge.None)
            {
                return;
            }

            var offset = SnapOffsetForPage(centerPage, edge);
            if (IsSwipeVertical)
            {
                animationManager.StartYAnimation(CurrentYOffset, -offset);
            }
            else
            {
                animationManager.StartXAnimation(CurrentXOffset, -offset);
            }
        }

        /**
         * Find the edge to snap to when showing the specified page
         */
        public SnapEdge FindSnapEdge(int page)
        {
            if (!PageSnap || page < 0)
            {
                return SnapEdge.None;
            }

            var currentOffset = IsSwipeVertical ? CurrentYOffset : CurrentXOffset;
            var offset = -PdfFile.GetPageOffset(page, Zoom);
            var length = IsSwipeVertical ? Height : Width;
            var pageLength = PdfFile.GetPageLength(page, Zoom);

            if (length >= pageLength)
            {
                return SnapEdge.Center;
            }

            if (currentOffset >= offset)
            {
                return SnapEdge.Start;
            }

            if (offset - pageLength > currentOffset - length)
            {
                return SnapEdge.End;
            }

            return SnapEdge.None;
        }

        /**
         * Get the offset to move to in order to snap to the page
         */
        public float SnapOffsetForPage(int pageIndex, SnapEdge edge)
        {
            var offset = PdfFile.GetPageOffset(pageIndex, Zoom);

            float length = IsSwipeVertical ? Height : Width;
            var pageLength = PdfFile.GetPageLength(pageIndex, Zoom);

            switch (edge)
            {
                case SnapEdge.Center:
                    offset = offset - length / 2f + pageLength / 2f;
                    break;
                case SnapEdge.End:
                    offset = offset - length + pageLength;
                    break;
                case SnapEdge.Start:
                    break;
                case SnapEdge.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }

            return offset;
        }

        public int FindFocusPage(float xOffset, float yOffset)
        {
            var currOffset = IsSwipeVertical ? yOffset : xOffset;
            float length = IsSwipeVertical ? Height : Width;
            // make sure first and last page can be found
            if (currOffset > -1)
            {
                return 0;
            }

            if (currOffset < -PdfFile.GetDocLen(Zoom) + length + 1)
            {
                return PdfFile.PagesCount - 1;
            }

            // else find page in center
            var center = currOffset - length / 2f;
            return PdfFile.GetPageAtOffset(-center, Zoom);
        }

        /**
         * @return true if single page fills the entire screen in the scrolling direction
         */
        public bool PageFillsScreen()
        {
            var start = -PdfFile.GetPageOffset(CurrentPage, Zoom);
            var end = start - PdfFile.GetPageLength(CurrentPage, Zoom);
            if (IsSwipeVertical)
            {
                return start > CurrentYOffset && end < CurrentYOffset - Height;
            }

            return start > CurrentXOffset && end < CurrentXOffset - Width;
        }

        /**
         * Move relatively to the current position.
         *
         * @param dx The X difference you want to apply.
         * @param dy The Y difference you want to apply.
         * @see #moveTo(float, float)
         */
        public void MoveRelativeTo(float dx, float dy)
        {
            MoveTo(CurrentXOffset + dx, CurrentYOffset + dy);
        }

        /**
         * Change the zoom level
         */
        public void ZoomTo(float zoom)
        {
            this.Zoom = zoom;
        }

        /**
         * Change the zoom level, relatively to a pivot point.
         * It will call moveTo() to make sure the given point stays
         * in the middle of the screen.
         *
         * @param zoom  The zoom level.
         * @param pivot The point on the screen that should stays.
         */
        public void ZoomCenteredTo(float zoom, PointF pivot)
        {
            var dzoom = zoom / this.Zoom;
            ZoomTo(zoom);
            var baseX = CurrentXOffset * dzoom;
            var baseY = CurrentYOffset * dzoom;
            baseX += pivot.X - pivot.X * dzoom;
            baseY += pivot.Y - pivot.Y * dzoom;
            MoveTo(baseX, baseY);
        }

        /**
         * @see #zoomCenteredTo(float, PointF)
         */
        public void ZoomCenteredRelativeTo(float dzoom, PointF pivot)
        {
            ZoomCenteredTo(Zoom * dzoom, pivot);
        }

        /**
         * Checks if whole document can be displayed on screen, doesn't include zoom
         *
         * @return true if whole document can displayed at once, false otherwise
         */
        public bool DocumentFitsView
        {
            get
            {
                var len = PdfFile.GetDocLen(1);
                if (IsSwipeVertical)
                {
                    return len < Height;
                }

                return len < Width;
            }
        }

        public void FitToWidth(int page)
        {
            if (state != State.Shown)
            {
                Log.Error(TAG, "Cannot fit, document not rendered yet");
                return;
            }

            ZoomTo(Width / PdfFile.GetPageSize(page).Width);
            JumpTo(page);
        }

        public SizeF GetPageSize(int pageIndex)
        {
            return PdfFile == null ? new SizeF(0, 0) : PdfFile.GetPageSize(pageIndex);
        }

      

        public float ToRealScale(float size)
        {
            return size / Zoom;
        }

        public float ToCurrentScale(float size)
        {
            return size * Zoom;
        }

       

        public bool IsZooming => Zoom != MinZoom;

        public void ResetZoom()
        {
            ZoomTo(MinZoom);
        }

        public void ResetZoomWithAnimation()
        {
            ZoomWithAnimation(MinZoom);
        }

        public void ZoomWithAnimation(float centerX, float centerY, float scale)
        {
            animationManager.StartZoomAnimation(centerX, centerY, Zoom, scale);
        }

        public void ZoomWithAnimation(float scale)
        {
            animationManager.StartZoomAnimation(Width / 2, Height / 2, Zoom, scale);
        }


        /**
         * Get page number at given offset
         *
         * @param positionOffset scroll offset between 0 and 1
         * @return page number at given offset, starting from 0
         */
        public int GetPageAtPositionOffset(float positionOffset)
        {
            return PdfFile.GetPageAtOffset(PdfFile.GetDocLen(Zoom) * positionOffset, Zoom);
        }

      

        private void SetSpacing(int spacing)
        {
            this.SpacingPx = Util.Util.GetDp(Context, spacing);
        }

        public PdfDocument.Meta DocumentMeta => PdfFile?.MetaData;

        public List<PdfDocument.Bookmark> TableOfContents =>
            PdfFile == null ? new List<PdfDocument.Bookmark>() : PdfFile.Bookmarks;

        /** Will be empty until document is loaded */
        public List<PdfDocument.Link> GetLinks(int page)
        {
            return PdfFile == null ? new List<PdfDocument.Link>() : PdfFile.GetPageLinks(page);
        }

        /**
         * Use an asset file as the pdf source
         */
        public Configurator FromAsset(string assetName)
        {
            return new Configurator(new AssetSource(assetName), this);
        }

        /**
         * Use a file as the pdf source
         */
        public Configurator FromFile(File file)
        {
            return new Configurator(new FileSource(file), this);
        }

        /**
         * Use URI as the pdf source, for use with content providers
         */
        public Configurator FromUri(Android.Net.Uri uri)
        {
            return new Configurator(new UriSource(uri), this);
        }

        /**
         * Use bytearray as the pdf source, documents is not saved
         *
         * @param bytes
         * @return
         */
        public Configurator FromBytes(byte[] bytes)
        {
            return new Configurator(new ByteArraySource(bytes), this);
        }

        public Configurator FromStream(InputStream stream)
        {
            return new Configurator(new InputStreamSource(stream), this);
        }

        /**
         * Use custom source as pdf source
         */
        public Configurator FromSource(DocumentSource docSource)
        {
            return new Configurator(docSource, this);
        }

        private enum State
        {
            Default,
            Loaded,
            Shown,
            Error
        }

        public class Configurator
        {
            private readonly PdfView pdfView;
            private readonly DocumentSource documentSource;

            private int[] pageNumbers;

            private bool enableSwipe = true;

            private bool enableDoubletap = true;

            public EventHandler<LoadCompletedEventArgs> OnLoadCompleted { get; set; }
            public EventHandler<ErrorEventArgs> OnError { get; set; }
            public EventHandler<PageErrorEventArgs> OnPageError { get; set; }
            public EventHandler<RenderedEventArgs> OnRendered { get; set; }
            public EventHandler<PageChangeEventArgs> OnPageChanged { get; set; }
            public EventHandler<PageScrolledEventArgs> OnPageScrolled { get; set; }
            public EventHandler<DrawEventArgs> OnDraw { get; set; }
            public EventHandler<DrawEventArgs> OnDrawAll { get; set; }
            public EventHandler<TapEventArgs> OnTap { get; set; }
            public EventHandler<LongPressEventArgs> OnLongPress { get; set; }

            private ILinkHandler linkHandler;
            private int defaultPage;

            private bool swipeHorizontal;

            private bool annotationRendering;

            private string password;

            private IScrollHandle scrollHandle;

            private bool antialiasing = true;

            private int spacing;

            private bool autoSpacing;

            public FitPolicy pageFitPolicy;

            private bool pageFling;

            private bool pageSnap;

            private bool nightMode;

            public Configurator(DocumentSource documentSource, PdfView pdfView)
            {
                this.documentSource = documentSource;
                this.pdfView = pdfView;
                this.linkHandler = new DefaultLinkHandler(pdfView);
            }

            public Configurator Pages(int[] pageNumbers)
            {
                this.pageNumbers = pageNumbers;
                return this;
            }

            public Configurator EnableSwipe(bool enableSwipe)
            {
                this.enableSwipe = enableSwipe;
                return this;
            }

            public Configurator EnableDoubletap(bool enableDoubletap)
            {
                this.enableDoubletap = enableDoubletap;
                return this;
            }

            public Configurator EnableAnnotationRendering(bool annotationRendering)
            {
                this.annotationRendering = annotationRendering;
                return this;
            }

            public Configurator SetOnDraw(EventHandler<DrawEventArgs> onDraw)
            {
                this.OnDraw = onDraw;
                return this;
            }

            public Configurator SetOnDrawAll(EventHandler<DrawEventArgs> onDrawAll)
            {
                this.OnDrawAll = onDrawAll;
                return this;
            }

            public Configurator SetOnLoad(EventHandler<LoadCompletedEventArgs> onLoadComplete)
            {
                this.OnLoadCompleted = onLoadComplete;
                return this;
            }

            public Configurator SetOnPageScroll(EventHandler<PageScrolledEventArgs> onPageScroll)
            {
                this.OnPageScrolled = onPageScroll;
                return this;
            }

            public Configurator SetOnError(EventHandler<ErrorEventArgs> onError)
            {
                this.OnError = onError;
                return this;
            }

            public Configurator SetOnPageError(EventHandler<PageErrorEventArgs> onPageError)
            {
                this.OnPageError = onPageError;
                return this;
            }

            public Configurator SetOnPageChanged(EventHandler<PageChangeEventArgs> onPageChanged)
            {
                this.OnPageChanged = onPageChanged;
                return this;
            }

            public Configurator SetOnRender(EventHandler<RenderedEventArgs> onRender)
            {
                this.OnRendered = onRender;
                return this;
            }

            public Configurator SetOnTap(EventHandler<TapEventArgs> onTap)
            {
                this.OnTap = onTap;
                return this;
            }

            public Configurator SetOnLongPress(EventHandler<LongPressEventArgs> onLongPress)
            {
                this.OnLongPress = onLongPress;
                return this;
            }

            public Configurator LinkHandler(ILinkHandler linkHandler)
            {
                this.linkHandler = linkHandler;
                return this;
            }

            public Configurator DefaultPage(int defaultPage)
            {
                this.defaultPage = defaultPage;
                return this;
            }

            public Configurator SwipeHorizontal(bool swipeHorizontal)
            {
                this.swipeHorizontal = swipeHorizontal;
                return this;
            }

            public Configurator Password(string password)
            {
                this.password = password;
                return this;
            }

            public Configurator ScrollHandle(IScrollHandle scrollHandle)
            {
                this.scrollHandle = scrollHandle;
                return this;
            }

            public Configurator EnableAntialiasing(bool antialiasing)
            {
                this.antialiasing = antialiasing;
                return this;
            }

            public Configurator Spacing(int spacing)
            {
                this.spacing = spacing;
                return this;
            }

            public Configurator AutoSpacing(bool autoSpacing)
            {
                this.autoSpacing = autoSpacing;
                return this;
            }

            public Configurator SetPageFitPolicy(FitPolicy pageFitPolicy)
            {
                this.pageFitPolicy = pageFitPolicy;
                return this;
            }

            public Configurator SetPageSnap(bool pageSnap)
            {
                this.pageSnap = pageSnap;
                return this;
            }

            public Configurator SetPageFling(bool pageFling)
            {
                this.pageFling = pageFling;
                return this;
            }

            public Configurator NightMode(bool nightMode)
            {
                this.nightMode = nightMode;
                return this;
            }

            public void Load()
            {
                if (!pdfView.hasSize)
                {
                    pdfView.waitingDocumentConfigurator = this;
                    return;
                }

                pdfView.Recycle();
                pdfView.Callbacks.OnDrawAll = OnDrawAll;
                pdfView.Callbacks.OnDraw = OnDraw;
                pdfView.Callbacks.OnError = OnError;
                pdfView.Callbacks.OnLoadCompleted = OnLoadCompleted;
                pdfView.Callbacks.OnLongPress = OnLongPress;
                pdfView.Callbacks.OnPageChanged = OnPageChanged;
                pdfView.Callbacks.OnPageError = OnPageError;
                pdfView.Callbacks.OnPageScrolled = OnPageScrolled;
                pdfView.Callbacks.OnRendered = OnRendered;
                pdfView.Callbacks.OnTap = OnTap;
                pdfView.Callbacks.SetLinkHandler(linkHandler);
                pdfView.IsSwipeEnabled = enableSwipe;
                pdfView.SetNightMode(nightMode);
                pdfView.IsDoubletapEnabled = enableDoubletap;
                pdfView.DefaultPage = defaultPage;
                pdfView.IsSwipeVertical = !swipeHorizontal;
                pdfView.IsAnnotationRendering = annotationRendering;
                pdfView.ScrollHandle = scrollHandle;
                pdfView.IsAntialiasing = antialiasing;
                pdfView.SetSpacing(spacing);
                pdfView.AutoSpacing = autoSpacing;
                pdfView.PageFitPolicy = pageFitPolicy;
                pdfView.PageSnap = pageSnap;
                pdfView.PageFling = pageFling;
                if (pageNumbers != null)
                {
                    pdfView.Load(documentSource, password, pageNumbers);
                }
                else
                {
                    pdfView.Load(documentSource, password);
                }
            }
        }
    }
}