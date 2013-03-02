using System;
using System.Collections.Generic;
using System.IO;
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
        public override Stream ResolveToStream()
        {
            var wc = new WebClient();
            var url = string.Format("http://mediafire.com/?{0}", Expand(resolverNode.SelectSingleNode("child::id").InnerText));

            MemoryStream ms = new MemoryStream();

            var s = wc.OpenRead(url);
            int length = UTF8Encoding.UTF8.GetMaxByteCount(16);
            byte[] buffer = new byte[length];
            s.Read(buffer, 0, length);

            if (Encoding.UTF8.GetString(buffer).Contains("<!DOCTYPE html>") || Encoding.UTF8.GetString(buffer).Contains("<html>"))
            {
                Console.WriteLine("MediafireResolver: Resolving page...");
                string m;
                using (StreamReader sr = new StreamReader(s))
                {
                    var c = sr.ReadToEnd();
                    m = Regex.Match(c, "kNO = \"(.+)\"").Groups[1].Value;
                }
                s.Close();
                s.Dispose();
                s = wc.OpenRead(m);
            }
            else
            {
                Console.WriteLine("MediafireResolver: No need for extensive resolving.");
                ms.Write(buffer, 0, length);
            }
            Console.WriteLine("MediafireResolver: Downloading file of type {0}", wc.ResponseHeaders[HttpResponseHeader.ContentType]);
            s.CopyTo(ms);
            s.Close();
            s.Dispose();

            return ms;
        }
    }
}
