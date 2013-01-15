using Microsoft.Win32;
using System;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace ModernMinas.Launcher
{
    public static class JavaPath
    {
        public static Process CreateJava(params string[] parameters)
        {
            return JavaPath.ApplyProcessInfo("java.exe", parameters);
        }
        public static Process CreateJavaW(params string[] parameters)
        {
            return JavaPath.ApplyProcessInfo("javaw.exe", parameters);
        }
        private static Process ApplyProcessInfo(string binary, string[] parameters)
        {
            return new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(JavaPath.GetJavaBinaryPath(), binary),
                    Arguments = JavaPath.CombineParameters(parameters),
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
        }
        private static string CombineParameters(string[] parameters)
        {
            return "\"" + string.Join("\" \"", parameters) + "\"";
        }
        public static string GetJavaBinaryPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32Windows)
            {
                Console.WriteLine("This is Windows, so we are parsing %path%...");
                string path = Environment.GetEnvironmentVariable("PATH");
                foreach (string singlepath in path.Split(';'))
                {
                    Console.WriteLine(singlepath);
                    if (File.Exists(Path.Combine(singlepath, "java.exe")))
                    {
                        string homepath = Path.GetFullPath(singlepath);
                        Console.WriteLine("=> Contains java, using as bin path", homepath);
                        return homepath;
                    }
                    else
                        Console.WriteLine("=> Does not contain java.");
                }
            }
            else
                Console.WriteLine("This is not Windows, don't use Windows path parsing.");

            object obj = JavaPath.GetJavaHome();
            if (obj == null)
            {
                throw new JavaNotFoundException();
            }
            obj = Path.Combine(obj.ToString(), "bin");
            if (!Directory.Exists(obj.ToString()))
            {
                throw new JavaNotFoundException();
            }
            Console.WriteLine("Java binary path combined: {0}", obj.ToString());
            return obj.ToString();
        }
        public static string GetJavaHome()
        {
            object obj = JavaPath.GetJavaRegistry().OpenSubKey(JavaPath.GetJavaVersion());
            if (obj == null)
            {
                throw new JavaNotFoundException();
            }
            obj = ((RegistryKey)obj).GetValue("JavaHome", null);
            if (obj == null)
            {
                throw new JavaNotFoundException();
            }
            Console.WriteLine("Java home path from registry: {0}", obj.ToString());
            return obj.ToString();
        }
        public static RegistryKey GetJavaRegistry()
        {
            Console.WriteLine("Detecting java registry...");

            if (Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("JavaSoft") == null)
                throw new JavaNotFoundException();

            var rs = new[] {
                                        Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("JavaSoft").OpenSubKey("Java Development Kit"),
                                        Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("JavaSoft").OpenSubKey("Java Runtime Environment"),
                                        Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Wow6432Node") != null ? Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Wow6432Node").OpenSubKey("JavaSoft").OpenSubKey("Java Development Kit") : null,
                                        Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Wow6432Node") != null ? Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Wow6432Node").OpenSubKey("JavaSoft").OpenSubKey("Java Runtime Environment") : null,
            }.ToList();

            int u = -1;
            foreach(var i in rs)
                Console.WriteLine("\tReading: [{0}] = {1}", ++u, i);

            rs = rs.Where(r => r != null).ToList();
            u = -1;
            foreach (var i in rs)
                Console.WriteLine("\tSorting out: [{0}] = {1}", ++u, i);

            RegistryKey registryKey = rs.FirstOrDefault();
            Console.WriteLine("\tSelecting: {0}", registryKey);

            if (registryKey == null)
                throw new JavaNotFoundException();
            return registryKey;
        }
        public static string GetJavaVersion()
        {
            object value = JavaPath.GetJavaRegistry().GetValue("CurrentVersion", null);
            if (value == null)
            {
                throw new JavaNotFoundException();
            }
            Console.WriteLine("Current java version read from registry: {0}", value);
            return value.ToString();
        }
    }
}