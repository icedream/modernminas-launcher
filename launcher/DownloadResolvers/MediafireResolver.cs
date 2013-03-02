using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace ModernMinas.Launcher.DownloadResolvers
{
    public class MediafireResolver : DownloadResolverBase
    {
        WebClient wc = new WebClient();

        public override WebRequest ResolveFile(Uri uri)
        {
            if (!uri.Host.Equals("www.mediafire.com") && !uri.Host.Equals("mediafire.com"))
                throw new InvalidOperationException(string.Format("Not a mediafire url: {0}", uri));

            string mfpage = wc.DownloadString(uri);
            var mfdlmatch = Regex.Match(mfpage, "kNO = \"(.+)\";$");

            if (!mfdlmatch.Success)
                throw new Exception("Could not resolve mediafire link.");

            var req = HttpWebRequest.Create(mfdlmatch.Groups[1].Value);
            return req;
        }

        public override string GetCurrentFileHash(Uri uri)
        {
            if (!uri.Host.Equals("www.mediafire.com") && !uri.Host.Equals("mediafire.com"))
                throw new InvalidOperationException(string.Format("Not a mediafire url: {0}", uri));

            string mfpage = wc.DownloadString(uri);
            var mfdlmatch = Regex.Match(mfpage, "YmI = \"(.+)\";$");

            if (!mfdlmatch.Success)
                throw new Exception("Could not resolve mediafire link.");

            return mfdlmatch.Groups[1].Value;
        }
    }
}
