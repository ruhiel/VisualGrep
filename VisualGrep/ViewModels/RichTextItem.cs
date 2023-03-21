using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace VisualGrep.ViewModels
{
    public class RichTextItem
    {
        public string Text { get; set; }
        public Brush Foreground { get; set; }
        public FontWeight FontWeight { get; set; }
        public Thickness Margin { get; set; }
    }
}
