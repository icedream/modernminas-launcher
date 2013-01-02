using System;
using System.Collections.Generic;
using System.Configuration;
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
    }
}
