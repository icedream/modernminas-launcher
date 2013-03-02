using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
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

            //Console.WriteLine("DownloadResolver: Returning a string");
            string url = Expand(resolverNode.SelectSingleNode("child::url").InnerText);
            //Console.WriteLine("DownloadResolver: URL is {0}", url);
            
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

            MemoryStream targetStream = new MemoryStream();

            using (var wc = new WebClient())
            {
                using (var input = wc.OpenRead(url))
                {
                    long length = long.Parse(wc.ResponseHeaders[HttpResponseHeader.ContentLength]);
                    System.Diagnostics.Debug.WriteLine("Receiving {0} bytes", length, null);

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
                            OnStatusChanged(input.Position / input.Length, StatusType.Downloading);
                            System.Threading.Thread.Sleep(50);
                        }
                    });

                    while (targetStream.Position < length)
                        System.Threading.Thread.Sleep(10);

                    targetStream.Flush();
                    targetStream.Seek(0, SeekOrigin.Begin);
                }
            }

            System.Diagnostics.Debug.WriteLine("Returning a memory stream from DLResolver");
            return targetStream;
        }
    }
}
