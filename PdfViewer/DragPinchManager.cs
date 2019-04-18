using System;
using Android.Graphics;
using Android.Views;
using PdfViewer.Listener;
using PdfViewer.Model;
using PdfViewer.Util;

namespace PdfViewer
{
   public class DragPinchManager :Java.Lang.Object,GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener,
        ScaleGestureDetector.IOnScaleGestureListener
    {
        private readonly PdfView pdfView;
        private readonly AnimationManager animationManager;

        private readonly GestureDetector gestureDetector;
        private readonly ScaleGestureDetector scaleGestureDetector;

        private bool scrolling;
        private bool scaling;
        private bool enabled;

        public DragPinchManager(PdfView pdfView, AnimationManager animationManager)
        {
            this.pdfView = pdfView;
            this.animationManager = animationManager;
            gestureDetector = new GestureDetector(pdfView.Context, this);
            scaleGestureDetector = new ScaleGestureDetector(pdfView.Context, this);
            pdfView.Touch+=(s,e)=>
            {
                if (!enabled)
                {
                  e.Handled=false;
                }

                var retVal = scaleGestureDetector.OnTouchEvent(e.Event);
                retVal = gestureDetector.OnTouchEvent(e.Event) || retVal;

                if (e.Event.Action != MotionEventActions.Up) e.Handled=retVal;
                if (!scrolling) e.Handled=retVal;
                scrolling = false;
                OnScrollEnd(e.Event);
                e.Handled = retVal;
            };
        }

       public void Enable()
        {
            enabled = true;
        }

        public void Disable()
        {
            enabled = false;
        }

        public bool OnDown(MotionEvent e)
        {
            animationManager.StopFling();
            return true;
        }

        public void OnScrollEnd(MotionEvent e) {
            pdfView.LoadPages();
            HideHandle();
            if (!animationManager.IsFlinging())
            {
                pdfView.PerformPageSnap();
            }
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (!pdfView.IsSwipeEnabled)
            {
                return false;
            }
            if (pdfView.PageFling)
            {
                if (pdfView.PageFillsScreen())
                {
                    OnBoundedFling(velocityX, velocityY);
                }
                else
                {
                    StartPageFling(e1, e2, velocityX, velocityY);
                }
                return true;
            }
            var xOffset = (int) pdfView.CurrentXOffset;
            var yOffset = (int) pdfView.CurrentYOffset;

            float minX, minY;
            var pdfFile = pdfView.PdfFile;
            if (pdfView.IsSwipeVertical)
            {
                minX = -(pdfView.ToCurrentScale(pdfFile.MaxPageWidth) - pdfView.Width);
                minY = -(pdfFile.GetDocLen(pdfView.Zoom) - pdfView.Height);
            }
            else
            {
                minX = -(pdfFile.GetDocLen(pdfView.Zoom) - pdfView.Width);
                minY = -(pdfView.ToCurrentScale(pdfFile.MaxPageHeight) - pdfView.Height);
            }

            animationManager.StartFlingAnimation(xOffset, yOffset, (int) velocityX, (int) velocityY,
                (int) minX, 0, (int) minY, 0);

            return true;
        }

        private void OnBoundedFling(float velocityX, float velocityY)
        {
            var xOffset = (int)pdfView.CurrentXOffset;
            var yOffset = (int)pdfView.CurrentYOffset;

            var pdfFile = pdfView.PdfFile;

            var pageStart = -pdfFile.GetPageOffset(pdfView.CurrentPage, pdfView.Zoom);
            var pageEnd = pageStart - pdfFile.GetPageLength(pdfView.CurrentPage, pdfView.Zoom);
            float minX, minY, maxX, maxY;
            if (pdfView.IsSwipeVertical)
            {
                minX = -(pdfView.ToCurrentScale(pdfFile.MaxPageWidth) - pdfView.Width);
                minY = pageEnd + pdfView.Height;
                maxX = 0;
                maxY = pageStart;
            }
            else
            {
                minX = pageEnd + pdfView.Width;
                minY = -(pdfView.ToCurrentScale(pdfFile.MaxPageHeight) - pdfView.Height);
                maxX = pageStart;
                maxY = 0;
            }

            animationManager.StartFlingAnimation(xOffset, yOffset, (int)velocityX, (int)velocityY,
                    (int)minX, (int)maxX, (int)minY, (int)maxY);
        }
        public void OnLongPress(MotionEvent e)
        {
            pdfView.Callbacks.CallOnLongPress(e);
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            scrolling = true;
            if (pdfView.IsZooming || pdfView.IsSwipeEnabled)
            {
                pdfView.MoveRelativeTo(-distanceX, -distanceY);
            }
            if (!scaling || pdfView.RenderDuringScale)
            {
                pdfView.LoadPageByOffset();
            }
            return true;
        }

        public void OnShowPress(MotionEvent e)
        {
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            return false;
        }

        public bool OnDoubleTap(MotionEvent e)
        {
            if (!pdfView.IsDoubletapEnabled)
            {
                return false;
            }
            if (pdfView.Zoom < pdfView.MidZoom)
            {
                pdfView.ZoomWithAnimation(e.GetX(), e.GetY(), pdfView.MidZoom);
            }
            else if (pdfView.Zoom < pdfView.MaxZoom)
            {
                pdfView.ZoomWithAnimation(e.GetX(), e.GetY(), pdfView.MaxZoom);
            }
            else
            {
                pdfView.ResetZoomWithAnimation();
            }
            return true;
        }

        public bool OnDoubleTapEvent(MotionEvent e)
        {
            return false;
        }

        public bool OnSingleTapConfirmed(MotionEvent e)
        {
            var onTapHandled = pdfView.Callbacks.CallOnTap(pdfView,new TapEventArgs(e));
            var linkTapped = CheckLinkTapped(e.GetX(), e.GetY());
            if (!onTapHandled && !linkTapped)
            {
                var ps = pdfView.ScrollHandle;
                if (ps != null && !pdfView.DocumentFitsView)
                {
                    if (!ps.Shown())
                    {
                        ps.Show();
                    }
                    else
                    {
                        ps.Hide();
                    }
                }
            }
            pdfView.PerformClick();
            return true;
        }
        private bool CheckLinkTapped(float x, float y)
        {
            var pdfFile = pdfView.PdfFile;
            var mappedX = -pdfView.CurrentXOffset + x;
            var mappedY = -pdfView.CurrentYOffset + y;
            var page = pdfFile.GetPageAtOffset(pdfView.IsSwipeVertical ? mappedY : mappedX, pdfView.Zoom);
            var pageSize = pdfFile.GetScaledPageSize(page, pdfView.Zoom);
            int pageX, pageY;
            if (pdfView.IsSwipeVertical)
            {
                pageX = (int)pdfFile.GetSecondaryPageOffset(page, pdfView.Zoom);
                pageY = (int)pdfFile.GetPageOffset(page, pdfView.Zoom);
            }
            else
            {
                pageY = (int)pdfFile.GetSecondaryPageOffset(page, pdfView.Zoom);
                pageX = (int)pdfFile.GetPageOffset(page, pdfView.Zoom);
            }
            foreach (var link in pdfFile.GetPageLinks(page))
            {
                var mapped = pdfFile.MapRectToDevice(page, pageX, pageY, (int)pageSize.Width,
                        (int)pageSize.Height, link.Bounds);
                mapped.Sort();
                if (!mapped.Contains(mappedX, mappedY)) continue;
                pdfView.Callbacks.CallLinkHandler(new LinkTapEvent(x, y, mappedX, mappedY, mapped, link));
                return true;
            }
            return false;
        }
        private void StartPageFling(MotionEvent downEvent, MotionEvent ev, float velocityX, float velocityY)
        {
            if (!CheckDoPageFling(velocityX, velocityY))
            {
                return;
            }

            int direction;
            if (pdfView.IsSwipeVertical)
            {
                direction = velocityY > 0 ? -1 : 1;
            }
            else
            {
                direction = velocityX > 0 ? -1 : 1;
            }
            // get the focused page during the down event to ensure only a single page is changed
            var delta = pdfView.IsSwipeVertical ? ev.GetY() - downEvent.GetY() : ev.GetX() - downEvent.GetX();
            var offsetX = pdfView.CurrentXOffset - delta * pdfView.Zoom;
            var offsetY = pdfView.CurrentYOffset - delta * pdfView.Zoom;
            var startingPage = pdfView.FindFocusPage(offsetX, offsetY);
            var targetPage = Math.Max(0, Math.Min(pdfView.PageCount - 1, startingPage + direction));

            var edge = pdfView.FindSnapEdge(targetPage);
            var offset = pdfView.SnapOffsetForPage(targetPage, edge);
            animationManager.StartPageFlingAnimation(-offset);
        }

        public bool OnScale(ScaleGestureDetector detector)
        {
            var dr = detector.ScaleFactor;
            var wantedZoom = pdfView.Zoom * dr;
            if (wantedZoom < Constants.Pinch.MinimumZoom)
            {
                dr =Constants.Pinch.MinimumZoom / pdfView.Zoom;
            }
            else if (wantedZoom > Constants.Pinch.MaximumZoom)
            {
                dr = Constants.Pinch.MaximumZoom / pdfView.Zoom;
            }
            pdfView.ZoomCenteredRelativeTo(dr, new PointF(detector.FocusX, detector.FocusY));
            return true;
        }

        public bool OnScaleBegin(ScaleGestureDetector detector)
        {
            scaling = true;
            return true;
        }

        public void OnScaleEnd(ScaleGestureDetector detector)
        {
            pdfView.LoadPages();
            HideHandle();
            scaling = false;
        }
        private void HideHandle()
        {
            if (pdfView.ScrollHandle != null && pdfView.ScrollHandle.Shown())
            {
                pdfView.ScrollHandle.HideDelayed();
            }
        }
        private bool CheckDoPageFling(float velocityX, float velocityY)
        {
            var absX = Math.Abs(velocityX);
            var absY = Math.Abs(velocityY);
            return pdfView.IsSwipeVertical ? absY > absX : absX > absY;
        }
    }
}