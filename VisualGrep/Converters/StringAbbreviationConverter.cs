using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VisualGrep.Converters
{
    public class StringAbbreviationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return value;
            }

            int maxLength = int.Parse(parameter.ToString());
            string input = value.ToString();

            if (input.Length <= maxLength)
            {
                return input;
            }

            int startLength = (maxLength - 3) / 2;
            int endLength = maxLength - 3 - startLength;

            return input.Substring(0, startLength) + "..." + input.Substring(input.Length - endLength, endLength);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
