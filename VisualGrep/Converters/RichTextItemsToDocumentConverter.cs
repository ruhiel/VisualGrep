using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using VisualGrep.ViewModels;

namespace VisualGrep.Converters
{
    public class RichTextItemsToDocumentConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value == null)
            {
                return new FlowDocument();
            }

            var doc = new FlowDocument();

            foreach (var item in (ICollection <RichTextItem>)value)
            {
                var paragraph = new Paragraph(new Run(item.Text))
                {
                    Foreground = item.Foreground,
                    FontWeight = item.FontWeight,
                    Margin = item.Margin,
                };

                doc.Blocks.Add(paragraph);
            }

            return doc;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
