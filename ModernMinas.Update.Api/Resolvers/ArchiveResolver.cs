using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernMinas.Update.Api
{
    using Archives;

    namespace Resolvers
    {
        [ResolverName("archive")]
        public class ArchiveResolver : ResolverBase
        {
            public override ArchiveBase ResolveToArchive()
            {
                if (!this.Input.GetType().IsSubclassOf(typeof(Stream)))
                    throw new InvalidOperationException();

                string password = null;
                if (resolverNode.SelectSingleNode("child::password") != null)
                    password = Expand(resolverNode.SelectSingleNode("child::password").InnerText);

                return new Archive(((Stream)this.Input), password);
            }
        }
    }

}