using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModernMinas.Update.Api
{
    public static class FileFilterUtil
    {
        public static IEnumerable<string> FilterFiles(string[] entries, string filter)
        {
            Console.WriteLine("Filter was {0}", filter);

            // Transform to a regex
            filter = filter.Replace("\\", "\\\\").Replace(".", "\\.").Replace("*", ".*").Replace("?", ".?")
                .Replace("/", "\\/").Replace("-", "\\-")
                .Replace("+", "\\+").Replace("|", "\\|").Replace("(", "\\(")
                .Replace(")", "\\)");
            filter = string.Format("^{0}$", filter);

            Console.WriteLine("Regular expression is {0}", filter);

            // Output filtered entries
            return from e in entries where Regex.IsMatch(e, filter) select e;
        }
    }
}
