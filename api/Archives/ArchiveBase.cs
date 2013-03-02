using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ModernMinas.Update.Api
{
    public abstract class ArchiveBase
    {
        public virtual void ExtractFile(string file, string targetFilePath)
        {
            throw new NotImplementedException();
        }
        public virtual string[] GetFileEntries()
        {
            throw new NotImplementedException();
        }
        public virtual void ExtractAllFiles(string targetFolderPath)
        {
            foreach (string fileEntry in GetFileEntries())
            {
                ExtractFile(fileEntry, Path.Combine(targetFolderPath, fileEntry.Replace('/', Path.DirectorySeparatorChar)));
            }
        }
        public virtual Stream OpenFile(string file)
        {
            throw new NotImplementedException();
        }
    }
}
