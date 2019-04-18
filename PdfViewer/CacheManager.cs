using System.Collections.Generic;
using Android.Graphics;
using PdfViewer.Model;
using PdfViewer.Util;
using PdfViewer.Util.OrderedSet;


namespace PdfViewer
{
    public class CacheManager
    {
        private readonly OrderedSet<PagePart> passiveCache;
        private readonly OrderedSet<PagePart> activeCache;

        private readonly List<PagePart> thumbnails;
        static readonly object passiveActiveLock = new object();


        public CacheManager()
        {
            activeCache = new OrderedSet<PagePart>(new PagePartComparator());
            passiveCache = new OrderedSet<PagePart>(new PagePartComparator());
            thumbnails = new List<PagePart>();
        }

        public void CachePart(PagePart part)
        {
            lock (passiveActiveLock)
            {
                // If cache too big, remove and recycle
                MakeAFreeSpace();
                // Then add part
                activeCache.Add(part);
            }
        }

        public void MakeANewSet()
        {
            lock (passiveActiveLock)
            {
                passiveCache.AddMany(activeCache);
                activeCache.Clear();
            }
        }

        private void MakeAFreeSpace()
        {
            lock (passiveActiveLock)
            {
                while (activeCache.Count + passiveCache.Count >= Constants.Cache.CacheSize &&
                       passiveCache.Count != 0)
                {
                    var part = passiveCache.RemoveFirst();
                    part.RenderedBitmap.Recycle();
                }

                while (activeCache.Count + passiveCache.Count >= Constants.Cache.CacheSize &&
                       activeCache.Count != 0)
                {
                    activeCache.RemoveFirst().RenderedBitmap.Recycle();
                }
            }
        }

        public void CacheThumbnail(PagePart part)
        {
            lock (thumbnails)
            {
                // If cache too big, remove and recycle
                if (thumbnails.Count >= Constants.Cache.ThumbnailsCacheSize)
                {
                    thumbnails[0].RenderedBitmap.Recycle();
                    thumbnails.RemoveAt(0);
                }

                // Then add thumbnail
                thumbnails.Add(part);
            }
        }

        public bool UpPartIfContained(int page, RectF pageRelativeBounds, int toOrder)
        {
            var fakePart = new PagePart(page, null, pageRelativeBounds, false, 0);

            lock (passiveActiveLock)
            {
                PagePart found;
                if ((found = Find(passiveCache, fakePart)) != null)
                {
                    passiveCache.Remove(found);
                    found.CacheOrder = toOrder;
                    activeCache.Add(found);
                    return true;
                }

                return Find(activeCache, fakePart) != null;
            }
        }

        public bool ContainsThumbnail(int page, RectF pageRelativeBounds)
        {
            var fakePart = new PagePart(page, null, pageRelativeBounds, true, 0);
            lock (thumbnails)
            {
                foreach (var part in thumbnails)
                {
                    if (part.Equals(fakePart))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static PagePart Find(IEnumerable<PagePart> vector, PagePart fakePart)
        {
            foreach (var part in vector)
            {
                if (part.Equals(fakePart))
                {
                    return part;
                }
            }

            return null;
        }

        public List<PagePart> GetPageParts()
        {
            lock (passiveActiveLock)
            {
                var parts = new List<PagePart>(passiveCache);
                parts.AddRange(activeCache);
                return parts;
            }
        }

        public List<PagePart> GetThumbnails()
        {
            lock (thumbnails)
            {
                return thumbnails;
            }
        }

        public void Recycle()
        {
            lock (passiveActiveLock)
            {
                foreach (var part in passiveCache)
                {
                    part.RenderedBitmap.Recycle();
                }

                passiveCache.Clear();
                foreach (var part in activeCache)
                {
                    part.RenderedBitmap.Recycle();
                }

                activeCache.Clear();
            }

            lock (thumbnails)
            {
                foreach (var part in thumbnails)
                {
                    part.RenderedBitmap.Recycle();
                }

                thumbnails.Clear();
            }
        }

        public class PagePartComparator : IComparer<PagePart>
        {
            public int Compare(PagePart x, PagePart y)
            {
                if (x?.CacheOrder == y?.CacheOrder)
                    return 0;
                return x.CacheOrder > y.CacheOrder ? 1 : -1;
            }
        }
    }
}