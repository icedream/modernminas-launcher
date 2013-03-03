using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Windows;

namespace ModernMinas.Launcher
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Java test
            try
            {
                if (JavaPath.GetJavaBinaryPath() == null)
                {
                    if (MessageBox.Show("You don't have Java 7 installed properly in your system. You need the {0}-bit version from http://java.com/de/download/manual.jsp. When you installed Java properly, try again." + Environment.NewLine + Environment.NewLine
                        + "Would you like to open the Java download page now?", "Java is not properly installed", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                    {
                        var p = new Process();
                        p.StartInfo.FileName = "http://java.com/de/download/manual.jsp";
                        p.Start();
                        return;
                    }
                }
            }
            catch(JavaNotFoundException)
            {
                if (MessageBox.Show(string.Format("You don't have Java 7 installed properly in your system. You need the {0}-bit version from http://java.com/de/download/manual.jsp. When you installed Java properly, try again." + Environment.NewLine + Environment.NewLine
                    + "Would you like to open the Java download page now?", Environment.Is64BitOperatingSystem ? 64 : 32), "Java is not properly installed", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                {
                    var p = new Process();
                    p.StartInfo.FileName = "http://java.com/de/download/manual.jsp";
                    p.Start();
                    return;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString(), "Loading", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (System.IO.File.Exists("version.txt"))
                if (UpdateWindow.CheckLauncherUpdates() != string.Empty)
                {
                    new UpdateWindow().ShowDialog();
                    Environment.Exit(0);
                }

        }

        public static string AppData
        {
            get {
                switch(Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                        return Environment.GetEnvironmentVariable("APPDATA");
                    default:
                        return Environment.GetEnvironmentVariable("HOME");
                }
            }
        }

        public static string GamePath
        {
            get
            {
                return System.IO.Path.Combine(AppData, ".modernminas");
            }
        }

        public static string GameBinPath
        {
            get
            {
                return System.IO.Path.Combine(GamePath, "bin");
            }
        }

        public static string GameLibPath
        {
            get
            {
                return System.IO.Path.Combine(GamePath, "lib");
            }
        }

        public static string StartupLibrarypath
        { get; internal set; }

        public static string StartupClasspath
        { get; internal set; }
    }
}
