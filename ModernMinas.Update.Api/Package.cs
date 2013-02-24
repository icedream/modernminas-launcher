using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using SharpCompress.Archive;
using SharpCompress.Archive.Zip;
using SharpCompress.IO;
using SharpCompress.Common;
using SharpCompress.Common.Zip;

namespace ModernMinas.Update.Api
{
    using Resolvers;

    public class Package
    {
        public Package(XmlNode packageNode)
        {
            _packageXmlNode = packageNode;

            var metadataNodes = new[] {
                packageNode.SelectSingleNode("child::name"),
                packageNode.SelectSingleNode("child::version"),
                packageNode.SelectSingleNode("child::description")
            };

            if (metadataNodes[0] == null)
                throw new InvalidOperationException("Package XML is missing a name tag.");
            if (metadataNodes[1] == null)
                throw new InvalidOperationException("Package XML is missing a version tag.");

            Name = metadataNodes[0].InnerText;
            Version = metadataNodes[1].InnerText;
            Description = metadataNodes[2] != null ? metadataNodes[2].InnerText : string.Empty;

            _resolvers = ResolverUtil.GetResolverChain(this);

            var optionsNode = packageNode.SelectSingleNode("child::options");
            if (optionsNode != null)
                Selectors = (from xmlNode in optionsNode.SelectNodes("child::selector").OfType<XmlNode>()
                             select new Selector(xmlNode)).ToArray();
            else
                Selectors = new Selector[] { };
        }

        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public Dependency[] Dependencies
        {
            get
            {
                return (
                            from n in _packageDependenciesXmlNode.SelectNodes("dependency").OfType<XmlNode>()
                            select Dependency.FromDependencyXmlNode(n)
                       ).ToArray();
            }
        }
        public Selector[] Selectors { get; set; }

        internal XmlNode _packageXmlNode;
        internal XmlNode _packageResolverChainXmlNode { get { return _packageXmlNode.SelectSingleNode("child::source"); } }
        internal XmlNode _packageInstallXmlNode { get { return _packageXmlNode.SelectSingleNode("child::install"); } }
        internal XmlNode _packageUninstallXmlNode { get { return _packageXmlNode.SelectSingleNode("child::uninstall"); } }
        internal XmlNode _packageDependenciesXmlNode { get { return _packageXmlNode.SelectSingleNode("child::dependencies"); } }
        internal List<ResolverBase> _resolvers = new List<ResolverBase>();
        internal ArchiveBase _archive;

        public void Uninstall()
        {
            ProcessNode(new XmlNodeReader(_packageUninstallXmlNode));
        }

        public void Install()
        {
            ProcessNode(new XmlNodeReader(_packageInstallXmlNode));
        }

        public void ProcessNode(XmlNodeReader reader)
        {
            GetSourceArchive();
            do
            {
                switch (reader.Name.ToLower())
                {
                    case "extract":
                        {
                            string file = reader.GetAttribute("file");
                            string targetpath = reader.GetAttribute("targetpath");

                            _archive.ExtractFile(file, targetpath);
                        }
                        break;
                    case "extract-all":
                        {
                            string targetfolder = reader.GetAttribute("targetfolder");
                            _archive.ExtractAllFiles(targetfolder);
                        }
                        break;
                    case "extract-filter":
                        {
                            string filter = reader.GetAttribute("filter");
                            string targetfolder = reader.GetAttribute("targetfolder");

                            if (string.IsNullOrEmpty(filter))
                                filter = "*";

                            foreach (string entry in FileFilterUtil.FilterFiles(_archive.GetFileEntries(), filter))
                            {
                                string targetpath = Path.Combine(targetfolder.Replace('/', Path.DirectorySeparatorChar), entry.Replace('/', Path.DirectorySeparatorChar));
                                _archive.ExtractFile(entry, targetpath);
                            }
                        }
                        break;
                    case "delete":
                        {
                            File.Delete(reader.GetAttribute("path"));
                        }
                        break;
                    case "delete-folder":
                        {
                            string filter = reader.GetAttribute("filter");
                            string folder = reader.GetAttribute("folder");
                            foreach (var file in new DirectoryInfo(folder).EnumerateFiles(string.IsNullOrEmpty(filter) ? "*" : filter, SearchOption.AllDirectories))
                            {
                                file.Delete();
                                if (!file.Directory.EnumerateFiles().Any())
                                    file.Directory.Delete();
                            }
                        }
                        break;
                    case "inject":
                        {
                            string target = reader.GetAttribute("target");
                            string filter = reader.GetAttribute("filter");

                            ZipArchive targetZip = ZipArchive.Open(target);

                            foreach (string entry in FileFilterUtil.FilterFiles(_archive.GetFileEntries(), filter))
                                using (var sourceStream = _archive.OpenFile(entry))
                                    targetZip.AddEntry(entry, sourceStream);
                        }
                        break;
                    case "move":
                        {
                            string sourcePath = reader.GetAttribute("path");
                            string targetPath = reader.GetAttribute("target");

                            File.Move(sourcePath, targetPath);
                        }
                        break;
                        /*
                    case "reinstall":
                    case "update":
                        {
                            // TODO: Implement package management so packages can force reinstall of other packages
                        }
                        break;
                         */
                    case "download":
                        {
                            string sourcePath = reader.GetAttribute("url");
                            string targetPath = reader.GetAttribute("target");

                            new System.Net.WebClient().DownloadFile(sourcePath, targetPath);
                        }
                        break;
                }
            } while (reader.Read());
        }

        public ArchiveBase GetSourceArchive()
        {
            if (_archive != null)
                return _archive;

            object lastResult = null;
            VariableResolver.Clear();
            foreach (ResolverBase resolver in _resolvers)
            {
                resolver.Input = lastResult;
                foreach (var method in from m in resolver.GetType().GetMethods() where m.Name.StartsWith("ResolveTo") select m)
                {
                    try
                    {
                        lastResult = method.Invoke(resolver, null);
                    }
                    catch (NotImplementedException)
                    {
                        continue;
                    }
                    catch (NotSupportedException)
                    {
                        continue;
                    }
                }
            }

            if (lastResult.GetType().IsSubclassOf(typeof(ArchiveBase)))
                return _archive = lastResult as ArchiveBase;

            throw new InvalidOperationException("Package did not resolve to an archive.");
        }

    }
}
