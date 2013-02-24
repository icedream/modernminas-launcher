using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ModernMinas.Update.Api.Resolvers
{
    [ResolverName("mediafire")]
    public class MediafireResolver : ResolverBase
    {
        public override string ResolveToString()
        {
            var wc = new WebClient();
            var c = wc.DownloadString(Expand(resolverNode.SelectSingleNode("child::id").InnerText));
            var m = Regex.Match(c, "kNO = \"(.+)\"").Captures[1].Value;
            return m;
        }
    }
}
