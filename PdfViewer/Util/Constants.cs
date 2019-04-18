namespace PdfViewer.Util
{
    internal class Constants
    {
        public static bool DebugMode = false;

        /** Between 0 and 1, the thumbnails quality (default 0.3). Increasing this value may cause performance decrease */
        public static float ThumbnailRatio = 0.3f;

        /**
         * The size of the rendered parts (default 256)
         * Tinier : a little bit slower to have the whole page rendered but more reactive.
         * Bigger : user will have to wait longer to have the first visual results
         */
        public static float PartSize = 256;

        /** Part of document above and below screen that should be preloaded, in dp */
        public static int PreloadOffset = 20;

        public static class Cache
        {
            /** The size of the cache (number of bitmaps kept) */
            public static int CacheSize = 120;

            public static int ThumbnailsCacheSize = 8;
        }

        public static class Pinch
        {
            public static float MaximumZoom = 10;

            public static float MinimumZoom = 1;
        }
    }
}