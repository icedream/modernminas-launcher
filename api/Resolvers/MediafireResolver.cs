using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Sockets;
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
            var url = new Uri(string.Format("http://mediafire.com/?{0}", Expand(resolverNode.SelectSingleNode("child::id").InnerText)));
            Uri oldM = null;
            
            MemoryStream ms = new MemoryStream();

        retry1:
            System.Diagnostics.Debug.WriteLine("MF URL: {0}", url.ToString());
            Dictionary<string, string> headers = new Dictionary<string, string>();

            // Mediafire has buggy HTTP servers which tend to violate the protocol, crashing the whole download with a ProtocolViolationException.
            // We are going to do a manual request with manual parsing here. ._.
            var socket1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket1.Connect(new DnsEndPoint(url.Host, 80));
            Stream s = new BufferedStream(new NetworkStream(socket1));
            using (var ncs = new NonClosingStream(s))
            using (var sr = new StreamReader(ncs))
            using (var sw = new StreamWriter(ncs))
            {
                sw.WriteLine("GET {0} HTTP/1.0", url.PathAndQuery);
                sw.WriteLine("Host: {0}", url.Host);
                sw.WriteLine("User-Agent: ModernMinasLauncher/3.0 (U; compatible)");
                if (oldM != null)
                    sw.WriteLine("Referer: {0}", oldM.ToString());
                sw.WriteLine("Connection: close");
                sw.WriteLine();
                sw.Flush();
                var status = sr.ReadLine().Trim();
                if (!new[] { "200", "301", "302", "303" }.Contains(status.Split(' ')[1]))
                    throw new WebException("HTTP error: " + status);
                string line = "";
                while ((line = sr.ReadLine().Trim()).Any())
                {
                    var l = line.Split(':');
                    headers.Add(l[0].ToLower(), string.Join(":", l.Skip(1)));
                }
            }
            oldM = url;
            if (headers.ContainsKey("location"))
            {
                url = new Uri(url, headers["location"]);
                goto retry1;
            }
            long length = UTF8Encoding.UTF8.GetMaxByteCount(16);
            byte[] buffer = new byte[length];
            s.Read(buffer, 0, (int)length);

            if (Encoding.UTF8.GetString(buffer).Contains("<!DOCTYPE html>") || Encoding.UTF8.GetString(buffer).Contains("<html>"))
            {
                System.Diagnostics.Debug.WriteLine("MediafireResolver: Resolving page...");
                Uri m;
                using (StreamReader sr = new StreamReader(s))
                {
                    var c = sr.ReadToEnd();
                    m = new Uri(Regex.Match(c, "kNO = \"(.+)\"").Groups[1].Value);
                }
                s.Close();
                s.Dispose();

                url = m;
                goto retry1;
            }
            else
            {
                Console.WriteLine("MediafireResolver: No need for extensive resolving.");
                ms.Write(buffer, 0, (int)length);
            }

            length = long.Parse(headers["content-length"]);

            buffer = new byte[1024];
            Task.Factory.StartNew(() =>
            {
                while (ms.Position < length)
                {
                    var l = s.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, l);
                }
            });
            Task.Factory.StartNew(() =>
            {
                while (ms.Position < length)
                {
                    OnStatusChanged(ms.Position / length, StatusType.Downloading);
                    System.Threading.Thread.Sleep(50);
                }
            });
            while (ms.Position < length)
            {
                System.Threading.Thread.Sleep(50);
            }

            OnStatusChanged(1, StatusType.Downloading);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            s.Close();
            s.Dispose();

            return ms;
        }
    }
}
