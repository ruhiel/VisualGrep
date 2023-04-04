using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualGrep.Utls
{
    public static class ObservableCollectionExtentions
    {
        public static void ClearAndAddAllSafe<T>(this ObservableCollection<T> collection, IEnumerable<T> itemEnumerable)
        {
            if(itemEnumerable == null)
            {
                return;
            }

            collection.Clear();

            foreach (var item in itemEnumerable)
            {
                collection.Add(item);
            }
        }
    }
}
