using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ModernMinas.Update.Api.Resolvers
{
    public class ResolverBase : IResolver
    {
        public object Input { get; set; }

        public XmlNode resolverNode { get; set; }

        public virtual string ResolveToString()
        {
            throw new NotImplementedException();
        }

        public virtual ArchiveBase ResolveToArchive()
        {
            throw new NotImplementedException();
        }

        public virtual MemoryMappedFile ResolveToMemoryMappedFile()
        {
            throw new NotImplementedException();
        }

        public virtual Stream ResolveToStream()
        {
            throw new NotImplementedException();
        }

        protected string Expand(string value)
        {
            value = VariableResolver.ExpandInternal(value).Replace("%{PIPE}", Input.ToString());

            var m = Regex.Matches(value, @"%\{([A-z0-9])}");

            foreach (var match in m.OfType<Match>())
                value = value.Replace(
                    string.Format("%{1}{0}{2}", match.Captures[0].Value, "{", "}"),
                    resolverNode.ParentNode.SelectSingleNode(string.Format("child::{0}", match.Captures[0].Value)).InnerText
                );

            return value;
        }

        public string Name
        { get { return ((ResolverNameAttribute)GetType().GetCustomAttributes(typeof(ResolverNameAttribute), false).First()).Name; } }
    }
}
