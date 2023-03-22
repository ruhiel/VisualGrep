using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualGrep.Models
{
    public class LineInfo : BindableBase
    {
        private string _FileName = string.Empty;
        public string FileName
        {
            get { return _FileName; }
            set { SetProperty(ref _FileName, value); }
        }

        private string _FilePath = string.Empty;
        public string FilePath
        {
            get { return _FilePath; }
            set { SetProperty(ref _FilePath, value); }
        }

        private uint _Line = 0;
        public string Line
        {
            get { return _Line.ToString(); }
            set { SetProperty(ref _Line, uint.Parse(value)); }
        }
        
        private string _Text = string.Empty;
        public string Text
        {
            get { return _Text; }
            set { SetProperty(ref _Text, value); }
        }

        private string _Sheet = string.Empty;
        public string Sheet
        {
            get { return _Sheet; }
            set { SetProperty(ref _Sheet, value); }
        }

        public string FullPath
        {
            get { return Path.Combine(FilePath, FileName); }
        }
    }
}
