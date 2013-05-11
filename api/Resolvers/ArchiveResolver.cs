using System;
using System.Collections.Generic;
using System.IO;
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
                OnStatusChanged(0);

                if (!this.Input.GetType().IsSubclassOf(typeof(Stream)))
                    throw new InvalidOperationException();

                string password = null;
                if (resolverNode.SelectSingleNode("child::password") != null)
                    password = Expand(resolverNode.SelectSingleNode("child::password").InnerText);

                var archive = new Archive(((Stream)this.Input), password);
                OnStatusChanged(1, StatusType.Finished);

                return archive;
            }
        }
    }

}