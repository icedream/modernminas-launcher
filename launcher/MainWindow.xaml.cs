using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Threading;
using System.Threading.Tasks;

namespace ModernMinas.Launcher
{
    using API;

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string UseSavedPasswordMagic = "\x00\xff\x00\xff\x00\xff\x00\xff";
        const string ConfigFileName = "config.dat";

        MinecraftStatusWindow w = new MinecraftStatusWindow();
        MinecraftLogin l = new MinecraftLogin();

        Configuration config;

        public MainWindow()
        {
            InitializeComponent();

            if (System.IO.File.Exists(ConfigFileName))
                try
                {
                    config = Configuration.LoadFromFile(ConfigFileName);
                }
                catch
                {
                    config = new Configuration();
                }
            if (config == null)
                config = new Configuration();

            w.Name = "StatusWindow";
            w.Opacity = 0;
            w.Background = Brushes.Transparent;

            // Image gallery
            Task.Factory.StartNew(() =>
            {
                var urls =
                    (
                        from url in (new WebClient()).DownloadString("http://www.modernminas.de/img/gallery/launcher/").Split('\n')
                        where !string.IsNullOrEmpty(url) && !string.IsNullOrWhiteSpace(url)
                        select url
                        ).Randomize();
                while ((bool)this.Dispatcher.Invoke(new Func<bool>(() => { return this.IsVisible; })))
                {
                    foreach (string url in urls)
                    {
                        System.Threading.Thread.Sleep(5000);
                        this.Dispatcher.Invoke(new Action(() => { this.ContentPanel.Background = new ImageBrush(new BitmapImage(new Uri(url))); }));
                    }
                }
                
            });

            // Logo animation
            this.Dispatcher.Invoke(new Action(() =>
            {

                Storyboard storyboard = new Storyboard();
                TimeSpan duration = TimeSpan.FromSeconds(8);

                DoubleAnimation animation = new DoubleAnimation();
                animation.Duration = new Duration(duration);

                var easing = new SineEase();
                easing.EasingMode = EasingMode.EaseInOut;
                animation.EasingFunction = easing;

                //Storyboard.SetTarget(animation, (System.Windows.Media.Effects.DropShadowEffect)this.Logo.Effect);
                //Storyboard.SetTargetProperty(animation, new PropertyPath(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty));
                Storyboard.SetTarget(animation, this.Logo);
                Storyboard.SetTargetProperty(animation, new PropertyPath(Control.WidthProperty));
                storyboard.Children.Add(animation);

                storyboard.Completed += (sender, e) =>
                {
                    // Reverse animation range...
                    var a = animation.From;
                    animation.From = animation.To;
                    animation.To = a;
                    // ...and start!
                    storyboard.Begin();
                };

                animation.From = this.Logo.Width - 10;
                animation.To = this.Logo.Width;
                storyboard.Begin();
            }));

            this.Password.Password = config.Password != null && config.Password.Length > 0 ? UseSavedPasswordMagic : string.Empty;
            this.Username.Text = config.Username;
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
                config.Username = Username.Text;
                config.Password = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(System.Runtime.InteropServices.Marshal.SecureStringToBSTR(Password.SecurePassword)).StartsWith("\x00\xff") ? config.Password : Password.SecurePassword;
                var thr = new System.Threading.Thread(
                    new System.Threading.ThreadStart(Login_SepThread)
                );
                thr.SetApartmentState(System.Threading.ApartmentState.STA);
                thr.IsBackground = true;
                thr.Start();
            });
        }
        
        [STAThread]
        private void Login_SepThread()
        {
            try
            {
                Login();
                UpdateMinecraft();
                SaveLoginDetails();
                StartMinecraft();
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                SetError(err.Message
                    + Environment.NewLine
                    + Environment.NewLine
                    + err.StackTrace
                    );
            }
        }
        public void SaveLoginDetails()
        {
            config.SaveToFile(ConfigFileName);
        }
        public void Login()
        {
            SetProgress();
            SetStatus("Logging in...");
            foreach(string apiUrl in new[] { "http://login.modernminas.de/", "http://login.minecraft.net/" })
            {
                l = new MinecraftLogin(new Uri(apiUrl));
                Debug.WriteLine("[Login] API url: {0}", apiUrl, null);
                bool success = l.Login(config.Username, config.Password);
                Debug.WriteLine("[Login] Succeeded: {0}", success, null);
                Debug.WriteLine("[Login] Last error: {0}", l.LastError, null);
                if (success)
                    break;
            }
            if(l.LastError != null)
                if (l.LastError is WebException)
                    throw new Exception(((WebException)l.LastError).Message.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries).Last());
                else
                    throw l.LastError;
            else
                SetStatus("Login succeeded!");
        }

        [STAThread]
        public void StartMinecraft()
        {
            SetProgress();
            SetStatus("Cleaning up...");
            var tmpdir = new System.IO.DirectoryInfo(System.IO.Path.Combine(App.GamePath, "tmp"));
            if (tmpdir.Exists)
                tmpdir.Delete(true);
            tmpdir.Create();

            SetStatus("Starting client...");
            var javaw = JavaPath.CreateJava(new[] {
                "-Xmx" + config.MaximalRam.ToMegabytes() + "M",
                "-Xincgc",
                Environment.Is64BitProcess ? "-d64" : "-d32",
                "-Djava.library.path=lib",
                "-Djava.io.tmpdir=" + System.IO.Path.Combine(App.GamePath, "tmp"),
                "-cp", string.Join(";", from file in (new System.IO.DirectoryInfo(Launcher.App.GameBinPath).GetFiles()) where file.Extension.EndsWith("jar", StringComparison.OrdinalIgnoreCase) select System.IO.Path.Combine("bin", file.Name)),
                "net.minecraft.client.Minecraft",
                config.Username,
                l.SessionId,
                "minas.mc.modernminas.de:25565"
            });
            javaw.StartInfo.WorkingDirectory = App.GamePath;
            if (javaw.StartInfo.EnvironmentVariables.ContainsKey("APPDATA"))
                javaw.StartInfo.EnvironmentVariables["APPDATA"] = App.GamePath;
            else
                javaw.StartInfo.EnvironmentVariables.Add("APPDATA", App.GamePath);
            if (javaw.StartInfo.EnvironmentVariables.ContainsKey("HOME"))
                javaw.StartInfo.EnvironmentVariables["HOME"] = App.GamePath;
            else
                javaw.StartInfo.EnvironmentVariables.Add("HOME", App.GamePath);
            javaw.StartInfo.RedirectStandardError = true;
            javaw.StartInfo.RedirectStandardOutput = true;
            javaw.StartInfo.CreateNoWindow = true;
            Debug.WriteLine("Starting minecraft, arguments: {0}", javaw.StartInfo.Arguments, null);
            javaw.Start();
            SetError(null);
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.Hide();
            }));

#if STATUS_WINDOW
#warning Status window eats a high amount of CPU on a single core. Not recommended for release.
            // Status window
            System.Threading.Tasks.Task.Factory.StartNew(
                () =>
                {
                    this.Dispatcher.Invoke(new Action(w.Show));
                    while (!javaw.HasExited)
                    {
                        RefreshStatusWindowPos();
                        System.Threading.Thread.Sleep(750); // Slow refresh if visible
                    }
                    this.Dispatcher.Invoke(new Action(w.Close));
                });
#endif

            // STDERR
            System.Threading.Tasks.Task.Factory.StartNew(
                () =>
            {

                string lastError = null;
                while (!javaw.HasExited)
                {

                    lastError = javaw.StandardError.ReadLine();
                    if (lastError != null) lastError = lastError.Trim();
                    Debug.WriteLine("[Minecraft] STDERR: {0}", lastError, null);
                    
                    if (lastError == null)
                        continue;
#if STATUS_WINDOW

                    if (lastError.Contains("early MinecraftForge initialization"))
                        this.Dispatcher.Invoke(new Action(() => { w.Show(); w.Fade(1.0, null, 250); }));
                    else if (lastError.Contains("/panorama"))
                        this.Dispatcher.Invoke(new Action(() => w.Fade(0.0, null, 10, (EventHandler)((sender, e) => { w.Hide(); }))));
#endif

                    lastError = string.Join(" ", lastError.Split(' ').Skip(3));

#if STATUS_WINDOW
                    // loading text
                    Match m;
                    if ((m = Regex.Match(lastError, "setupTexture: \"(.+)\"")).Success)
                        lastError = "Loading texture: " + m.Groups[1].Value;
                        //lastError = "Loading textures...";
                    else if (lastError.Contains("[STDOUT] Checking for new version"))
                        lastError = "Checking for Optifine updates...";
                    else if (lastError.Contains("Connecting"))
                        lastError = "Connecting to the server...";
                    else if ((m = Regex.Match(lastError, "TextureFX registered: (.+)")).Success)
                        lastError = "Loading texture effects...";
                    else if ((m = Regex.Match(lastError, "TextureFX removed: (.+)")).Success)
                        lastError = "Unloading texture effects...";
                    else if ((m = Regex.Match(lastError, "Loading custom colors: (.+)")).Success)
                        lastError = "Loading custom colors...";
                    else if ((m = Regex.Match(lastError, "\\[(.+)\\] (Initializing|Starting|Loading|Attempting|Config) ([^\\s]+) .*")).Success)
                        switch (m.Groups[1].Value)
                        {
                            case "STDOUT":
                                if (m.Groups[3].Value.Length < 3)
                                    continue;
                                lastError = "Loading " + m.Groups[3].Value + "...";
                                break;
                            default:
                                lastError = "Loading " + m.Groups[1].Value.Replace("ForgeModLoader", "Forge") + "...";
                                break;
                        }
                    else if ((m = Regex.Match(lastError, "\\[ForgeModLoader\\] Searching .*")).Success)
                        lastError = "Loading modifications...";
                    else if ((m = Regex.Match(lastError, "\\(Audiotori\\) .+")).Success)
                        lastError = "Loading audio system...";
                    else if ((m = Regex.Match(lastError, "\\(ATSystem\\) Performing .+")).Success)
                        lastError = "Loading sounds...";
                    else
                        continue;
#endif

                    this.Dispatcher.Invoke(new Action(() => w.SetStatus(lastError)));
                }
                Debug.WriteLine("[Minecraft] End of error stream");

                // Error handling
                if (javaw.ExitCode != 0 /* No error at all */
                    && javaw.ExitCode != -1 /* Closed via clicking "X" */)
                    this.Dispatcher.Invoke(new Action(() => SetError(string.Format("Minecraft error code {0}. {1}", javaw.ExitCode, lastError))));
                this.Dispatcher.Invoke(new Action(() => { this.Close(); Environment.Exit(0); }));
            }, System.Threading.Tasks.TaskCreationOptions.AttachedToParent);

            // STDOUT
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                    while(!javaw.HasExited)
                    {
                        var lastError = javaw.StandardOutput.ReadLine();
                        if (lastError != null) lastError = lastError.Trim();
                        Debug.WriteLine("[Minecraft] STDOUT: {0}", lastError, null);

                        if (lastError == null)
                            continue;
                        
#if STATUS_WINDOW
                        if (lastError.Contains("early MinecraftForge initialization"))
                            this.Dispatcher.Invoke(new Action(() => { w.Show(); w.Fade(1.0, null, 250); }));
                        else if (lastError.Contains("/panorama"))
                            this.Dispatcher.Invoke(new Action(() => w.Fade(0.0, null, 10, (EventHandler)((sender, e) => { w.Hide(); }))));
#endif

                        lastError = string.Join(" ", lastError.Split(' ').Skip(3));
                        
#if STATUS_WINDOW
                        // loading text
                        Match m;
                        if ((m = Regex.Match(lastError, "setupTexture: \"(.+)\"")).Success)
                            lastError = "Loading texture: " + m.Groups[1].Value;
                            //lastError = "Loading textures...";
                        else if ((m = Regex.Match(lastError, "TextureFX registered: (.+)")).Success)
                            lastError = "Loading texture effects...";
                        else if ((m = Regex.Match(lastError, "TextureFX removed: (.+)")).Success)
                            lastError = "Unloading texture effects...";
                        else if ((m = Regex.Match(lastError, "Loading custom colors: (.+)")).Success)
                            lastError = "Loading custom colors...";
                        else if ((m = Regex.Match(lastError, "\\[(.+)\\] (Initializing|Starting)")).Success)
                            lastError = "Loading " + m.Groups[1].Value + "...";
                        else if ((m = Regex.Match(lastError, "\\(Audiotori\\) .+")).Success)
                            lastError = "Loading audio system...";
                        else if ((m = Regex.Match(lastError, "\\(ATSystem\\) Performing .+")).Success)
                            lastError = "Loading sounds...";
                        else
                            continue;
#endif

                        this.Dispatcher.Invoke(new Action(() => w.SetStatus(lastError)));
                    }
            });
        }

#if STATUS_WINDOW
        private void RefreshStatusWindowPos()
        {
            IntPtr ptr = new IntPtr(FindWindow(null, "Minecraft"));
#if !NO_STATUS_INTPTR_ZERO_TEST
            if (ptr == IntPtr.Zero)
                return;
#endif
            Rect pos = new Rect();
            GetWindowRect(ptr, ref pos);
            SetStatusWindow(pos);
        }

        private void SetStatusWindow(Rect pos)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                //pos.Left -= (int)SystemParameters.VirtualScreenLeft;
                //pos.Top -= (int)SystemParameters.VirtualScreenTop;
#if !NO_STATUS_WINDOW_TEST
                if (pos.Right - pos.Left < 1)
                    return;
                if (pos.Bottom - pos.Top < 1)
                    return;
#endif
#if NO_STATUS_IF_SCREEN_OVERSIZED
                if (pos.Right - pos.Left > SystemParameters.VirtualScreenWidth)
                    return;
                if (pos.Bottom - pos.Top > SystemParameters.VirtualScreenHeight)
                    return;
#endif
                var size = new Size(pos.Right - pos.Left, pos.Bottom - pos.Top);
#if STATUS_WINDOW_DEBUG
                Debug.Write(" with L");
                Debug.Write(pos.Left);
                Debug.Write(" x T");
                Debug.Write(pos.Top);
                Debug.Write(" x R");
                Debug.Write(pos.Right);
                Debug.Write(" x B");
                Debug.Write(pos.Bottom);
                Debug.Write(" (Size is W");
                Debug.Write(size.Width);
                Debug.Write(" x H");
                Debug.Write(size.Height);
                Debug.Write(")");
#endif
                w.Left = pos.Left;
                w.Width = size.Width;
                w.Top = pos.Top + (size.Height / 1.5);
            }));
        }
#endif

        public void UpdateMinecraft()
        {
            // Connect to update server
            SetProgress();
            SetStatus("Connecting to update server...");
            TcpClient tcp = new TcpClient("update.modernminas.de", 25555);
            var ns = tcp.GetStream();
            var updater = new Connection(ns);
            updater.SendProtocolVersion(); // Check if protocol version fits

            SetStatus("Checking for updates...");
            var repository = updater.RequestFileList();
            List<FileInfo> filesToUpdate = new List<FileInfo>();
            List<System.IO.FileInfo> filesToDelete = new List<System.IO.FileInfo>();
            var baseDir = new System.IO.DirectoryInfo(App.GamePath);
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
                Debug.WriteLine("Download: {0}", fi.FullName, null);
                fi.Directory.Create();
                var fis = fi.Create();
                var status = updater.RequestFileAsync(f, fis);
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
                fis.Flush();
                fis.Close();
                fis.Dispose();
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
                    CheckUpdateFile(f, new System.IO.FileInfo(f.GetAbsolutePath(App.GamePath)), ref filesToUpdate);
            if(remote.Files.Select(f => f.Name).Contains(".mm-sys.delete"))
                foreach (var f in
                        from file in local.GetFiles()
                        where !remote.Files.Select(remoteFile => remoteFile.Name.ToLower()).Contains(file.Name.ToLower())
                        select file
                    )
                {
                    Debug.WriteLine("Needs deletion: {0}", local.FullName, null);
                    filesToDelete.Add(f);
                }
            foreach (var d in remote.Directories)
                CheckUpdateDir(d, local.CreateSubdirectory(d.Name), ref filesToUpdate, ref filesToDelete);
        }

        void CheckUpdateFile(FileInfo remote, System.IO.FileInfo local, ref List<FileInfo> filesToUpdate)
        {
            if (!local.Exists || !local.Length.Equals(remote.Length) || local.LastWriteTimeUtc < remote.LastWriteTimeUtc)
            {
                Debug.WriteLine(null);
                //Debug.WriteLine("Local file: {0}, {1} bytes, {2}", local.Name, local.Length, local.LastWriteTimeUtc);
                Debug.WriteLine("Remote file: {0}, {1} bytes, {2}", remote.Name, remote.Length, remote.LastWriteTimeUtc);
                filesToUpdate.Add(remote);
                Debug.WriteLine("=> Needs update");
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
            dlg.MaximumRam = config.MaximalRam;
            dlg.Hide();
            dlg.ShowDialog();
            if(dlg.ShouldApply)
                config.MaximalRam = dlg.MaximumRam;
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hwnd, string lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private void Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            w.Close(); // Fix for main thread not quitting
        }
    }
}
