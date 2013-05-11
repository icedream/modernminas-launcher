using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModernMinas.Update.Api.Resolvers
{
    [ResolverName("download")]
    public class DownloadResolver : ResolverBase
    {
        public override string ResolveToString()
        {
            OnStatusChanged(0);

            if (resolverNode.SelectSingleNode("child::type") == null)
                throw new NotImplementedException();
            if (resolverNode.SelectSingleNode("child::url") == null)
                throw new InvalidOperationException();
            if (resolverNode.SelectSingleNode("child::type").InnerText != "string")
                throw new NotImplementedException();

            string url = Expand(resolverNode.SelectSingleNode("child::url").InnerText);
            Log.InfoFormat("Downloading as string from URL: {0}", url);
            
            string resultString = null;

            using (var wc = new WebClient())
            {
                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) =>
                {
                    OnStatusChanged(e.BytesReceived / e.TotalBytesToReceive, StatusType.Downloading);
                });
                wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler((sender, e) =>
                {
                    resultString = e.Result;
                });
                wc.DownloadStringAsync(new Uri(url));
                while (wc.IsBusy)
                    System.Threading.Thread.Sleep(10);
            }

            Log.InfoFormat("Download saved in string (actual string length is {0})", resultString.Length);
            return resultString;
        }

        public override Stream ResolveToStream()
        {
            if (resolverNode.SelectSingleNode("child::url") == null)
                throw new InvalidOperationException();

            if (resolverNode.SelectSingleNode("child::type") != null)
                if (resolverNode.SelectSingleNode("child::type").InnerText != "stream")
                    throw new NotImplementedException();

            string url = Expand(resolverNode.SelectSingleNode("child::url").InnerText);
            Log.InfoFormat("Downloading as memory stream from URL: {0}", url);

            MemoryStream targetStream = new MemoryStream();

            using (var wc = new WebClient())
            {
                using (var input = wc.OpenRead(url))
                {
                    long length = long.Parse(wc.ResponseHeaders[HttpResponseHeader.ContentLength]);
                    Log.InfoFormat("Download length (as described by header): {0}", length);

                    // Transfer thread
                    Task.Factory.StartNew(() =>
                    {
                        while (targetStream.Position < length)
                        {
                            byte[] buffer = new byte[1024];
                            var read = input.Read(buffer, 0, buffer.Length);
                            targetStream.Write(buffer, 0, read);
                        }
                    });

                    // Status thread
                    Task.Factory.StartNew(() =>
                    {
                        while (targetStream.Position < length)
                        {
                            OnStatusChanged(targetStream.Position / length, StatusType.Downloading);
                            System.Threading.Thread.Sleep(50);
                        }
                    });

                    while (targetStream.Position < length)
                    {
                        System.Threading.Thread.Sleep(50);
                    }

                    targetStream.Flush();
                    targetStream.Seek(0, SeekOrigin.Begin);
                }
            }

            Log.InfoFormat("Download cached in memory stream (actual file size is {0})", targetStream.Length);
            return targetStream;
        }
    }
}
