using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using ModernMinas.Launcher.API;

namespace TestUpdateClient
{
    class Program
    {
        static Socket s;
        static Connection c;
        static DirectoryInfo fl;
        static string prefix = @".\files";
        static string server = "minas.mc.modernminas.tk";
        static int port = 25555;

        static void DownloadFiles(DirectoryInfo di)
        {
            Console.WriteLine("Directory: {0}", di.Name, di.GetAbsolutePath(prefix));

            foreach (var d in di.Directories)
            {
                var dio = d.GetIODirectoryInfo(prefix);
                if (!dio.Exists)
                    dio.Create();
                DownloadFiles(d);
            }

            foreach (var f in di.Files)
            {
                var fio = f.GetIOFileInfo(prefix);
                if (!fio.Exists || fio.Length != f.Length || fio.LastWriteTimeUtc < f.LastWriteTimeUtc)
                {
                    Console.WriteLine("Downloading: {0}", f.Name, f.GetAbsolutePath(prefix));
                    var fs = System.IO.File.OpenWrite(f.GetAbsolutePath(prefix));
                    c.RequestFile(f, fs);
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Connecting...");

                // Initialize socket and connect
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(server, port);

                // Protocol version test (required)
                c = new Connection(s);
                c.SendProtocolVersion();

                // Get file list
                fl = c.RequestFileList();
                DownloadFiles(fl);

                // Disconnect
                c.Disconnect();
            }
            catch (Exception n)
            {
                Console.Error.WriteLine("Error: {0}", n.ToString());
            }

            Console.WriteLine("Press a key...");
            Console.ReadKey(true);
        }
    }
}
