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
        string memoryMapFileName;

        public override string ResolveToString()
        {
            if (resolverNode.SelectSingleNode("child::type") == null)
                throw new NotImplementedException();
            if (resolverNode.SelectSingleNode("child::url") == null)
                throw new InvalidOperationException();
            if (resolverNode.SelectSingleNode("child::type").InnerText != "string")
                throw new NotImplementedException();

            using (var wc = new WebClient())
                return wc.DownloadString(Expand(resolverNode.SelectSingleNode("child::url").InnerText));
        }

        public override Stream ResolveToStream()
        {
            if (resolverNode.SelectSingleNode("child::url") == null)
                throw new InvalidOperationException();

            if (resolverNode.SelectSingleNode("child::type") != null)
                if (resolverNode.SelectSingleNode("child::type").InnerText != "memfile")
                    throw new NotImplementedException();
            
            MemoryStream targetStream = new MemoryStream();

            using (var wc = new WebClient())
            {
                using (var input = wc.OpenRead(Expand(resolverNode.SelectSingleNode("child::url").InnerText)))
                {
                    input.CopyTo(targetStream);
                    targetStream.Flush();
                    targetStream.Seek(0, SeekOrigin.Begin);
                }
            }

            return targetStream;
        }
    }
}
