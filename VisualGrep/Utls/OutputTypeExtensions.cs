using System.ComponentModel.DataAnnotations;
using System.Reflection;
using VisualGrep.Models;

namespace VisualGrep.Utls
{
    public static class OutputTypeExtensions
    {
        public static string GetDisplayName(this OutputType classification)
        {
            return classification.GetType()
                                 .GetMember(classification.ToString())[0]
                                 .GetCustomAttribute<DisplayAttribute>()?.Name
                ?? classification.ToString();
        }
    }
}
