using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace PdfViewer.Scroll
{
    public sealed class DefaultScrollHandle : RelativeLayout, IScrollHandle
    {
        private const int HandleLong = 65;
        private const int HandleShort = 40;
        private const int DefaultTextSize = 16;

        private float relativeHandlerMiddle;
        private readonly TextView textView;
        private readonly Context context;
        private readonly bool inverted;
        private PdfView pdfView;
        private float currentPos;

        private readonly Handler handler = new Handler();

        public DefaultScrollHandle(Context context) : base(context)
        {
            this.context = context;
            textView = new TextView(context) {Visibility = ViewStates.Invisible};
            textView.SetTextSize(ComplexUnitType.Dip, DefaultTextSize);
            textView.SetTextColor(Color.Black);
        }

        public DefaultScrollHandle(Context context, bool inverted) : base(context)
        {
            this.context = context;
            this.inverted = inverted;
            textView = new TextView(context) {Visibility = ViewStates.Invisible};
            textView.SetTextSize(ComplexUnitType.Dip, DefaultTextSize);
            textView.SetTextColor(Color.Black);
        }

        public void SetScroll(float position)
        {
            if (!Shown())
            {
                Show();
            }
            else
            {
                handler.RemoveCallbacks(() => this.Visibility = ViewStates.Invisible);
            }

            SetPosition((pdfView.IsSwipeVertical ? pdfView.Height : pdfView.Width) * position);
        }

        private void SetPosition(float pos)
        {
            if (Float.InvokeIsInfinite(pos) || Float.InvokeIsNaN(pos))
                return;
            float pdfViewSize = pdfView.IsSwipeVertical ? pdfView.Height : pdfView.Width;
            pos -= relativeHandlerMiddle;

            if (pos < 0)
                pos = 0;
            else if (pos > pdfViewSize - Util.Util.GetDp(context, HandleShort))
                pos = pdfViewSize - Util.Util.GetDp(context, HandleShort);

            if (pdfView.IsSwipeVertical)
                SetY(pos);
            else
                SetX(pos);
            CalculateMiddle();
            Invalidate();
        }

        private void CalculateMiddle()
        {
            float pos, viewSize, pdfViewSize;
            if (pdfView.IsSwipeVertical)
            {
                pos = GetY();
                viewSize = Height;
                pdfViewSize = pdfView.Height;
            }
            else
            {
                pos = GetX();
                viewSize = Width;
                pdfViewSize = pdfView.Width;
            }

            relativeHandlerMiddle = (pos + relativeHandlerMiddle) / pdfViewSize * viewSize;
        }

        public void SetupLayout(PdfView pdfView)
        {
            LayoutRules align;
            int width, height;
            Drawable background;
            // determine handler position, default is right (when scrolling vertically) or bottom (when scrolling horizontally)
            if (pdfView.IsSwipeVertical)
            {
                width = HandleLong;
                height = HandleShort;
                if (inverted)
                {
                    // left

                    align = LayoutRules.AlignParentLeft;
                    background = Context.GetDrawable(Resource.Drawable.default_scroll_handle_left);
                }
                else
                {
                    // right
                    align = LayoutRules.AlignParentRight;
                    background =Resources.GetDrawable(Resource.Drawable.default_scroll_handle_right);
                }
            }
            else
            {
                width = HandleShort;
                height = HandleLong;
                if (inverted)
                {
                    // top
                    align = LayoutRules.AlignParentTop;
                    background = Context.GetDrawable(Resource.Drawable.default_scroll_handle_top);
                }
                else
                {
                    // bottom
                    align = LayoutRules.AlignParentBottom;
                    background = Context.GetDrawable(Resource.Drawable.default_scroll_handle_bottom);
                }
            }

            if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBean)
            {
                SetBackgroundDrawable(background);
            }
            else
            {
                Background = background;
            }

            var lp = new LayoutParams(Util.Util.GetDp(context, width), Util.Util.GetDp(context, height));
            lp.SetMargins(0, 0, 0, 0);

            var tvlp = new LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            tvlp.AddRule(LayoutRules.CenterInParent);
            AddView(textView, tvlp);

            lp.AddRule(align);
            pdfView.AddView(this, lp);

            this.pdfView = pdfView;
        }

        public void DestroyLayout()
        {
            pdfView.RemoveView(this);
        }

        public void SetPageNum(int pageNum)
        {
            var text = pageNum.ToString();
            if (!textView.Text.Equals(text))
            {
                textView.Text = text;
            }
        }

        public bool Shown()
        {
            return Visibility == ViewStates.Visible;
        }

        public void Show()
        {
            Visibility = ViewStates.Visible;
        }

        public void Hide()
        {
            Visibility = ViewStates.Invisible;
        }

        public void HideDelayed()
        {
            handler.PostDelayed(() => this.Visibility = ViewStates.Invisible, 1000);
        }

        public void SetTextColor(Color color)
        {
            textView.SetTextColor(color);
        }

        public void SetTextSize(int size)
        {
            textView.SetTextSize(ComplexUnitType.Dip, size);
        }

        private bool IsPdfViewReady()
        {
            return pdfView != null && pdfView.PageCount > 0 && !pdfView.DocumentFitsView;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (!IsPdfViewReady())
            {
                return base.OnTouchEvent(e);
            }

            switch (e.Action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                    pdfView.StopFling();
                    handler.RemoveCallbacks(() => this.Visibility = ViewStates.Invisible);
                    if (pdfView.IsSwipeVertical)
                    {
                        currentPos = e.RawY - GetY();
                    }
                    else
                    {
                        currentPos = e.RawX - GetX();
                    }

                    break;
                case MotionEventActions.Move:
                    if (pdfView.IsSwipeVertical)
                    {
                        SetPosition(e.RawY - currentPos + relativeHandlerMiddle);
                        pdfView.SetPositionOffset(relativeHandlerMiddle / (float) Height, false);
                    }
                    else
                    {
                        SetPosition(e.RawX - currentPos + relativeHandlerMiddle);
                        pdfView.SetPositionOffset(relativeHandlerMiddle / (float) Width, false);
                    }

                    return true;
                case MotionEventActions.Cancel:
                case MotionEventActions.Up:
                case MotionEventActions.PointerUp:
                    HideDelayed();
                    return true;
            }

            return base.OnTouchEvent(e);
        }
    }
}