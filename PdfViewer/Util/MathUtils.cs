namespace PdfViewer.Util
{
   public class MathUtils
    {
        private const int BigEnoughInt = 16 * 1024;
        private const double BigEnoughFloor = BigEnoughInt;
        private const double BigEnoughCeil = 16384.999999999996;

        private MathUtils()
        {
            // Prevents instantiation
        }

        /**
         * Limits the given <b>number</b> between the other values
         * @param number  The number to limit.
         * @param between The smallest value the number can take.
         * @param and     The biggest value the number can take.
         * @return The limited number.
         */
        public static int Limit(int number, int between, int and)
        {
            if (number <= between)
                return between;
            return number >= and ? and : number;
        }

        /**
         * Limits the given <b>number</b> between the other values
         * @param number  The number to limit.
         * @param between The smallest value the number can take.
         * @param and     The biggest value the number can take.
         * @return The limited number.
         */
        public static float Limit(float number, float between, float and)
        {
            if (number <= between)
                return between;
            return number >= and ? and : number;
        }

        public static float Max(float number, float max)
        {
            return number > max ? max : number;
        }

        public static float Min(float number, float min)
        {
            return number < min ? min : number;
        }

        public static int Max(int number, int max)
        {
            return number > max ? max : number;
        }

        public static int Min(int number, int min)
        {
            return number < min ? min : number;
        }

        /**
         * Methods from libGDX - https://github.com/libgdx/libgdx
         */

        /** Returns the largest integer less than or equal to the specified float. This method will only properly floor floats from
         * -(2^14) to (Float.MAX_VALUE - 2^14). */
         public static int Floor(float value)
        {
            return (int) (value + BigEnoughFloor) - BigEnoughInt;
        }

        /** Returns the smallest integer greater than or equal to the specified float. This method will only properly ceil floats from
         * -(2^14) to (Float.MAX_VALUE - 2^14). */
         public static int Ceil(float value)
        {
            return (int) (value + BigEnoughCeil) - BigEnoughInt;
        }
    }
}