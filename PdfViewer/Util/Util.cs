using Android.Content;
using Android.Util;
using Java.IO;

namespace PdfViewer.Util
{
    public class Util
    {
        private const int DefaultBufferSize = 1024 * 4;

        public static int GetDp(Context context, int dp)
        {
            return (int) TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, context.Resources.DisplayMetrics);
        }

        public static byte[] ToByteArray(InputStream inputStream)
        {
            var os = new ByteArrayOutputStream();
            var buffer = new byte[DefaultBufferSize];
            int n;
            while (-1 != (n = inputStream.Read(buffer)))
            {
                os.Write(buffer, 0, n);
            }
            return os.ToByteArray();
        }
    }
}