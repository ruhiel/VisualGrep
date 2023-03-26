using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualGrep.Models;
using VisualGrep.Utls;

namespace VisualGrep.ViewModels
{
    public class OutputTypeViewModel
    {
        private OutputType _OutputType;
        public string DisplayName
        {
            get { return _OutputType.GetDisplayName(); }
        }
        public OutputType OutputType
        {
            get { return _OutputType; }
        }

        public OutputTypeViewModel(OutputType outputType)
        {
            _OutputType = outputType;
        }
    }
}
