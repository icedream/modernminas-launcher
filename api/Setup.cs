using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using SharpCompress.Compressor.Deflate;

namespace ModernMinas.Update.Api
{
    public class Setup
    {
        XmlDocument _doc = new XmlDocument();
        WebClient _wc = new WebClient();

        public Repository Repository { get; set; }
        public CacheFile Cache { get { return Repository.Cache; } }

        public Setup(Uri uri, CacheFile cache)
            : this(uri, new Repository(uri, cache))
        {
        }

        public Setup(Uri uri, Repository repo)
        {
            using (var rs = _wc.OpenRead(new Uri(uri, "setup.dat"))) // remote access to compressed data
            using (var ds = new DeflateStream(rs, SharpCompress.Compressor.CompressionMode.Decompress)) // decompress (deflate)
                _doc.Load(ds); // load xml
            Repository = repo;
        }

        public IEnumerable<SetupPackage> Packages
        {
            get
            {
                foreach (var node in _doc.SelectNodes("//setup/package").OfType<XmlNode>())
                {
                    yield return new SetupPackage(node, Repository);
                }
            }
        }

        public string GetStartupClasspath()
        {
            List<Tuple<int, string>> classpaths = new List<Tuple<int, string>>();

            foreach (string id in Cache.GetAllCachedPackageIDs())
            {
                Package pkg = Repository.GetLocalPackage(id);
                if (!pkg._packageXmlNode.SelectNodes("//startup-classpath").OfType<XmlNode>().Any())
                    continue;
                foreach (var node in pkg._packageXmlNode.SelectNodes("//startup-classpath").OfType<XmlNode>())
                    classpaths.Add(new Tuple<int, string>(int.Parse(Resolvers.VariableResolver.Expand(node.Attributes["priority"].Value, pkg)), Resolvers.VariableResolver.Expand(node.Attributes["path"].Value, pkg)));
            }

            var cps = classpaths.OrderBy(tuple => -tuple.Item1);

            return string.Join(System.IO.Path.PathSeparator.ToString(), from cp in cps select cp.Item2);
        }
        public string GetStartupLibrarypath()
        {
            List<Tuple<int, string>> librarypaths = new List<Tuple<int, string>>();

            foreach (string id in Cache.GetAllCachedPackageIDs())
            {
                Package pkg = Repository.GetLocalPackage(id);
                if (!pkg._packageXmlNode.SelectNodes("//startup-librarypath").OfType<XmlNode>().Any())
                    continue;
                foreach (var node in pkg._packageXmlNode.SelectNodes("//startup-librarypath").OfType<XmlNode>())
                    librarypaths.Add(new Tuple<int, string>(int.Parse(Resolvers.VariableResolver.Expand(node.Attributes["priority"].Value, pkg)), Resolvers.VariableResolver.Expand(node.Attributes["path"].Value, pkg)));
            }

            var lps = librarypaths.OrderBy(tuple => -tuple.Item1);

            return string.Join(System.IO.Path.PathSeparator.ToString(), from lp in lps select lp.Item2);
        }
    }
}
