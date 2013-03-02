using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModernMinas.Update.Api;
using ModernMinas.Launcher;
using System.Xml;
using System.IO;

namespace TestResolve
{
    class Program
    {
        static string GamePath = "TestInstallation1";
        static string GameBinPath { get { return string.Format("{0}/bin", GamePath); } }
        static string Librarypath = "";
        static string Classpath = "";

        static void Main(string[] args)
        {
            GamePath = Path.GetFullPath(GamePath);

            CacheFile cache = new CacheFile("installation.cache");
            Setup setup = new Setup(new Uri(Path.Combine(Environment.CurrentDirectory, "TestRepository1") + "/"), cache);
            setup.Repository.TargetDirectory = new DirectoryInfo(GamePath);
            foreach (var package in setup.Packages)
            {
                if (package.IsInstalled)
                    package.Update();
                else
                    package.Install();
            }

            Librarypath = setup.GetStartupLibrarypath();
            Classpath = setup.GetStartupClasspath();


            StartMinecraft();

            System.Threading.Thread.Sleep(-1);
        }

        static void StartMinecraft()
        {
            var tmpdir = new System.IO.DirectoryInfo(System.IO.Path.Combine(GamePath, "tmp"));
            if (tmpdir.Exists)
                tmpdir.Delete(true);
            tmpdir.Create();

            var javaw = JavaPath.CreateJava(new[] {
                "-Xmx1G",
                "-Xincgc",
                Environment.Is64BitProcess ? "-d64" : "-d32",
                string.Format("-Djava.library.path={0}", Librarypath),
                "-Djava.io.tmpdir=" + System.IO.Path.Combine(GamePath, "tmp"),
                "-cp", Classpath,
                "net.minecraft.client.Minecraft",
                "test", // user
                "12345", // session id
                "minas.mc.modernminas.de:25565"
            });
            javaw.StartInfo.WorkingDirectory = GamePath;
            if (javaw.StartInfo.EnvironmentVariables.ContainsKey("APPDATA"))
                javaw.StartInfo.EnvironmentVariables["APPDATA"] = GamePath;
            else
                javaw.StartInfo.EnvironmentVariables.Add("APPDATA", GamePath);
            if (javaw.StartInfo.EnvironmentVariables.ContainsKey("HOME"))
                javaw.StartInfo.EnvironmentVariables["HOME"] = GamePath;
            else
                javaw.StartInfo.EnvironmentVariables.Add("HOME", GamePath);
            javaw.StartInfo.RedirectStandardError = true;
            javaw.StartInfo.RedirectStandardOutput = true;
            javaw.StartInfo.CreateNoWindow = true;
            Console.WriteLine("Starting minecraft, arguments: {0}", javaw.StartInfo.Arguments, null);
            javaw.Start();

            // STDERR
            System.Threading.Tasks.Task.Factory.StartNew(
                () =>
                {
                    string lastError = null;
                    while (!javaw.HasExited)
                    {

                        lastError = javaw.StandardError.ReadLine();
                        if (lastError != null) lastError = lastError.Trim();
                        Console.WriteLine("[Minecraft] STDERR: {0}", lastError, null);

                    }
                    Console.WriteLine("[Minecraft] End of error stream");

                    // Error handling
                    if (javaw.ExitCode != 0 /* No error at all */
                        && javaw.ExitCode != -1 /* Closed via clicking "X" */)
                        Console.WriteLine("Minecraft error code {0}. {1}", javaw.ExitCode, lastError);
                    Environment.Exit(0);
                }, System.Threading.Tasks.TaskCreationOptions.AttachedToParent);

            // STDOUT
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                while (!javaw.HasExited)
                {
                    var lastError = javaw.StandardOutput.ReadLine();
                    if (lastError != null) lastError = lastError.Trim();
                    Console.WriteLine("[Minecraft] STDOUT: {0}", lastError, null);

                    if (lastError == null)
                        continue;

                    lastError = string.Join(" ", lastError.Split(' ').Skip(3));
                }
            });
        }
    }
}
