using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace VisualGrep.Controls
{
    public class BindableRichTextBox : RichTextBox
    {
        #region 依存関係プロパティ
        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register("Document", typeof(FlowDocument), typeof(BindableRichTextBox), new UIPropertyMetadata(null, OnRichTextItemsChanged));
        #endregion  // 依存関係プロパティ

        #region 公開プロパティ
        public new FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
        #endregion  // 公開プロパティ

        #region イベントハンドラ
        private static void OnRichTextItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as RichTextBox;
            if (control != null)
            {
                control.Document = e.NewValue as FlowDocument;
            }
        }
        #endregion  // イベントハンドラ
    }
}
