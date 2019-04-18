using System;
using System.IO;
using Android.Content;

namespace PdfViewer.Util
{
    public class FileUtils
    {
        private FileUtils()
        {
            // Prevents instantiation
        }

        public static Java.IO.File FileFromAssetAsync(Context context, string assetName)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), assetName);
            using (var stream = context.Assets.Open(assetName))
            {
                using (var fileStream = File.Create(path))
                {
                    var buffer = new byte[1024];
                    var b = buffer.Length;
                    int length;

                    while ((length = stream.Read(buffer, 0, b)) > 0)
                    {
                        fileStream.Write(buffer, 0, length);
                    }

                    fileStream.Flush();
                    fileStream.Close();
                    stream.Close();
                    return new Java.IO.File(path);
                }
            }
        }
    }
}