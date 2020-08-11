using System;

namespace Catutil.Migration
{
    // https://stackoverflow.com/questions/19123506/jaro-winkler-distance-algorithm-in-c-sharp

    /// <summary>
    /// Jaro-Winkler string distance calculator.
    /// </summary>
    public static class JaroWinkler
    {
        /* The Winkler modification will not be applied unless the 
         * percent match was at or above the mWeightThreshold percent 
         * without the modification. 
         * Winkler's paper used a default value of 0.7
         */
        private const double WEIGHT_TRESHOLD = 0.7;

        /* Size of the prefix to be concidered by the Winkler modification. 
         * Winkler's paper used a default value of 4
         */
        private const int NUM_CHARS = 4;

        /// <summary>
        /// Returns the Jaro-Winkler distance between the specified  
        /// strings. The distance is symmetric and will fall in the 
        /// range 0 (perfect match) to 1 (no match). 
        /// </summary>
        /// <param name="a">First String</param>
        /// <param name="b">Second String</param>
        /// <returns>Distance.</returns>
        public static double Distance(string a, string b)
        {
            return 1.0 - Proximity(a, b);
        }

        /// <summary>
        /// Returns the Jaro-Winkler distance between the specified  
        /// strings. The distance is symmetric and will fall in the 
        /// range 0 (no match) to 1 (perfect match). 
        /// </summary>
        /// <param name="a">First String</param>
        /// <param name="b">Second String</param>
        /// <returns>Proximity.</returns>
        public static double Proximity(string a, string b)
        {
            int lena = a.Length;
            int lenb = b.Length;
            if (lena == 0)
                return lenb == 0 ? 1.0 : 0.0;

            int searchRange = Math.Max(0, (Math.Max(lena, lenb) / 2) - 1);

            // default initialized to false
            bool[] matcheda = new bool[lena];
            bool[] matchedb = new bool[lenb];

            int numCommon = 0;
            for (int i = 0; i < lena; ++i)
            {
                int start = Math.Max(0, i - searchRange);
                int end = Math.Min(i + searchRange + 1, lenb);
                for (int j = start; j < end; ++j)
                {
                    if (matchedb[j]) continue;
                    if (a[i] != b[j])
                        continue;
                    matcheda[i] = true;
                    matchedb[j] = true;
                    ++numCommon;
                    break;
                }
            }
            if (numCommon == 0) return 0.0;

            int numHalfTransposed = 0;
            int k = 0;
            for (int i = 0; i < lena; ++i)
            {
                if (!matcheda[i]) continue;
                while (!matchedb[k]) ++k;
                if (a[i] != b[k])
                    ++numHalfTransposed;
                ++k;
            }
            int lNumTransposed = numHalfTransposed / 2;

            double numCommonD = numCommon;
            double weight = (numCommonD / lena
                            + numCommonD / lenb
                            + (numCommon - lNumTransposed) / numCommonD) / 3.0;

            if (weight <= WEIGHT_TRESHOLD) return weight;
            int lMax = Math.Min(NUM_CHARS, Math.Min(a.Length, b.Length));
            int lPos = 0;
            while (lPos < lMax && a[lPos] == b[lPos])
                ++lPos;
            if (lPos == 0) return weight;
            return weight + (0.1 * lPos * (1.0 - weight));
        }
    }
}