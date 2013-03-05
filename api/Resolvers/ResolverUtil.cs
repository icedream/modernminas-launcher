using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace ModernMinas.Update.Api.Resolvers
{
    public class ResolverUtil
    {
        private static ILog _log;
        protected static ILog Log { get { if (_log == null) _log = LogManager.GetLogger(typeof(ResolverUtil)); return _log; } }

        public static ResolverBase GetResolverByName(string name)
        {
            Log.DebugFormat("Getting resolver for id {0}", name);
            var a = (from type in Assembly.GetExecutingAssembly().GetTypes()
                    where type.IsSubclassOf(typeof(ResolverBase)) && ((ResolverNameAttribute)type.GetCustomAttributes(typeof(ResolverNameAttribute), false).First()).Name.Equals(name)
                     select type).First();
            Log.DebugFormat("Found resolver {0}", a.FullName);
            return (ResolverBase)Activator.CreateInstance(a);
        }

        public static List<ResolverBase> GetResolverChain(Package package)
        {
            return GetResolverChain(package._packageXmlNode.SelectSingleNode("child::source"));
        }

        public static List<ResolverBase> GetResolverChain(System.Xml.XmlNode packageResolverChainNode)
        {
            var resolvers = new List<ResolverBase>();
            foreach (var resolverNode in packageResolverChainNode.SelectNodes("child::resolver").OfType<System.Xml.XmlNode>())
            {
                if (resolverNode.Attributes["type"] == null)
                    throw new InvalidOperationException("Resolver misses a type attribute.");
                var resolver = GetResolverByName(resolverNode.Attributes["type"].Value);
                resolver.resolverNode = resolverNode;
                resolvers.Add(resolver);
            }
            return resolvers;
        }

    }
}
