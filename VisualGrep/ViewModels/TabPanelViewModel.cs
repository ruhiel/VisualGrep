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
        public ReactiveProperty<List<RichTextItem>> OutMessage { get; } = new ReactiveProperty<List<RichTextItem>>();
    }
}
