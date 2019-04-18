using Android.Graphics;
using Android.OS;
using Java.Lang;
using PdfViewer.Exceptions;
using PdfViewer.Model;

namespace PdfViewer
{
    public class RenderingHandler : Handler
    {
        public static int MsgRenderTask = 1;

        private readonly PdfView pdfView;
        private readonly RectF renderBounds = new RectF();
        private readonly Rect roundedRenderBounds = new Rect();
        private readonly Matrix renderMatrix = new Matrix();
        private bool running;

        public RenderingHandler(Looper looper, PdfView pdfView) : base(looper)
        {
            this.pdfView = pdfView;
        }

        public void AddRenderingTask(int page, float width, float height, RectF bounds, bool thumbnail, int cacheOrder,
            bool bestQuality, bool annotationRendering)
        {
            var task = new RenderingTask(width, height, bounds, page, thumbnail, cacheOrder, bestQuality,
                annotationRendering);
            var msg = ObtainMessage(MsgRenderTask, task);
            SendMessage(msg);
        }

        public override void HandleMessage(Message msg)
        {
            var task = (RenderingTask) msg.Obj;
            try
            {
                var part = Proceed(task);
                if (part != null)
                {
                    if (running)
                    {
                        pdfView.Post(() => pdfView.OnBitmapRendered(part));
                    }
                    else
                    {
                        part.RenderedBitmap.Recycle();
                    }
                }
            }
            catch (PageRenderingException ex)
            {
                pdfView.Post(() => pdfView.SetOnPageError(ex));
            }
        }

        private PagePart Proceed(RenderingTask renderingTask)
        {
            var pdfFile = pdfView.PdfFile;
            pdfFile.OpenPage(renderingTask.Page);

            var w = (int) System.Math.Round(renderingTask.Width);
            var h = (int) System.Math.Round(renderingTask.Height);
            Bitmap render;
            try
            {
                render = Bitmap.CreateBitmap(w, h,
                    renderingTask.BestQuality ? Bitmap.Config.Argb8888 : Bitmap.Config.Rgb565);
            }
            catch (IllegalArgumentException e)
            {
                return null;
            }

            CalculateBounds(w, h, renderingTask.Bounds);
            pdfFile.RenderPageBitmap(render, renderingTask.Page,
                roundedRenderBounds, renderingTask.AnnotationRendering);

            return new PagePart(renderingTask.Page, render, //
                renderingTask.Bounds, renderingTask.Thumbnail, //
                renderingTask.CacheOrder);
        }

        private void CalculateBounds(int width, int height, RectF pageSliceBounds)
        {
            renderMatrix.Reset();
            renderMatrix.PostTranslate(-pageSliceBounds.Left * width, -pageSliceBounds.Top * height);
            renderMatrix.PostScale(1 / pageSliceBounds.Width(), 1 / pageSliceBounds.Height());

            renderBounds.Set(0, 0, width, height);
            renderMatrix.MapRect(renderBounds);
            renderBounds.Round(roundedRenderBounds);
        }

        public void Stop()
        {
            running = false;
        }

        public void Start()
        {
            running = true;
        }

        public class RenderingTask : Object
        {
            public float Width, Height;

            public RectF Bounds;

            public int Page;

            public bool Thumbnail;

            public int CacheOrder;

            public bool BestQuality;

            public bool AnnotationRendering;

            public RenderingTask(float width, float height, RectF bounds, int page, bool thumbnail, int cacheOrder,
                bool bestQuality, bool annotationRendering)
            {
                this.Page = page;
                this.Width = width;
                this.Height = height;
                this.Bounds = bounds;
                this.Thumbnail = thumbnail;
                this.CacheOrder = cacheOrder;
                this.BestQuality = bestQuality;
                this.AnnotationRendering = annotationRendering;
            }
        }
    }
}