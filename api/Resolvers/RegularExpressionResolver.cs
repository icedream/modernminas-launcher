using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Net;

namespace ModernMinas.Update.Api.Resolvers
{
    [ResolverName("regex")]
    public class RegularExpressionResolver : ResolverBase
    {
        public override string ResolveToString()
        {
            ushort matchIndex;

            //if (resolverNode.SelectSingleNode("child::url") == null)
            //    throw new InvalidOperationException();
            if (resolverNode.SelectSingleNode("child::regex") == null)
                throw new InvalidOperationException();
            if (resolverNode.SelectSingleNode("child::match") == null)
                throw new InvalidOperationException();
            if (!ushort.TryParse(resolverNode.SelectSingleNode("child::match").InnerText, out matchIndex))
                throw new InvalidOperationException();
            
            //var url = resolverNode.SelectSingleNode("child::url").InnerText;
            var regex = Expand(resolverNode.SelectSingleNode("child::regex").InnerText);
            var match = Expand(resolverNode.SelectSingleNode("child::match").InnerText);
            //var content = new WebClient().DownloadString(url);
            var content = Input.ToString();

            //Console.WriteLine("Regex: {0}", regex);
            //Console.WriteLine("Requested match: {0}", match);

#if REGEX_DEBUG_INPUT
            Console.WriteLine("=== Input starts here ===");
            Console.WriteLine(content);
            Console.WriteLine("=== Input ends here ===");
#endif

            var m = Regex.Matches(content, regex, RegexOptions.Compiled);

            if (matchIndex >= m.Count)
                throw new ArgumentOutOfRangeException("Requested result #" + matchIndex + " but there are only " + m.Count + " matches.");

            return m[matchIndex].Value;
        }
    }
}
