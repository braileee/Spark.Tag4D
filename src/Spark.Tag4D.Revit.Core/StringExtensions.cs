using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.Core
{
    public static class StringExtensions
    {
        public static double ToDouble(this string value)
        {
            string input = value.Trim();

            // Normalize: replace comma with dot
            input = input.Replace(',', '.');

            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return 0;
        }

        public static int ToInt(this string value)
        {
            string input = value.Trim();
            if (int.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }
            return 0;
        }

        public static bool ToBool(this string value)
        {
            string input = value.Trim().ToLower();

            if (bool.TryParse(input, out bool result))
            {
                return result;
            }
            return false;
        }
    }
}
