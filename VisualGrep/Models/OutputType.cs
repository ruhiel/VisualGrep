using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualGrep.Models
{
    public enum OutputType
    {
        [Display(Name = "文字列のみ")]
        StringOnly,
        [Display(Name = "ファイル名のみ")]
        FileNameOnly,
        [Display(Name = "明細データ")]
        DetailData,
    }
}
