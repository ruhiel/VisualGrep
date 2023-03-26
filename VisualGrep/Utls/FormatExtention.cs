using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualGrep.Models;

namespace VisualGrep.Utls
{
    public static class FormatExtention
    {
        public static string LineCreate(this Format format, LineInfo lineInfo)
        {
            if(format == Format.Tsv)
            {
                return FileUtils.TsvLineCreate(lineInfo.FullPath, lineInfo.Line.ToString(), lineInfo.Text);
            }
            else
            {
                return FileUtils.CsvLineCreate(lineInfo.FullPath, lineInfo.Line.ToString(), lineInfo.Text);
            }
            
        }
    }
}
