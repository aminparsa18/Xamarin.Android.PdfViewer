
namespace PdfViewer.PDFium.Utils
{
    public class Size:Java.Lang.Object
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (this == obj)
            {
                return true;
            }
            if (obj is Size other) {
                return Width == other.Width && Height == other.Height;
            }
            return false;
        }
        public override string ToString()
        {
            return Width + "x" + Height;
        }

        public override int GetHashCode()
        {
            return Height ^ ((Width << (32 / 2)) | (int)((uint)Width >> (32/ 2)));
        }
    }
}