using System;
using PdfViewer.PDFium.Utils;

namespace PdfViewer.Util
{
    public class PageSizeCalculator
    {
        private readonly FitPolicy fitPolicy;
        private readonly Size originalMaxWidthPageSize;
        private readonly Size originalMaxHeightPageSize;
        private readonly Size viewSize;
        private SizeF optimalMaxWidthPageSize;
        private SizeF optimalMaxHeightPageSize;
        private float widthRatio;
        private float heightRatio;

        public PageSizeCalculator(FitPolicy fitPolicy, Size originalMaxWidthPageSize, Size originalMaxHeightPageSize,
            Size viewSize)
        {
            this.fitPolicy = fitPolicy;
            this.originalMaxWidthPageSize = originalMaxWidthPageSize;
            this.originalMaxHeightPageSize = originalMaxHeightPageSize;
            this.viewSize = viewSize;
            CalculateMaxPages();
        }

        public SizeF Calculate(Size pageSize)
        {
            if (pageSize.Width <= 0 || pageSize.Height <= 0)
            {
                return new SizeF(0, 0);
            }

            switch (fitPolicy)
            {
                case FitPolicy.Height:
                    return FitHeight(pageSize, pageSize.Height * heightRatio);
                case FitPolicy.Both:
                    return FitBoth(pageSize, pageSize.Width * widthRatio, pageSize.Height * heightRatio);
                default:
                    return FitWidth(pageSize, pageSize.Width * widthRatio);
            }
        }

        public SizeF GetOptimalMaxWidthPageSize()
        {
            return optimalMaxWidthPageSize;
        }

        public SizeF GetOptimalMaxHeightPageSize()
        {
            return optimalMaxHeightPageSize;
        }

        private void CalculateMaxPages()
        {
            switch (fitPolicy)
            {
                case FitPolicy.Height:
                    optimalMaxHeightPageSize = FitHeight(originalMaxHeightPageSize, viewSize.Height);
                    heightRatio = optimalMaxHeightPageSize.Height / originalMaxHeightPageSize.Height;
                    optimalMaxWidthPageSize = FitHeight(originalMaxWidthPageSize,
                        originalMaxWidthPageSize.Height * heightRatio);
                    break;
                case FitPolicy.Both:
                    var localOptimalMaxWidth = FitBoth(originalMaxWidthPageSize, viewSize.Width, viewSize.Height);
                    var localWidthRatio = localOptimalMaxWidth.Width / originalMaxWidthPageSize.Width;
                    this.optimalMaxHeightPageSize = FitBoth(originalMaxHeightPageSize,
                        originalMaxHeightPageSize.Width * localWidthRatio,
                        viewSize.Height);
                    heightRatio = optimalMaxHeightPageSize.Height / originalMaxHeightPageSize.Height;
                    optimalMaxWidthPageSize = FitBoth(originalMaxWidthPageSize, viewSize.Width,
                        originalMaxWidthPageSize.Height * heightRatio);
                    widthRatio = optimalMaxWidthPageSize.Width / originalMaxWidthPageSize.Width;
                    break;
                default:
                    optimalMaxWidthPageSize = FitWidth(originalMaxWidthPageSize, viewSize.Width);
                    widthRatio = optimalMaxWidthPageSize.Width / originalMaxWidthPageSize.Width;
                    optimalMaxHeightPageSize = FitWidth(originalMaxHeightPageSize,
                        originalMaxHeightPageSize.Width * widthRatio);
                    break;
            }
        }

        private static SizeF FitWidth(Size pageSize, float maxWidth)
        {
            float w = pageSize.Width, h = pageSize.Height;
            var ratio = w / h;
            w = maxWidth;
            h = (float) Math.Floor(maxWidth / ratio);
            return new SizeF(w, h);
        }

        private static SizeF FitHeight(Size pageSize, float maxHeight)
        {
            float w = pageSize.Width, h = pageSize.Height;
            var ratio = h / w;
            h = maxHeight;
            w = (float) Math.Floor(maxHeight / ratio);
            return new SizeF(w, h);
        }

        private static SizeF FitBoth(Size pageSize, float maxWidth, float maxHeight)
        {
            float w = pageSize.Width, h = pageSize.Height;
            var ratio = w / h;
            w = maxWidth;
            h = (float) Math.Floor(maxWidth / ratio);
            if (!(h > maxHeight)) return new SizeF(w, h);
            h = maxHeight;
            w = (float) Math.Floor(maxHeight * ratio);

            return new SizeF(w, h);
        }
    }
}