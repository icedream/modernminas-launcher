using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

using ModernMinas.Update.Api;
using ModernMinas.Launcher;

namespace PackageManager
{
    class Program
    {
        static Assembly _thisAssembly { get { return Assembly.GetExecutingAssembly(); } }
        static Process _thisProcess { get { return Process.GetCurrentProcess(); } }

        static T GetAttribute<T>() where T : Attribute
        { return (T)_thisAssembly.GetCustomAttributes(typeof(T), true).First(); }

        static void Version()
        {
            Console.WriteLine(GetAttribute<AssemblyTitleAttribute>().Title);
            Console.WriteLine("\tVersion {0}", _thisAssembly.GetName().Version.ToString());
            Console.WriteLine("\t{0}", GetAttribute<AssemblyTrademarkAttribute>().Trademark);
            Console.WriteLine();
        }

        static CacheFile Cache { get; set; }
        static Repository Repository { get; set; }

        static string cacheFile = "installation.cache";
        static string repositoryUrl = "http://repo.update.modernminas.de/";
        static string targetDirectory = System.IO.Path.Combine(Environment.CurrentDirectory);

        static void Main(string[] args)
        {

            var switches = (from a in args where a.StartsWith("-") && a.Split('=').Count() > 1 select a.Split('=')).ToArray();
            foreach(var s in switches)
            {
                string switchname = s[0];
                string switchvalue = string.Join("=", s.Skip(1)).Trim('"');

                switch (switchname.ToLower())
                {
                    case "--repository":
                        repositoryUrl = switchvalue;
                        break;
                    case "--cache":
                        cacheFile = switchvalue;
                        break;
                    case "--target":
                        targetDirectory = switchvalue;
                        break;
                    default:
                        Console.WriteLine("Unknown switch \"{0}\"", switchname);
                        break;
                }
            }

            args = (from a in args where !a.StartsWith("-") select a).ToArray();

            Version();

            if (!args.Any())
            {
                Usage();
                return;
            }

            Console.WriteLine("Using repository url: {0}", repositoryUrl);
            Console.WriteLine("Using cache file: {0}", cacheFile);
            Console.WriteLine("Using target directory: {0}", targetDirectory);

            Cache = new CacheFile(cacheFile);
            Repository = new Repository(new Uri(repositoryUrl), Cache);
            Repository.TargetDirectory = new System.IO.DirectoryInfo(targetDirectory);

            switch (args.First().ToLower())
            {
                case "install":
                    {
                        var packages = args.Skip(1);
                        foreach (string package in packages)
                        {
                            if (Cache.IsCached(package))
                            {
                                Console.WriteLine("Package \"{0}\" already installed, can't install.", package);
                                continue;
                            }

                            Console.WriteLine("Package \"{0}\" now installing...", package);
                            Repository.InstallPackage(package);
                        }
                        Console.WriteLine("Finished.");
                    }
                    break;
                case "uninstall":
                    {
                        var packages = args.Skip(1);
                        foreach (string package in packages)
                        {
                            if (!Cache.IsCached(package))
                            {
                                Console.WriteLine("Package \"{0}\" is not installed, can't uninstall.", package);
                                continue;
                            }

                            Console.WriteLine("Package \"{0}\" now uninstalling...", package);
                            Repository.UninstallPackage(package);
                        }
                        Console.WriteLine("Finished.");
                    }
                    break;
                case "update":
                    {
                        var packages = args.Skip(1);
                        foreach (string package in packages)
                        {
                            if (!Cache.IsCached(package))
                            {
                                Console.WriteLine("Package \"{0}\" is not installed, can't update.", package);
                                continue;
                            }

                            Console.WriteLine("Package \"{0}\" now updating...", package);
                            Repository.UninstallPackage(package);
                        }
                        Console.WriteLine("Finished.");
                    }
                    break;
                default:
                    Console.WriteLine("Unknown mode \"{0}\"", args.First());
                    break;
            }
        }

        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\t{0} [--switches here] [install|update|uninstall] [packages...]", _thisProcess.ProcessName);
            Console.WriteLine();
            Console.WriteLine("install");
            Console.WriteLine("\tInstalls a package if it isn't installed yet");
            Console.WriteLine("update");
            Console.WriteLine("\tUpdates an already installed package");
            Console.WriteLine("uninstall");
            Console.WriteLine("\tUninstalls an installed package");
            Console.WriteLine();
            Console.WriteLine("Additionally, you can apply switches anywhere in the command line, preferably before you provide which mode the manager uses for the packages:");
            Console.WriteLine("\t--repository=...\tDefines the repository url (default: {0})", repositoryUrl);
            Console.WriteLine("\t--cache=...\tDefines which cache file to use (default: {0})", cacheFile);
            Console.WriteLine("\t--target=...\tDefines the directory in which to install the packages (default: {0})", targetDirectory);
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\t{0} install minecraft-client", _thisProcess.ProcessName);
            Console.WriteLine("\t{0} update minecraft-forge optifine shaders", _thisProcess.ProcessName);
        }
    }
}
