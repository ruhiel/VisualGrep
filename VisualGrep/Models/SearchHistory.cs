using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualGrep.Models
{
    public class SearchHistory
    {
        public List<string> SearchTextHistory { get; set; }
        public List<string> SearchDirectoryHistory { get; set; }
        public List<string> SearchFileNameHistory { get; set; }
        public List<string> ExcludeFilePathHistory { get; set; }
    }
}
