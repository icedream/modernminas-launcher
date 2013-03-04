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
        public Package(XmlNode packageNode, Repository repository)
        {
            _packageXmlNode = packageNode;
            _repository = repository;

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
        public string ID { get { return _packageXmlNode.Attributes["id"].Value; } }
        public /*Dependency[]*/ /*Package[]*/ string[] Dependencies
        {
            get
            {
                if (_packageDependenciesXmlNode == null)
                    return new string[0];
                return (
                            from n in _packageDependenciesXmlNode.SelectNodes("package").OfType<XmlNode>()
                            select /*Dependency.FromDependencyXmlNode(n)*/ /*_repository.GetPackage(n.Attributes["id"].Value)*/ n.Attributes["id"].Value
                       ).ToArray();
            }
        }
        public Selector[] Selectors { get; set; }

        internal Repository _repository;
        internal XmlNode _packageXmlNode;
        internal XmlNode _packageResolverChainXmlNode { get { return _packageXmlNode.SelectSingleNode("child::source"); } }
        internal XmlNode _packageInstallXmlNode { get { return _packageXmlNode.SelectSingleNode("child::install"); } }
        internal XmlNode _packageUninstallXmlNode { get { return _packageXmlNode.SelectSingleNode("child::uninstall"); } }
        internal XmlNode _packageDependenciesXmlNode { get { return _packageXmlNode.SelectSingleNode("child::dependencies"); } }
        internal List<ResolverBase> _resolvers = new List<ResolverBase>();
        internal ArchiveBase _archive;

        /// <summary>
        /// Uninstalls the package. Note, that dependencies need to be resolved manually when you call this function.
        /// </summary>
        public void Uninstall(DirectoryInfo targetDirectory)
        {
            Console.WriteLine("Uninstalling package: {0}", this.Name);
            ProcessNode(new XmlNodeReader(_packageUninstallXmlNode), targetDirectory);
        }

        /// <summary>
        /// Installs the package. Note, that dependencies need to be resolved manually when you call this function.
        /// </summary>
        public void Install(DirectoryInfo targetDirectory)
        {
            Console.WriteLine("Installing package: {0}", this.Name);
            GetSourceArchive();
            ProcessNode(new XmlNodeReader(_packageInstallXmlNode), targetDirectory);
        }

        internal void ProcessNode(XmlNodeReader reader, DirectoryInfo targetDirectory)
        {
            StatusEventArgs e = new StatusEventArgs(this, StatusType.Parsing, 0);
            float step = 1f / _packageInstallXmlNode.ChildNodes.OfType<XmlNode>().Count();

            do
            {
                _repository.OnStatusChanged(e);
                switch (reader.Name.ToLower())
                {
                    case "extract":
                        {
                            string file = VariableResolver.Expand(reader.GetAttribute("file"), this);
                            string targetpath = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));

                            Directory.CreateDirectory(Path.GetDirectoryName(targetpath));
                            _archive.ExtractFile(file, targetpath);
                        }
                        break;
                    case "extract-all":
                        {
                            string targetfolder = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetfolder"), this));

                            _archive.ExtractAllFiles(targetfolder);
                        }
                        break;
                    case "extract-filter":
                        {
                            string filter = VariableResolver.Expand(reader.GetAttribute("filter"), this);
                            string targetfolder = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetfolder"), this));

                            if (string.IsNullOrEmpty(filter))
                                filter = "*";

                            foreach (string entry in FileFilterUtil.FilterFiles(_archive.GetFileEntries(), filter))
                            {
                                string targetpath = Path.Combine(targetfolder.Replace('/', Path.DirectorySeparatorChar), entry.Replace('/', Path.DirectorySeparatorChar));
                                Directory.CreateDirectory(Path.GetDirectoryName(targetpath));
                                _archive.ExtractFile(entry, targetpath);
                            }
                            e.Progress += step;
                        }
                        break;
                    case "delete":
                        {
                            string targetpath = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));

                            File.Delete(targetpath);
                            e.Progress += step;
                        }
                        break;
                    case "delete-folder":
                        {
                            string filter = VariableResolver.Expand(reader.GetAttribute("filter"), this);
                            string folder = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetfolder"), this));
                            var d = new DirectoryInfo(folder);
                            foreach (var file in d.EnumerateFiles(string.IsNullOrEmpty(filter) ? "*" : filter, SearchOption.AllDirectories))
                            {
                                file.Delete();
                                if (!file.Directory.EnumerateFiles().Any())
                                    file.Directory.Delete();
                            }
                            if (!d.EnumerateFiles().Any())
                                d.Delete();
                            e.Progress += step;
                        }
                        break;
                    case "uninject":
                        {
                            string target = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));
                            string filter = VariableResolver.Expand(reader.GetAttribute("filter"), this);

                            Directory.CreateDirectory(Path.GetDirectoryName(target));

                            ZipArchive targetZip;
                            if (File.Exists(target))
                                targetZip = ZipArchive.Open(target);
                            else
                            {
                                targetZip = ZipArchive.Create();
                                break;
                            }
                            targetZip.DeflateCompressionLevel = SharpCompress.Compressor.Deflate.CompressionLevel.BestCompression;

                            foreach (ZipArchiveEntry entry in from x in targetZip.Entries where FileFilterUtil.IsMatch(x.FilePath, filter) select x)
                            {
                                targetZip.RemoveEntry(entry);
                            }

                            targetZip.SaveTo(target + ".~", CompressionType.Deflate);
                            targetZip.Dispose();

                            File.Delete(target);
                            File.Move(target + ".~", target);
                            e.Progress += step;
                        }
                        break;
                    case "inject":
                        {
                            string target = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));
                            string filter = VariableResolver.Expand(reader.GetAttribute("filter"), this);

                            Directory.CreateDirectory(Path.GetDirectoryName(target));

                            ZipArchive targetZip;
                            if (File.Exists(target))
                                targetZip = ZipArchive.Open(target);
                            else
                                targetZip = ZipArchive.Create();
                            targetZip.DeflateCompressionLevel = SharpCompress.Compressor.Deflate.CompressionLevel.BestCompression;

                            foreach (string entry in FileFilterUtil.FilterFiles(_archive.GetFileEntries(), filter))
                            {
                                if (entry.EndsWith("/") || entry.EndsWith(Path.DirectorySeparatorChar.ToString())) continue;

                                MemoryStream ms = new MemoryStream();
                                _archive.OpenFile(entry).CopyTo(ms);
                                ms.Flush();
                                ms.Seek(0, SeekOrigin.Begin);
                                var entriesWhichAreSame = (from zipentry in targetZip.Entries where zipentry.FilePath.Replace(Path.DirectorySeparatorChar, '\\') == entry select zipentry).ToArray();
                                if (entriesWhichAreSame.Any())
                                    foreach (var zipentry in entriesWhichAreSame)
                                        targetZip.RemoveEntry(zipentry);
                                targetZip.AddEntry(entry, ms);
                            }

                            targetZip.SaveTo(target + ".~", CompressionType.Deflate);
                            targetZip.Dispose();

                            File.Delete(target);
                            File.Move(target + ".~", target);
                            e.Progress += step;
                        }
                        break;
                    case "move":
                        {
                            string sourcePath = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("path"), this));
                            string targetPath = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));

                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                            File.Move(sourcePath, targetPath);
                            e.Progress += step;
                        }
                        break;
                    case "download":
                        {
                            string sourcePath = VariableResolver.Expand(reader.GetAttribute("url"), this);
                            string targetPath = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));

                            e.Status = StatusType.Downloading;
                            _repository.OnStatusChanged(e);
                            new System.Net.WebClient().DownloadFile(new Uri(sourcePath), targetPath);

                            e.Progress += step;
                        }
                        break;
                }
            } while (reader.Read());
            _repository.OnStatusChanged(e);
        }

        /// <summary>
        /// Resolves and fetches the archive containing all files of this package.
        /// </summary>
        /// <returns>ArchiveBase</returns>
        public ArchiveBase GetSourceArchive()
        {
            if (_archive != null)
                return _archive;

            object lastResult = null;

            float[] progress = new float[_resolvers.Count];
            int i = -1;
            foreach (ResolverBase resolver in _resolvers)
            {
                var eventHandler = new EventHandler<StatusEventArgs>((sender, e) =>
                {
                    progress[i] = e.Progress;
                });
                resolver.StatusChanged += eventHandler;
                resolver.Input = lastResult;
                i++;
                foreach (var method in from m in resolver.GetType().GetMethods() where m.Name.StartsWith("ResolveTo") select m)
                {
                    System.Diagnostics.Debug.WriteLine("Trying method {0} when input is {1}NULL.", method.Name, resolver.Input == null ? "": "NOT ");
                    try
                    {
                        try
                        {
                            lastResult = method.Invoke(resolver, null);
                            System.Diagnostics.Debug.WriteLine("Method {0} successful.", method.Name, null);
                        }
                        catch (System.Reflection.TargetInvocationException err)
                        {
                            System.Diagnostics.Debug.WriteLine("Method {0} failed (TargetInvocationException).", method.Name, null);
                            System.Diagnostics.Debug.WriteLine(err.ToString());
                            throw err.InnerException;
                        }
                        break;
                    }
                    catch (NotImplementedException)
                    {
                        System.Diagnostics.Debug.WriteLine("Method {0} rejected.", method.Name, null);
                        continue;
                    }
                    catch (Exception err)
                    {
                        System.Diagnostics.Debug.WriteLine("Method {0} failed.", method.Name, null);
                        //System.Diagnostics.Debug.WriteLine("Exception of type {0}", err.GetType().Name, null);
                        System.Diagnostics.Debug.WriteLine(err.ToString());
                        throw err;
                    }
                }

                resolver.StatusChanged -= eventHandler;
            }

            if (lastResult.GetType().IsSubclassOf(typeof(ArchiveBase)))
                return _archive = lastResult as ArchiveBase;

            throw new InvalidOperationException("Package did not resolve to an archive.");
        }
    }
}
