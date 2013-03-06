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
            var originUrl = new Uri(string.Format("http://mediafire.com/?{0}", Expand(resolverNode.SelectSingleNode("child::id").InnerText)));
            var url = originUrl;
            Uri oldM = null;
            
            MemoryStream ms = new MemoryStream();

        retry1:
            Log.InfoFormat("Currently targeted URL is {0}", url.ToString());
            Log.InfoFormat("Using {0} as referer", oldM);
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
                Log.InfoFormat("Server returned status: {0}", status);
                if (!new[] { "200", "301", "302", "303" }.Contains(status.Split(' ')[1]))
                    throw new WebException("HTTP error: " + status);
                string line = "";
                while ((line = sr.ReadLine().Trim()).Any())
                {
                    var l = line.Split(':');
                    string n = l[0].ToLower();
                    string v = string.Join(":", l.Skip(1)).Substring(1);
                    Log.InfoFormat("Server returned header: {0} = {1}", n, v);
                    headers.Add(n, v);
                }
            }
            oldM = url;
            if (headers.ContainsKey("location"))
            {
                Log.InfoFormat("Server redirected us to {0}", headers["location"]);
                url = new Uri(url, headers["location"]);
                goto retry1;
            }
            
            long length;
            byte[] buffer;

            if (headers.ContainsKey("content-type") && headers["content-type"].Contains("text/html"))
            {
                Log.Info("We were served an HTML page, resolving it to the actual download link");
                Uri m;
                using (StreamReader sr = new StreamReader(s))
                {
                    var c = sr.ReadToEnd();

                    // download_repair.php, fixes empty URI exception.
                    if (c.Contains("There was a problem with your download"))
                    {
                        Log.WarnFormat("Mediafire reported a broken download, going back to {0}", originUrl);
                        m = originUrl;
                    }
                    else
                    {
                        // Find javascripted URI of direct download
                        m = new Uri(Regex.Match(c, "kNO = \"(.+)\"").Groups[1].Value);
                        Log.InfoFormat("Resolved to: {0}", m.ToString());
                    }
                }
                s.Close();
                s.Dispose();

                url = m;
                goto retry1;
            }
            else
            {
                Log.Info("We were served a non-HTML file, downloading");
            }

            if (!headers.ContainsKey("content-length"))
            {
                Log.Error("Content-length header missing.");
                throw new Exception("Mediafire server did not send back content-length header. Please contact the developer.");
            }

            length = long.Parse(headers["content-length"]);
            Log.InfoFormat("Content-length is {0}.", length);

            buffer = new byte[2048];
            Task.Factory.StartNew(() =>
            {
                var l0 = 0;
                var l = 1;
                while (l0 < 3) // ignore length for debugging purposes
                {
                    l = s.Read(buffer, 0, buffer.Length);
                    if (l == 0)
                    {
                        l0++;
                        continue;
                    }
                    else
                    {
                        l0 = 0;
                        ms.Write(buffer, 0, l);
                    }
                }
                
                length = ms.Position;
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
            Log.InfoFormat("Download finished (received {0} bytes).", ms.Position);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            s.Close();
            s.Dispose();
            Log.Info("File saved.");

            return ms;
        }
    }
}
