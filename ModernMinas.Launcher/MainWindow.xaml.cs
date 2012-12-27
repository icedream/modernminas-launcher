using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security;

namespace ModernMinas.Launcher
{
    using API;

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MinecraftLogin l = new MinecraftLogin();

        FileSize MaximalRam = FileSize.FromGigabytes(1);

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string text)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                this.LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ProgressText.Content = text;
            }));
        }

        public void SetError(string text)
        {
            if (!string.IsNullOrEmpty(text))
                MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Dispatcher.Invoke(new Action(delegate()
            {
                this.LoginError.Content = text;
                if (!string.IsNullOrEmpty(text))
                    Fade(this.ProgressPanel, 0, null, 250.0, (a, b) =>
                    {
                        this.ProgressPanel.Visibility = System.Windows.Visibility.Collapsed;
                        this.LoginPanel.Visibility = System.Windows.Visibility.Visible;
                        this.LoginPanel.Opacity = 0;
                        this.LoginPanel.Height = this.BottomContentPanel.Height = !string.IsNullOrEmpty(text) ? 84 : 60;
                        this.LoginError.Visibility = !string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
                        Fade(this.LoginPanel, 1);
                    });
                else
                    Fade(this.LoginError, 0, null, 250.0);
            }));
        }

        public void SetProgress(int val = -1, int max = int.MinValue, int min = int.MinValue)
        {
            this.ProgressBar.Dispatcher.Invoke(new Action(delegate()
            {
                this.ProgressBar.Visibility = System.Windows.Visibility.Visible;
                if (min > int.MinValue) this.ProgressBar.Minimum = min;
                if (max > int.MinValue) this.ProgressBar.Maximum = max;
                if (val >= 0)
                {
                    this.ProgressBar.Value = val;
                    this.ProgressBar.IsIndeterminate = false;
                }
                else
                {
                    this.ProgressBar.Value = 100;
                    this.ProgressBar.IsIndeterminate = true;
                }
            }));
        }

        public void ChangeProgress(int byValue = 1)
        {
            this.ProgressBar.Dispatcher.Invoke(new Action(delegate()
            {
                this.ProgressBar.Value += byValue;
            }));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.BottomContentPanel.Height = 60;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Username.Text) || Username.Text.Contains(' '))
            {
                SetError("You need to enter a valid username");
                return;
            }
            if (Password.SecurePassword.Length == 0)
            {
                SetError("You need to enter a password.");
                return;
            }
            Fade(LoginPanel, 0, null, 250, (a, b) => {
                LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
                ProgressPanel.Visibility = System.Windows.Visibility.Visible;
                ProgressPanel.Opacity = 0;
                Fade(ProgressPanel, 1, null, 250); //, (c, d) => {
                System.Threading.Tasks.Task.Factory.StartNew(
                    new Action<object>(Login_SepThread),
                    new object[] { this.Username.Text, this.Password.SecurePassword }
                );
            });
        }
        
        private void Login_SepThread(object o)
        {
            object[] s = (object[])o;
            Login_SepThread(s[0].ToString(), (SecureString)s[1]);
        }
        private void Login_SepThread(string f, SecureString g)
        {
            try
            {
                Login(f, g);
                UpdateMinecraft();
                StartMinecraft(f);
            }
            catch (Exception err)
            {
                SetError(err.Message);
            }
        }

        public void Login(string username, SecureString password)
        {
            SetProgress();
            SetStatus("Logging in...");
            if(!l.Login(username, password))
            {
                if(l.LastError is WebException)
                {
                    throw new Exception(((WebException)l.LastError).Message.Split(new[]{": "}, StringSplitOptions.RemoveEmptyEntries).Last());
                }
                else
                {
                    throw l.LastError;
                }
            }
            SetStatus("Login succeeded!");
        }

        public void StartMinecraft(string username)
        {
            SetProgress();
            SetStatus("Cleaning up...");
            var tmpdir = new System.IO.DirectoryInfo(System.IO.Path.Combine("data", "tmp"));
            if (tmpdir.Exists)
                tmpdir.Delete(true);
            tmpdir.Create();

            SetStatus("Starting client...");
            var javaw = JavaPath.CreateJava(new[] {
                "-Xmx" + MaximalRam.ToMegabytes() + "M",
                "-Xincgc",
                Environment.Is64BitProcess ? "-d64" : "-d32",
                "-Djava.library.path=data/lib",
                //"-Djava.io.tmpdir=" + System.IO.Path.Combine(Environment.CurrentDirectory, "data", "tmp"),
                //"-Duser.home=" + System.IO.Path.Combine(Environment.CurrentDirectory, "data"),
                //"-Duser.dir=" + System.IO.Path.Combine(Environment.CurrentDirectory, "data"),
                "-cp", "data/bin/minecraft.jar;data/bin/lwjgl.jar;data/bin/lwjgl_util.jar;data/bin/jinput.jar",
                "net.minecraft.client.Minecraft",
                username,
                l.SessionId,
                "minas.mc.modernminas.tk:25565"
            });
            if (javaw.StartInfo.EnvironmentVariables.ContainsKey("APPDATA"))
                javaw.StartInfo.EnvironmentVariables["APPDATA"] = System.IO.Path.Combine(Environment.CurrentDirectory, "data");
            else
                javaw.StartInfo.EnvironmentVariables.Add("APPDATA", System.IO.Path.Combine(Environment.CurrentDirectory, "data"));
            if (javaw.StartInfo.EnvironmentVariables.ContainsKey("HOME"))
                javaw.StartInfo.EnvironmentVariables["HOME"] = System.IO.Path.Combine(Environment.CurrentDirectory, "data");
            else
                javaw.StartInfo.EnvironmentVariables.Add("HOME", System.IO.Path.Combine(Environment.CurrentDirectory, "data"));
#if DEBUG
            javaw.StartInfo.RedirectStandardError = true;
            javaw.StartInfo.RedirectStandardOutput = true;
#endif
            Console.WriteLine("Starting minecraft, arguments: {0}", javaw.StartInfo.Arguments);
            javaw.Start();
            SetError(null);
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.Hide();
            }));

#if DEBUG
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                string lastError = null;
                while (!javaw.HasExited)
                {
                    lastError = javaw.StandardError.ReadLine();
                    if (lastError != null) lastError = lastError.Trim();
                    Console.WriteLine("[Minecraft] STDERR: {0}", lastError);
                }
                Console.WriteLine("[Minecraft] End of error stream");
                if(javaw.ExitCode != 0)
                    this.Dispatcher.Invoke(new Action(() => SetError(string.Format("Minecraft error code {0}. {1}", javaw.ExitCode, lastError))));
            });
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    while (!javaw.HasExited && javaw.StandardOutput != null)
                    {
                        Console.WriteLine("[Minecraft] STDOUT: {0}", javaw.StandardOutput.ReadLine());
                    }
                }
                catch
                {
                    { }
                }
                System.Threading.Thread.Sleep(1000);
                this.Dispatcher.Invoke(new Action(() => this.Close()));
            });
#else
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.Dispatcher.Invoke(new Action(() => this.Hide()));
                javaw.WaitForExit();
                this.Dispatcher.Invoke(new Action(() => this.Close()));
            }));
#endif
        }

        public void UpdateMinecraft()
        {
            // Connect to update server
            SetProgress();
            SetStatus("Connecting to update server...");
            TcpClient tcp = new TcpClient("minas.mc.modernminas.tk", 25555);
            var ns = tcp.GetStream();
            var updater = new Connection(ns);
            updater.SendProtocolVersion(); // Check if protocol version fits

            SetStatus("Checking for updates...");
            var repository = updater.RequestFileList();
            List<FileInfo> filesToUpdate = new List<FileInfo>();
            List<System.IO.FileInfo> filesToDelete = new List<System.IO.FileInfo>();
            var baseDir = new System.IO.DirectoryInfo("data");
            baseDir.Create();
            CheckUpdateDir(repository, baseDir, ref filesToUpdate, ref filesToDelete);

            SetStatus("Downloading...");
            var totalUpdateSize = filesToUpdate.Select(u => u.Length).Sum();
            SetProgress(0, (int)(totalUpdateSize / 1024));
            long ou = 0; // finished updates size
            foreach (var f in filesToUpdate)
            {
                SetProgress((int)(ou / 1024));
                System.IO.FileInfo fi = new System.IO.FileInfo(System.IO.Path.Combine(f.Directory.GetAbsolutePath(baseDir.FullName), f.Name));
                Console.WriteLine("Download: {0}", fi.FullName);
                fi.Directory.Create();
                var status = updater.RequestFileAsync(f, fi.Create());
                while (status.Status != RequestFileStatus.Finished)
                {
                    System.Threading.Thread.Sleep(100);
                    switch (status.Status)
                    {
                        case RequestFileStatus.DownloadingFile:
                            switch (status.DownloadStatus.Status)
                            {
                                case ReadFileStatus.Downloading:
                                    SetProgress((int)((ou + status.DownloadStatus.BytesRead) / 1024));
                                    SetStatus("Downloading: " + GetSizeString(ou + status.DownloadStatus.BytesRead) + "/" + GetSizeString(totalUpdateSize));
                                    break;
                                default:
                                    if (status.DownloadStatus != null)
                                        SetStatus("Downloading: " + GetSizeString(ou + status.DownloadStatus.BytesRead) + "/" + GetSizeString(totalUpdateSize));
                                    break;
                            }
                            break;
                        default:
                            if (status.DownloadStatus != null)
                                SetStatus("Downloading: " + GetSizeString(ou + status.DownloadStatus.BytesRead) + "/" + GetSizeString(totalUpdateSize));
                            break;
                    }
                }
                ou += f.Length;
            }
            updater.Disconnect();

            SetStatus("Deleting files...");
            foreach (var f in filesToDelete)
                f.Delete();

            SetStatus("Update finished");
            SetProgress(1, 1);
        }

        string GetSizeString(double size)
        {
            string[] suffixes = new[] {
                "B", "kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"
            };
            int i = 0;
            while(size > 1024)
            {
                i++;
                size /= 1024;
            }
            return string.Format("{0:N1} {1}", size, suffixes[Math.Min(suffixes.Length, i)]);
        }

        void CheckUpdateDir(DirectoryInfo remote, System.IO.DirectoryInfo local, ref List<FileInfo> filesToUpdate, ref List<System.IO.FileInfo> filesToDelete)
        {
            foreach (var f in remote.Files)
                if(!f.Name.StartsWith(".mm-sys"))
                    CheckUpdateFile(f, new System.IO.FileInfo(f.GetAbsolutePath("data")), ref filesToUpdate);
            if(remote.Files.Select(f => f.Name).Contains(".mm-sys.delete"))
                foreach (var f in
                        from file in local.GetFiles()
                        where !remote.Files.Select(remoteFile => remoteFile.Name.ToLower()).Contains(file.Name.ToLower())
                        select file
                    )
                {
                    Console.WriteLine("Needs deletion: {0}", local.FullName);
                    filesToDelete.Add(f);
                }
            foreach (var d in remote.Directories)
                CheckUpdateDir(d, local.CreateSubdirectory(d.Name), ref filesToUpdate, ref filesToDelete);
        }

        void CheckUpdateFile(FileInfo remote, System.IO.FileInfo local, ref List<FileInfo> filesToUpdate)
        {
            if (!local.Exists || !local.Length.Equals(remote.Length) || local.LastWriteTimeUtc < remote.LastWriteTimeUtc)
            {
                Console.WriteLine();
                //Console.WriteLine("Local file: {0}, {1} bytes, {2}", local.Name, local.Length, local.LastWriteTimeUtc);
                Console.WriteLine("Remote file: {0}, {1} bytes, {2}", remote.Name, remote.Length, remote.LastWriteTimeUtc);
                filesToUpdate.Add(remote);
                Console.WriteLine("=> Needs update");
            }
        }

        public void Fade(FrameworkElement c, double targetOpacity, EasingFunctionBase f = null, double ms = 500.0, EventHandler onFinish = null)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                Storyboard storyboard = new Storyboard();
                TimeSpan duration = TimeSpan.FromMilliseconds(ms);

                DoubleAnimation animation = new DoubleAnimation();
                animation.From = c.Opacity;
                animation.To = targetOpacity;
                animation.Duration = new Duration(duration);

                if ((animation.EasingFunction = f) == null)
                {
                    var easing = new SineEase();
                    easing.EasingMode = EasingMode.EaseOut;
                    animation.EasingFunction = easing;
                }

                Storyboard.SetTargetName(animation, c.Name);
                Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));

                storyboard.Children.Add(animation);

                if (onFinish != null)
                    storyboard.Completed += onFinish;

                storyboard.Begin(this);
            }));
        }

        private void Main_Initialized(object sender, EventArgs e)
        {
        }

        private void Main_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void Main_ContentRendered(object sender, EventArgs e)
        {
            LoginPanel.Visibility = System.Windows.Visibility.Visible;
            LoginPanel.Opacity = 0;
            Fade(LoginPanel, 1, null, 1000.0);
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsDialog dlg = new OptionsDialog();
            dlg.MaximumRam = MaximalRam;
            dlg.Hide();
            dlg.ShowDialog();
            if(dlg.ShouldApply)
            {
                MaximalRam = dlg.MaximumRam;
            }
        }
    }
}
