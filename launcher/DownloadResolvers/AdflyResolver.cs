using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace ModernMinas.Launcher.DownloadResolvers
{
    public class AdflyResolver : DownloadResolverBase
    {
        WebClient wc = new WebClient();
        // var url = '/go/411c12796890a1521fb33b4ce4a163c0/aHR0cDovL2RsLmRyb3Bib3guY29tL3UvMzAzNjEwODUvRWxlbWVudGFsQ3JlZXBlcnNfdjIuM19zcF9NQzEuMi41LnppcA';
        public override WebRequest ResolveFile(Uri uri)
        {
            if (!uri.Host.Equals("adf.ly"))
                throw new InvalidOperationException("Not an adf.ly link");

            string content = wc.DownloadString(uri);

            var m = Regex.Match(content, "
        }

        public override string GetCurrentFileHash(Uri uri)
        {

        }
    }
}
