using System;
using System.Collections.Generic;

namespace MeroDokan
{
    public static class TableHelper
    {
        public static int CompareTableNumbers(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int numX = ExtractTablePrefixNumber(x, out string suffixX);
            int numY = ExtractTablePrefixNumber(y, out string suffixY);

            if (numX != numY)
                return numX.CompareTo(numY);

            return string.Compare(suffixX, suffixY, StringComparison.OrdinalIgnoreCase);
        }

        private static int ExtractTablePrefixNumber(string s, out string suffix)
        {
            suffix = "";
            if (string.IsNullOrEmpty(s)) return 0;

            string trimmed = s.Trim();
            if (trimmed.StartsWith("Table ", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(6).Trim();

            int i = 0;
            while (i < trimmed.Length && char.IsDigit(trimmed[i])) i++;

            if (i > 0)
            {
                int.TryParse(trimmed.Substring(0, i), out int val);
                suffix = trimmed.Substring(i);
                return val;
            }

            suffix = trimmed;
            return 999999;
        }

        public static string GetBaseTableNumber(string tableNum)
        {
            if (string.IsNullOrEmpty(tableNum)) return "1";
            int dashIdx = tableNum.IndexOf('-');
            return dashIdx > 0 ? tableNum.Substring(0, dashIdx) : tableNum;
        }

        public static string GetCustomerSuffix(string tableNum)
        {
            if (string.IsNullOrEmpty(tableNum)) return "";
            int dashIdx = tableNum.IndexOf('-');
            return dashIdx > 0 && dashIdx < tableNum.Length - 1 ? tableNum.Substring(dashIdx + 1) : "";
        }
    }
}
