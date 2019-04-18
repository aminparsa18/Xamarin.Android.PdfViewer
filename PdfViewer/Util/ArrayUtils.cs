using System.Collections.Generic;
using System.Text;
namespace PdfViewer.Util
{
   public static class ArrayUtils
    {
        public static int[] DeleteDuplicatedPages(int[] pages)
        {
            var result = new List<int>();
            var lastInt = -1;
            foreach (var currentInt in pages)
            {
                if (lastInt != currentInt)
                    result.Add(currentInt);
                lastInt = currentInt;
            }
            var arrayResult = new int[result.Count];
            for (var i = 0; i < result.Count; i++)
                arrayResult[i] = result[i];
            return arrayResult;
        }

        public static int[] CalculateIndexesInDuplicateArray(int[] originalUserPages)
        {
            var result = new int[originalUserPages.Length];
            if (originalUserPages.Length == 0)
                return result;
            var index = 0;
            result[0] = index;
            for (var i = 1; i < originalUserPages.Length; i++)
            {
                if (originalUserPages[i] != originalUserPages[i - 1])
                    index++;
                result[i] = index;
            }
            return result;
        }

        public static string ArrayToString(int[] array)
        {
            var builder = new StringBuilder("[");
            for (var i = 0; i < array.Length; i++)
            {
                builder.Append(array[i]);
                if (i != array.Length - 1)
                    builder.Append(",");
            }
            builder.Append("]");
            return builder.ToString();
        }
    }
}