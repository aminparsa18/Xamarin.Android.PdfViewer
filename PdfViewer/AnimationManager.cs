using Android.Animation;
using Android.Graphics;
using Android.Views.Animations;
using Android.Widget;

namespace PdfViewer
{
    public class AnimationManager
    {
        private readonly PdfView pdfView;

        private ValueAnimator animation;

        private readonly OverScroller scroller;

        private bool flinging;
        private static bool pageFlinging;

        public AnimationManager(PdfView pdfView)
        {
            this.pdfView = pdfView;
            scroller = new OverScroller(pdfView.Context);
        }

        public void StartXAnimation(float xFrom, float xTo)
        {
            StopAll();
            animation = ValueAnimator.OfFloat(xFrom, xTo);
            animation.SetInterpolator(new DecelerateInterpolator());
            animation.AnimationCancel += (s, e) =>
            {
                pdfView.LoadPages();
                pageFlinging = false;
            };
            animation.Update += (s, e) =>
            {
                pdfView.MoveTo((float) e.Animation.AnimatedValue, pdfView.CurrentYOffset);
            };
            animation.AnimationCancel += (s, e) =>
            {
                pdfView.LoadPages();
                pageFlinging = false;
            };
            animation.SetDuration(400);
            animation.Start();
        }

        public void StartYAnimation(float yFrom, float yTo)
        {
            StopAll();
            animation = ValueAnimator.OfFloat(yFrom, yTo);
            animation.SetInterpolator(new DecelerateInterpolator());
            animation.AnimationCancel += (s, e) =>
            {
                pdfView.LoadPages();
                pageFlinging = false;
            };
            animation.Update += (s, e) =>
            {
                pdfView.MoveTo(pdfView.CurrentXOffset, (float) e.Animation.AnimatedValue);
                pdfView.LoadPageByOffset();
            };
            animation.AnimationEnd += (s, e) =>
            {
                pdfView.LoadPages();
                pageFlinging = false;
            };
            animation.SetDuration(400);
            animation.Start();
        }

        public void StartZoomAnimation(float centerX, float centerY, float zoomFrom, float zoomTo)
        {
            StopAll();
            animation = ValueAnimator.OfFloat(zoomFrom, zoomTo);
            animation.SetInterpolator(new DecelerateInterpolator());
            animation.Update += (s, e) =>
            {
                pdfView.ZoomCenteredTo((float) e.Animation.AnimatedValue, new PointF(centerX, centerY));
            };
            animation.AnimationEnd += (s, e) =>
            {
                pdfView.LoadPages();
                if (pdfView.ScrollHandle != null)
                    pdfView.ScrollHandle.HideDelayed();
                pdfView.PerformPageSnap();
            };
            animation.SetDuration(400);
            animation.Start();
        }

        public void StartFlingAnimation(int startX, int startY, int velocityX, int velocityY, int minX, int maxX,
            int minY, int maxY)
        {
            StopAll();
            flinging = true;
            scroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY);
        }

        public void StartPageFlingAnimation(float targetOffset)
        {
            if (pdfView.IsSwipeVertical)
            {
                StartYAnimation(pdfView.CurrentYOffset, targetOffset);
            }
            else
            {
                StartXAnimation(pdfView.CurrentXOffset, targetOffset);
            }

            pageFlinging = true;
        }

        public void ComputeFling()
        {
            if (scroller.ComputeScrollOffset())
            {
                pdfView.MoveTo(scroller.CurrX, scroller.CurrY);
                pdfView.LoadPageByOffset();
            }
            else if (flinging)
            {
                // fling finished
                flinging = false;
                pdfView.LoadPages();
                HideHandle();
                pdfView.PerformPageSnap();
            }
        }

        public void StopAll()
        {
            if (animation != null)
            {
                animation.Cancel();
                animation = null;
            }

            StopFling();
        }

        public void StopFling()
        {
            flinging = false;
            scroller.ForceFinished(true);
        }

        public bool IsFlinging()
        {
            return flinging || pageFlinging;
        }

        public void HideHandle()
        {
            pdfView.ScrollHandle?.HideDelayed();
        }
    }
}