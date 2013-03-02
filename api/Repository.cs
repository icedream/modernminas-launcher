using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using SharpCompress.Compressor.Deflate;
using System.IO;

namespace ModernMinas.Update.Api
{
    public class Repository
    {
        public Repository(Uri repository, CacheFile cache)
        {
            Cache = cache;
            RepositoryBase = repository;

            Console.WriteLine("Using {0} as repo base", RepositoryBase.ToString());

            TargetDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        }

        public CacheFile Cache { get; set; }
        public Uri RepositoryBase { get; set; }
        public DirectoryInfo TargetDirectory { get; set; }
        private WebClient _wc = new WebClient();

        public event EventHandler<StatusEventArgs> StatusChanged;

        internal void OnStatusChanged(StatusEventArgs e)
        {
            if (StatusChanged != null)
                StatusChanged.Invoke(this, e);
        }

        private XmlNode GetRemotePackageXmlNode(string package)
        {
            package = package.ToLower();

            XmlDocument doc = new XmlDocument();
            var uri = new Uri(RepositoryBase, string.Format("packages/{0}.dat", package));
            //Console.WriteLine("Fetching package info for {0}: {1}", package, uri.ToString());
            using (var rs = _wc.OpenRead(uri)) // remote access to compressed data
                using (var ds = new DeflateStream(rs, SharpCompress.Compressor.CompressionMode.Decompress)) // decompress (deflate)
                    doc.Load(ds); // load xml
            return doc.FirstChild; // <package...>...</package>
        }

        public Package GetRemotePackage(string package)
        {
            return new Package(GetRemotePackageXmlNode(package), this);
        }

        private XmlNode GetLocalPackageXmlNode(string package)
        {
            package = package.ToLower();

            XmlDocument doc = new XmlDocument();
            var xml = Cache.GetCachedPackageXml(package);
            doc.LoadXml(xml);
            return doc.FirstChild; // <package...>...</package>
        }

        public Package GetLocalPackage(string package)
        {
            return new Package(GetLocalPackageXmlNode(package), this);
        }

        private void InstallDependenciesOfPackage(Package package)
        {
            //Console.WriteLine("Checking dependencies of package: {0}", package.Name);

            // TODO: Soft dependencies? See Package.cs and Dependency.cs for suggestions.
            var packages = from pkg in package.Dependencies select GetRemotePackage(pkg);

            int i = 0;
            int j = packages.Count();

            foreach (var currentPackage in packages)
            {
                OnStatusChanged(new StatusEventArgs(package, StatusType.CheckingDependencies, (float)(i / j)));

                // This expects all packages to be hard requirements ("needed to have this package working")
                if (!Cache.IsCached(currentPackage.ID)) // not installed?
                {
                    Console.WriteLine("Dependency needs to be installed: {0}", currentPackage.Name);
                    OnStatusChanged(new StatusEventArgs(package, StatusType.InstallingDependencies, (float)(i / j)));
                    InstallPackage(currentPackage);
                }

                i++;
            }
        }

        private void InstallPackage(Package package)
        {
            InstallDependenciesOfPackage(package);
            OnStatusChanged(new StatusEventArgs(package, StatusType.Installing, 0));
            package.Install(TargetDirectory);
            OnStatusChanged(new StatusEventArgs(package, StatusType.Finalizing, 1));
            Cache.CachePackage(package.ID, package.Version, package._packageXmlNode.OwnerDocument.OuterXml);
            OnStatusChanged(new StatusEventArgs(package, StatusType.Finished, 1));
        }

        public void InstallPackage(string package)
        {
            InstallPackage(GetRemotePackage(package));
        }

        public void UninstallPackage(Package package, bool checkDependencies = true)
        {
            if (checkDependencies)
            {
                OnStatusChanged(new StatusEventArgs(package, StatusType.CheckingDependencies, 0));
                Console.WriteLine("Checking for depending packages: {0}", package.Name);

                // Check depending packages before uninstall
                foreach (string xml in Cache.GetAllCachedPackageXml())
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);
                    if (doc.SelectNodes("//dependencies/package[@id=\"" + package.Name + "\"]").OfType<XmlNode>().Any())
                        throw new DependencyException(string.Format("Package {0} is depending on package {1} which was marked for uninstallation.", doc.SelectSingleNode("//package/name").InnerText, package.Name));
                }
            }

            OnStatusChanged(new StatusEventArgs(package, StatusType.Uninstalling, 0));
            package.Uninstall(TargetDirectory);
            OnStatusChanged(new StatusEventArgs(package, StatusType.Finalizing, 1));
            Cache.DeleteCache(package.ID);
            OnStatusChanged(new StatusEventArgs(package, StatusType.Finished, 1));
        }

        public void UninstallPackage(string package, bool checkDependencies = true)
        {
            UninstallPackage(GetLocalPackage(package), checkDependencies);
        }

        public void UpdatePackage(string package)
        {
            if (!Cache.IsCached(package))
                throw new InvalidOperationException(string.Format("Can't update a package which isn't installed: {0}", package));

            Package local = GetLocalPackage(package);
            Package remote = GetRemotePackage(package);

            OnStatusChanged(new StatusEventArgs(local, StatusType.CheckingUpdates, 0));

            if (local.Version.Equals(remote.Version))
            {
                OnStatusChanged(new StatusEventArgs(local, StatusType.Finished, 1));
                Console.WriteLine("Skipping {0}, no update to {1} needed.", local.Name, local.Version);
                return;
            }

            Console.WriteLine("Updating {0} from {1} to {2}:", local.Name, local.Version, remote.Version);
            UninstallPackage(local, false);
            InstallPackage(remote);
        }
    }
}
