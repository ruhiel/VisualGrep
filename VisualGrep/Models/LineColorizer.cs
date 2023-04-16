using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;

namespace VisualGrep.Models
{
    public class LineColorizer : DocumentColorizingTransformer
    {
        int lineNumber;

        public LineColorizer(int lineNumber)
        {
            this.lineNumber = lineNumber;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (!line.IsDeleted && line.LineNumber == lineNumber)
            {
                ChangeLinePart(line.Offset, line.EndOffset, ApplyChanges);
            }
        }

        void ApplyChanges(VisualLineElement element)
        {
            // This is where you do anything with the line
            element.TextRunProperties.SetForegroundBrush(Brushes.Red);
        }
    }
}
