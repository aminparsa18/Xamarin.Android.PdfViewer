using System;
using Android.Graphics;
using PdfViewer.Util;

namespace PdfViewer
{
    public class PagesLoader
    {
        private readonly PdfView pdfView;
        private int cacheOrder;
        private float xOffset;
        private float yOffset;
        private float pageRelativePartWidth;
        private float pageRelativePartHeight;
        private float partRenderWidth;
        private float partRenderHeight;
        private readonly RectF thumbnailRect = new RectF(0, 0, 1, 1);
        private readonly int preloadOffset;
        private readonly Holder firstHolder = new Holder();
        private readonly Holder lastHolder = new Holder();
        private readonly GridSize firstGrid = new GridSize();
        private readonly GridSize lastGrid = new GridSize();
        private readonly GridSize middleGrid = new GridSize();

        private class Holder
        {
            public int Page { get; set; }
            public int Row { get; set; }
            public int Col { get; set; }
        }

        private class GridSize
        {
            public int Rows { get; set; }
            public int Cols { get; set; }
        }

        public PagesLoader(PdfView pdfView)
        {
            this.pdfView = pdfView;
            this.preloadOffset = Util.Util.GetDp(pdfView.Context, preloadOffset);
        }

        private void GetPageColsRows(GridSize grid, int pageIndex)
        {
            var size = pdfView.PdfFile.GetPageSize(pageIndex);
            var ratioX = 1f / size.Width;
            var ratioY = 1f / size.Height;
            var partHeight = Constants.PartSize * ratioY / pdfView.Zoom;
            var partWidth = Constants.PartSize * ratioX / pdfView.Zoom;
            grid.Rows = MathUtils.Ceil(1f / partHeight);
            grid.Cols = MathUtils.Ceil(1f / partWidth);
        }

        private void GetPageAndCoordsByOffset(Holder holder, GridSize grid, float localXOffset,
            float localYOffset, bool endOffset)
        {
            var fixedXOffset = -MathUtils.Max(localXOffset, 0);
            var fixedYOffset = -MathUtils.Max(localYOffset, 0);
            var offset = pdfView.IsSwipeVertical ? fixedYOffset : fixedXOffset;
            holder.Page = pdfView.PdfFile.GetPageAtOffset(offset, pdfView.Zoom);
            GetPageColsRows(grid, holder.Page);
            var scaledPageSize = pdfView.PdfFile.GetScaledPageSize(holder.Page, pdfView.Zoom);
            var rowHeight = scaledPageSize.Height/ grid.Rows;
            var colWidth = scaledPageSize.Width / grid.Cols;
            float row, col;
            var secondaryOffset = pdfView.PdfFile.GetSecondaryPageOffset(holder.Page, pdfView.Zoom);

            if (pdfView.IsSwipeVertical)
            {
                row = Math.Abs(fixedYOffset - pdfView.PdfFile.GetPageOffset(holder.Page, pdfView.Zoom)) /
                      rowHeight;
                col = MathUtils.Min(fixedXOffset - secondaryOffset, 0) / colWidth;
            }
            else
            {
                col = Math.Abs(fixedXOffset - pdfView.PdfFile.GetPageOffset(holder.Page, pdfView.Zoom)) / colWidth;
                row = MathUtils.Min(fixedYOffset - secondaryOffset, 0) / rowHeight;
            }

            if (endOffset)
            {
                holder.Row = MathUtils.Ceil(row);
                holder.Col = MathUtils.Ceil(col);
            }
            else
            {
                holder.Row = MathUtils.Floor(row);
                holder.Col = MathUtils.Floor(col);
            }
        }

        private void CalculatePartSize(GridSize grid)
        {
            pageRelativePartWidth = 1f / (float) grid.Cols;
            pageRelativePartHeight = 1f / (float) grid.Rows;
            partRenderWidth = Constants.PartSize / pageRelativePartWidth;
            partRenderHeight = Constants.PartSize / pageRelativePartHeight;
        }

        public void LoadVisible()
        {
            var parts = 0;
            var scaledPreloadOffset = preloadOffset * pdfView.Zoom;
            var firstXOffset = -xOffset + scaledPreloadOffset;
            var lastXOffset = -xOffset - pdfView.Width - scaledPreloadOffset;
            var firstYOffset = -yOffset + scaledPreloadOffset;
            var lastYOffset = -yOffset - pdfView.Height - scaledPreloadOffset;

            GetPageAndCoordsByOffset(firstHolder, firstGrid, firstXOffset, firstYOffset, false);
            GetPageAndCoordsByOffset(lastHolder, lastGrid, lastXOffset, lastYOffset, true);

            for (var i = firstHolder.Page; i <= lastHolder.Page; i++)
            {
                LoadThumbnail(i);
            }

            var pagesCount = lastHolder.Page - firstHolder.Page + 1;
            for (var page = firstHolder.Page; page <= lastHolder.Page && parts < Constants.Cache.CacheSize; page++)
            {
                if (page == firstHolder.Page && pagesCount > 1)
                {
                    parts += LoadPageEnd(firstHolder, firstGrid, Constants.Cache.CacheSize - parts);
                }
                else if (page == lastHolder.Page && pagesCount > 1)
                {
                    parts += LoadPageStart(lastHolder, lastGrid, Constants.Cache.CacheSize - parts);
                }
                else if (pagesCount == 1)
                {
                    parts += LoadPageCenter(firstHolder, lastHolder, firstGrid, Constants.Cache.CacheSize - parts);
                }
                else
                {
                    GetPageColsRows(middleGrid, page);
                    parts += LoadWholePage(page, middleGrid, Constants.Cache.CacheSize - parts);
                }
            }
        }

        private int LoadWholePage(int page, GridSize grid, int nbOfPartsLoadable)
        {
            CalculatePartSize(grid);
            return LoadPage(page, 0, grid.Rows - 1, 0, grid.Cols - 1, nbOfPartsLoadable);
        }

        private int LoadPageCenter(Holder firstHolder, Holder lastHolder, GridSize grid, int nbOfPartsLoadable)
        {
            CalculatePartSize(grid);
            return LoadPage(firstHolder.Page, firstHolder.Row, lastHolder.Row, firstHolder.Col, lastHolder.Col,
                nbOfPartsLoadable);
        }

        private int LoadPageEnd(Holder holder, GridSize grid, int nbOfPartsLoadable)
        {
            CalculatePartSize(grid);
            if (pdfView.IsSwipeVertical)
            {
                var firstRow = holder.Row;
                return LoadPage(holder.Page, firstRow, grid.Rows - 1, 0, grid.Cols - 1, nbOfPartsLoadable);
            }

            var firstCol = holder.Col;
            return LoadPage(holder.Page, 0, grid.Rows - 1, firstCol, grid.Cols - 1, nbOfPartsLoadable);
        }

        private int LoadPageStart(Holder holder, GridSize grid, int nbOfPartsLoadable)
        {
            CalculatePartSize(grid);
            if (pdfView.IsSwipeVertical)
            {
                var lastRow = holder.Row;
                return LoadPage(holder.Page, 0, lastRow, 0, grid.Cols - 1, nbOfPartsLoadable);
            }

            var lastCol = holder.Col;
            return LoadPage(holder.Page, 0, grid.Rows - 1, 0, lastCol, nbOfPartsLoadable);
        }

        private int LoadPage(int page, int firstRow, int lastRow, int firstCol, int lastCol,
            int nbOfPartsLoadable)
        {
            var loaded = 0;
            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var col = firstCol; col <= lastCol; col++)
                {
                    if (LoadCell(page, row, col, pageRelativePartWidth, pageRelativePartHeight))
                    {
                        loaded++;
                    }

                    if (loaded >= nbOfPartsLoadable)
                    {
                        return loaded;
                    }
                }
            }

            return loaded;
        }

        private bool LoadCell(int page, int row, int col, float pageRelativePartWidth, float pageRelativePartHeight)
        {
            var relX = pageRelativePartWidth * col;
            var relY = pageRelativePartHeight * row;
            var relWidth = pageRelativePartWidth;
            var relHeight = pageRelativePartHeight;

            var renderWidth = partRenderWidth;
            var renderHeight = partRenderHeight;
            if (relX + relWidth > 1)
            {
                relWidth = 1 - relX;
            }

            if (relY + relHeight > 1)
            {
                relHeight = 1 - relY;
            }

            renderWidth *= relWidth;
            renderHeight *= relHeight;
            var pageRelativeBounds = new RectF(relX, relY, relX + relWidth, relY + relHeight);

            if (renderWidth > 0 && renderHeight > 0)
            {
                if (!pdfView.CacheManager.UpPartIfContained(page, pageRelativeBounds, cacheOrder))
                {
                    pdfView.RenderingHandler.AddRenderingTask(page, renderWidth, renderHeight,
                        pageRelativeBounds, false, cacheOrder, pdfView.IsBestQuality,
                        pdfView.IsAnnotationRendering);
                }

                cacheOrder++;
                return true;
            }

            return false;
        }

        private void LoadThumbnail(int page)
        {
            var pageSize = pdfView.PdfFile.GetPageSize(page);
            var thumbnailWidth = pageSize.Width * Constants.ThumbnailRatio;
            var thumbnailHeight = pageSize.Height * Constants.ThumbnailRatio;
            if (!pdfView.CacheManager.ContainsThumbnail(page, thumbnailRect))
            {
                pdfView.RenderingHandler.AddRenderingTask(page,
                    thumbnailWidth, thumbnailHeight, thumbnailRect,
                    true, 0, pdfView.IsBestQuality, pdfView.IsAnnotationRendering);
            }
        }

        public void LoadPages()
        {
            cacheOrder = 1;
            xOffset = -MathUtils.Max(pdfView.CurrentXOffset, 0);
            yOffset = -MathUtils.Max(pdfView.CurrentYOffset, 0);

            LoadVisible();
        }
    }
}