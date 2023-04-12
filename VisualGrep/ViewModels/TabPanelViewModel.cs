using ICSharpCode.AvalonEdit.Document;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualGrep.ViewModels
{
    public class TabPanelViewModel
    {
        public ReactiveProperty<string> Header { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<TextDocument> TextView { get; } = new ReactiveProperty<TextDocument>();
    }
}
