using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModernMinas.Update.Api.Resolvers
{
    interface IResolver
    {
        string ResolveToString();
        ArchiveBase ResolveToArchive();
        MemoryMappedFile ResolveToMemoryMappedFile();
        Stream ResolveToStream();
    }
}
