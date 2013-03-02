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
        private static string GetPlatformString()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return "osx";
                case PlatformID.Unix:
                    return "linux";
                case PlatformID.Xbox:
                    return "xbox";
                default:
                    return "windows";
            }
        }

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

        public static string Expand(string value, Package package)
        {
            if (value == null)
                return null;

            value = value
                .Replace("%{PLATFORM}", GetPlatformString())
                .Replace("%{ID}", package.ID)
                ;
            //Console.WriteLine("ResolverBase: Expanding input is: {0}", value);

            //Console.WriteLine("ResolverBase: Searching %{*}");
            var m = Regex.Matches(value, @"%\{([A-z0-9]+)}");
            //Console.WriteLine("ResolverBase: Found {0} matches", m.Count);

            foreach (var match in m.OfType<Match>())
            {
                //Console.WriteLine("ResolverBase: Expanding {0}", match.Groups[0].Value);
                var node = package._packageXmlNode.SelectSingleNode(string.Format("child::{0}", match.Groups[1].Value.ToLower()));
                if (node != null)
                {
                    value = value.Replace(
                        match.Groups[0].Value,
                        node.InnerText
                    );
                }
                else
                {
                    //Console.WriteLine("ResolverBase: WARNING: Not expandable, meta variable does not exist.");
                }
            }

            return value;
        }

        protected string Expand(string value)
        {
            value = VariableResolver.ExpandInternal(value);

            if(Input != null)
                value = value.Replace("%{PIPE}", Input.ToString());

            value = value
                .Replace("%{PLATFORM}", GetPlatformString())
                .Replace("%{ID}", resolverNode.ParentNode.ParentNode.Attributes["id"].Value);

            //Console.WriteLine("ResolverBase: Expanding input is: {0}", value);

            //Console.WriteLine("ResolverBase: Searching %{*}");
            var m = Regex.Matches(value, @"%\{([A-z0-9]+)}");
            //Console.WriteLine("ResolverBase: Found {0} matches", m.Count);

            foreach (var match in m.OfType<Match>())
            {
                //Console.WriteLine("ResolverBase: Expanding {0}", match.Groups[0].Value);
                var node = resolverNode.ParentNode.ParentNode.SelectSingleNode(string.Format("child::{0}", match.Groups[1].Value.ToLower()));
                if (node != null)
                {
                    value = value.Replace(
                        match.Groups[0].Value,
                        node.InnerText
                    );
                }
                else
                {
                    //Console.WriteLine("ResolverBase: WARNING: Not expandable, meta variable does not exist.");
                }
            }

            return value;
        }

        public string Name
        { get { return ((ResolverNameAttribute)GetType().GetCustomAttributes(typeof(ResolverNameAttribute), false).First()).Name; } }

        public event EventHandler<StatusEventArgs> StatusChanged;

        protected void OnStatusChanged(float progress, StatusType status = StatusType.Parsing)
        {
            if (StatusChanged != null)
                StatusChanged(this, new StatusEventArgs(null, status, progress));
        }
    }
}
