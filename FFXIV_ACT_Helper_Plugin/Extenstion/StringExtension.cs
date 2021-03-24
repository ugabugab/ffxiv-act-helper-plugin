using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class StringExtension
    {
        public static int CompareAsIntTo(this string strA, string strB)
        {
            if (int.TryParse(strA, out int numA) == false)
            {
                numA = int.MinValue;
            }
            if (int.TryParse(strB, out int numB) == false)
            {
                numB = int.MinValue;
            }
            return numA.CompareTo(numB);
        }

        public static int CompareAsDoubleTo(this string strA, string strB)
        {
            if (double.TryParse(strA, out double numA) == false)
            {
                numA = double.MinValue;
            }
            if (double.TryParse(strB, out double numB) == false)
            {
                numB = double.MinValue;
            }
            return numA.CompareTo(numB);
        }
    }
}
