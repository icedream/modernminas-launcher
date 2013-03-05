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

            if (resolverNode.SelectSingleNode("child::regex") == null)
                throw new InvalidOperationException();
            if (resolverNode.SelectSingleNode("child::match") == null)
                throw new InvalidOperationException();
            if (!ushort.TryParse(resolverNode.SelectSingleNode("child::match").InnerText, out matchIndex))
                throw new InvalidOperationException();
           
            var regex = Expand(resolverNode.SelectSingleNode("child::regex").InnerText);
            var match = Expand(resolverNode.SelectSingleNode("child::match").InnerText);
            var content = Input.ToString();

            Log.DebugFormat("Searching value input: {0}", content);
            Log.DebugFormat("Regex: {0}", regex);
            Log.DebugFormat("Requested match ID: {0}", match);

            var m = Regex.Matches(content, regex, RegexOptions.Compiled);

            if (matchIndex >= m.Count)
                throw new ArgumentOutOfRangeException("Requested result #" + matchIndex + " but there are only " + m.Count + " matches.");

            Log.DebugFormat("Result is: {0}", m[matchIndex].Value);
            return m[matchIndex].Value;
        }
    }
}
