using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using log4net;

namespace ModernMinas.Update.Api.Resolvers
{
    public class ResolverBase : IResolver
    {
        private ILog _log;
        protected ILog Log { get { if (_log == null) _log = LogManager.GetLogger(this.GetType()); return _log; } }

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
            Log.Debug("Skipping string resolve.");
            throw new NotImplementedException();
        }

        public virtual ArchiveBase ResolveToArchive()
        {
            Log.Debug("Skipping archive resolve.");
            throw new NotImplementedException();
        }

        /*
        public virtual MemoryMappedFile ResolveToMemoryMappedFile()
        {
            Log.Debug("Skipping memory map file resolve.");
            throw new NotImplementedException();
        }
         */

        public virtual Stream ResolveToStream()
        {
            Log.Debug("Skipping stream resolve.");
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
            Log.DebugFormat("Expanding value input: {0}", value);

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

            Log.DebugFormat("Expanding value output: {0}", value);
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
