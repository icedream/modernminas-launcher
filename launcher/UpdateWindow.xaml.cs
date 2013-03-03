using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace ModernMinas.Launcher
{
    /// <summary>
    /// Interaktionslogik für UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        WebClient wc = new WebClient();

        public UpdateWindow()
        {
            InitializeComponent();
        }

        public void StartLauncherUpdates()
        {
            Process p = new Process();
            p.StartInfo.FileName = Path.Combine(Path.GetTempPath(), "modernminas_update.exe");
            p.StartInfo.Arguments = "/S /D=" + Environment.CurrentDirectory;
            p.Start();
        }

        public void DownloadLauncherUpdates()
        {
            string url = "http://update.modernminas.de/bootstrap/setup";
            wc.DownloadFile(url, Path.Combine(Path.GetTempPath(), "modernminas_update.exe"));

        }

        public string CheckLauncherUpdates()
        {
            string url = "http://update.modernminas.de/bootstrap/version";
            string newVersion = wc.DownloadString(url);
            string oldVersion = File.ReadAllText("version.txt").Trim();
            if (newVersion.Equals(oldVersion))
            {
                return string.Empty;
            }
            else
                return newVersion;
        }

        private void UpdateWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            if (CheckLauncherUpdates() == string.Empty)
            {
                this.Hide();
            }
            else
                Task.Factory.StartNew(() =>
                {
                    DownloadLauncherUpdates();
                    StartLauncherUpdates();
                    Environment.Exit(0);
                });
        }
    }
}
