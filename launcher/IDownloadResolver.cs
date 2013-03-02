using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ModernMinas.Launcher
{
    public interface IDownloadResolver
    {
        public WebRequest ResolveFile(Uri uri);
        public string GetCurrentFileHash(Uri uri);
    }

    public class DownloadResolverBase : IDownloadResolver
    {
        public virtual WebRequest ResolveFile(Uri uri)
        {
            throw new NotImplementedException();
        }

        public virtual string GetCurrentFileHash(Uri uri)
        {
            throw new NotImplementedException();
        }
    }
}
