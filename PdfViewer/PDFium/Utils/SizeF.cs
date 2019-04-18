namespace PdfViewer.PDFium.Utils
{
    public class SizeF
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public SizeF(float width, float height)
        {
            this.Width = width;
            this.Height = height;
        }

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
            if (obj is SizeF other) {
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
            return Java.Lang.Float.FloatToIntBits(Width) ^ Java.Lang.Float.FloatToIntBits(Height);
        }

        public Size ToSize()
        {
            return new Size()
            {
                Width = (int)Width,
                Height = (int)Height
            };
        }
    }
}