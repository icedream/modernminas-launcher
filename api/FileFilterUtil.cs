using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModernMinas.Update.Api
{
    public static class FileFilterUtil
    {
        private static string Transform(string filter)
        {
            return string.Format("^{0}$", filter.Replace("\\", "\\\\").Replace(".", "\\.").Replace("*", ".*").Replace("?", ".?")
                .Replace("/", "\\/").Replace("-", "\\-")
                .Replace("+", "\\+").Replace("|", "\\|").Replace("(", "\\(")
                .Replace(")", "\\)"));
        }

        public static IEnumerable<string> FilterFiles(string[] entries, string filter = "*")
        {
            if (filter == null)
                filter = "*";

            // Transform to a regex
            filter = Transform(filter);

            // Output filtered entries
            return from e in entries where Regex.IsMatch(e.Replace(System.IO.Path.DirectorySeparatorChar, '/'), filter) select e;
        }

        public static bool IsMatch(string entry, string filter)
        {
            entry = entry.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            
            // Transform to a regex
            filter = Transform(filter);

            return Regex.IsMatch(entry, filter);
        }
    }
}
