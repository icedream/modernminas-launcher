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
using SharpCompress.Common;
using SharpCompress.Common.Zip;
using log4net;

namespace ModernMinas.Update.Api
{
    using Resolvers;

    public class Package
    {
        private ILog _log;
        protected ILog Log { get { if (_log == null) _log = LogManager.GetLogger(this.GetType()); return _log; } }

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

            Log.DebugFormat("Opened package, ID is {0}, Name is {1}, Version is {2}", ID, Name, Version);
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
            Log.InfoFormat("Uninstalling package: {0}", this.Name);
            ProcessNode(new XmlNodeReader(_packageUninstallXmlNode), targetDirectory);
        }

        /// <summary>
        /// Installs the package. Note, that dependencies need to be resolved manually when you call this function.
        /// </summary>
        public void Install(DirectoryInfo targetDirectory)
        {
            Log.InfoFormat("Installing package: {0}", this.Name);
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
                Log.DebugFormat("ProcessNode({1}): {0}", reader.Name, ID);
                switch (reader.Name.ToLower())
                {
                    case "extract":
                        {
                            string file = VariableResolver.Expand(reader.GetAttribute("file"), this);
                            string targetpath = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));

                            Log.InfoFormat("ProcessNode: Extracting {0} to {1}", file, targetpath);
                            Directory.CreateDirectory(Path.GetDirectoryName(targetpath));
                            _archive.ExtractFile(file, targetpath);
                        }
                        break;
                    case "extract-all":
                        {
                            string targetfolder = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetfolder"), this));

                            Log.InfoFormat("ProcessNode: Extracting {0} to {1}", "everything", targetfolder);
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
                                Log.InfoFormat("ProcessNode: Extracting {0} to {1}", entry, targetpath);
                                Directory.CreateDirectory(Path.GetDirectoryName(targetpath));
                                _archive.ExtractFile(entry, targetpath);
                            }
                            e.Progress += step;
                        }
                        break;
                    case "delete":
                        {
                            string targetpath = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetpath"), this));

                            Log.InfoFormat("ProcessNode: Deleting {0}", targetpath);
                            File.Delete(targetpath);
                            e.Progress += step;
                        }
                        break;
                    case "delete-folder":
                        {
                            string filter = VariableResolver.Expand(reader.GetAttribute("filter"), this);
                            string folder = Path.Combine(targetDirectory.FullName, VariableResolver.Expand(reader.GetAttribute("targetfolder"), this));
                            var d = new DirectoryInfo(folder);
                            foreach (var file in d.GetFiles(string.IsNullOrEmpty(filter) ? "*" : filter, SearchOption.AllDirectories))
                            {
                                Log.InfoFormat("ProcessNode: Deleting {0}", file.FullName);
                                file.Delete();
                                if (!file.Directory.GetFiles().Any())
                                    file.Directory.Delete();
                            }
                            if (!d.GetFiles().Any())
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
                                Log.InfoFormat("ProcessNode: Removing {0} from {1}", entry.FilePath, target);
                                targetZip.RemoveEntry(entry);
                            }

                            Log.InfoFormat("ProcessNode: Updating {0}", target);
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
                                using (var s = _archive.OpenFile(entry))
                                {
                                    //s.CopyTo(ms);
                                    int i = 1;
                                    while (i > 0)
                                    {
                                        byte[] buffer = new byte[2048];
                                        i = s.Read(buffer, 0, buffer.Length);
                                        if (i > 0)
                                            ms.Write(buffer, 0, i);
                                    }
                                }
                                ms.Flush();
                                ms.Seek(0, SeekOrigin.Begin);
                                var entriesWhichAreSame = (from zipentry in targetZip.Entries where zipentry.FilePath.Replace(Path.DirectorySeparatorChar, '\\') == entry select zipentry).ToArray();
                                if (entriesWhichAreSame.Any())
                                    foreach (var zipentry in entriesWhichAreSame)
                                    {
                                        Log.InfoFormat("ProcessNode: Removing {0} from {1}", zipentry.FilePath, target);
                                        targetZip.RemoveEntry(zipentry);
                                    }
                                Log.InfoFormat("ProcessNode: Adding {0} to {1}", entry, target);
                                targetZip.AddEntry(entry, ms);
                            }

                            Log.InfoFormat("ProcessNode: Updating {0}", target);
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

                            Log.InfoFormat("ProcessNode: Moving {0} to {1}", sourcePath, targetPath);
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
                            Log.InfoFormat("ProcessNode: Downloading {0} to {1}", sourcePath, targetPath);
                            new System.Net.WebClient().DownloadFile(new Uri(sourcePath), targetPath);

                            e.Progress += step;
                        }
                        break;
                    default:
                        Log.WarnFormat("ProcessNode: Unknown step {0}", reader.Name);
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
                Log.DebugFormat("GetSourceArchive({0}): Current resolver is {0}", ID, resolver.GetType().Name);

                var eventHandler = new EventHandler<StatusEventArgs>((sender, e) =>
                {
                    progress[i] = e.Progress;
                });
                resolver.StatusChanged += eventHandler;
                resolver.Input = lastResult;
                i++;
                foreach (var method in from m in resolver.GetType().GetMethods() where m.Name.StartsWith("ResolveTo") select m)
                {
                    Log.DebugFormat("GetSourceArchive: Trying method {0} when input is {1}NULL.", method.Name, resolver.Input == null ? "" : "NOT ");
                    try
                    {
                        try
                        {
                            lastResult = method.Invoke(resolver, null);
                            Log.DebugFormat("GetSourceArchive: Method {0} successful.", method.Name);
                        }
                        catch (System.Reflection.TargetInvocationException err)
                        {
                            Log.WarnFormat("GetSourceArchive: Method {0} failed (TargetInvocationException).", method.Name);
                            Log.Warn("GetSourceArchive: TargetInvocationException detected", err);
                            throw err.InnerException;
                        }
                        break;
                    }
                    catch (NotImplementedException)
                    {
                        Log.DebugFormat("GetSourceArchive: Method {0} rejected.", method.Name, null);
                        continue;
                    }
                    catch (Exception err)
                    {
                        Log.WarnFormat("GetSourceArchive: Method {0} failed", method.Name);
                        Log.Warn("Exception detected", err);
                        throw err;
                    }
                }

                resolver.StatusChanged -= eventHandler;
            }

            if (lastResult.GetType().IsSubclassOf(typeof(ArchiveBase)))
            {
                Log.Fatal("GetSourceArchive: Package resolved.");
                return _archive = lastResult as ArchiveBase;
            }

            Log.Fatal("GetSourceArchive: Package not resolvable: Result is not an archive.");
            throw new InvalidOperationException("Package did not resolve to an archive.");
        }
    }
}
