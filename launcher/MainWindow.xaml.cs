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
using ModernMinas.Update.Api;

namespace ModernMinas.Launcher
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string UseSavedPasswordMagic = "\x00\xff\x00\xff\x00\xff\x00\xff";
        string ConfigFileName = "config.dat";

        MinecraftStatusWindow w = new MinecraftStatusWindow();
        MinecraftLogin l = new MinecraftLogin();

        Configuration config;

        public MainWindow()
        {
            InitializeComponent();

            System.IO.Directory.CreateDirectory(App.GamePath);
            ConfigFileName = System.IO.Path.Combine(App.GamePath, ConfigFileName);

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

            // Show app version in titlebar
            if (System.IO.File.Exists("version.txt"))
            {
                this.Title += " v";
                this.Title += System.IO.File.ReadAllText("version.txt").Replace("_", ".");
            }

            // Image gallery
            Task.Factory.StartNew(() =>
            {
                var urls =
                    (
                        from url in (new WebClient()).DownloadString("http://www.modernminas.de/img/gallery/launcher/").Split('\n')
                        where !string.IsNullOrEmpty(url) //&& !string.IsNullOrWhiteSpace(url)
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

#if LOGO_ANIMATION
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
#endif

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
                    Fade(this.ProgressPanel, 0, 250.0, (a, b) =>
                    {
                        this.ProgressPanel.Visibility = System.Windows.Visibility.Collapsed;
                        this.LoginPanel.Visibility = System.Windows.Visibility.Visible;
                        this.LoginPanel.Opacity = 0;
                        this.LoginPanel.Height = this.BottomContentPanel.Height = !string.IsNullOrEmpty(text) ? 84 : 60;
                        this.LoginError.Visibility = !string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
                        Fade(this.LoginPanel, 1);
                    });
                else
                    Fade(this.LoginError, 0, 250.0);
            }));
        }

        public void SetProgress(double val = -1, double max = double.MinValue, double min = double.MinValue)
        {
            this.ProgressBar.Dispatcher.Invoke(new Action(delegate()
            {
                this.ProgressBar.Visibility = System.Windows.Visibility.Visible;
                if (min > double.MinValue) this.ProgressBar.Minimum = min;
                if (max > double.MinValue) this.ProgressBar.Maximum = max;
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
            Fade(LoginPanel, 0, 250, (a, b) => {
                LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
                ProgressPanel.Visibility = System.Windows.Visibility.Visible;
                ProgressPanel.Opacity = 0;
                Fade(ProgressPanel, 1, 250); //, (c, d) => {
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
                Debug.WriteLine(string.Format("[Login] API url: {0}", apiUrl));
                bool success = l.Login(config.Username, config.Password);
                Debug.WriteLine(string.Format("[Login] Succeeded: {0}", success));
                Debug.WriteLine(string.Format("[Login] Last error: {0}", l.LastError));
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
                /*Environment.Is64BitProcess*/ IntPtr.Size == 8 ? "-d64" : "-d32",
                "-Djava.library.path=" + App.StartupLibrarypath,
                "-Djava.io.tmpdir=" + System.IO.Path.Combine(App.GamePath, "tmp"),
                "-cp", App.StartupClasspath,
                "net.minecraft.client.Minecraft",
                l.CaseCorrectUsername,
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
            Debug.WriteLine(string.Format("Starting minecraft, arguments: {0}", javaw.StartInfo.Arguments));
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
                    Debug.WriteLine(string.Format("[Minecraft] STDERR: {0}", lastError));
                    
                    if (lastError == null)
                        continue;
#if STATUS_WINDOW

                    if (lastError.Contains("early MinecraftForge initialization"))
                        this.Dispatcher.Invoke(new Action(() => { w.Show(); w.Fade(1.0, null, 250); }));
                    else if (lastError.Contains("/panorama"))
                        this.Dispatcher.Invoke(new Action(() => w.Fade(0.0, null, 10, (EventHandler)((sender, e) => { w.Hide(); }))));
#endif

                    lastError = string.Join(" ", lastError.Split(' ').Skip(3).ToArray());

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
                    && javaw.ExitCode != -1 && javaw.ExitCode != 1 /* Closed via clicking "X" */)
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
                        Debug.WriteLine(string.Format("[Minecraft] STDOUT: {0}", lastError));

                        if (lastError == null)
                            continue;
                        
#if STATUS_WINDOW
                        if (lastError.Contains("early MinecraftForge initialization"))
                            this.Dispatcher.Invoke(new Action(() => { w.Show(); w.Fade(1.0, null, 250); }));
                        else if (lastError.Contains("/panorama"))
                            this.Dispatcher.Invoke(new Action(() => w.Fade(0.0, null, 10, (EventHandler)((sender, e) => { w.Hide(); }))));
#endif

                        lastError = string.Join(" ", lastError.Split(' ').Skip(3).ToArray());
                        
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
            SetProgress();
            SetStatus("Fetching packages...");

            var uri = new Uri("http://repo.update.modernminas.de/");

            CacheFile cache = new CacheFile(System.IO.Path.Combine(App.GamePath, "installation.cache"));
            Repository repository = new Repository(uri, cache) { TargetDirectory = new System.IO.DirectoryInfo(App.GamePath) };
            Setup setup = new Setup(uri, repository);

            Dictionary<string, float> progress = new Dictionary<string,float>();
            foreach (var package in setup.Packages)
            {
                progress.Add(package.ID + "_preinstall", 0);
                progress.Add(package.ID + "_download", 0);
                progress.Add(package.ID + "_install", 0);
                progress.Add(package.ID + "_uninstall", 0);
            }

            int i = 0;
            repository.StatusChanged += (sender, e) =>
            {
                switch (e.Status)
                {
                    case StatusType.Finished:
                        if (!progress.ContainsKey(e.Package.ID + "_install"))
                            break;
                        if (progress[e.Package.ID + "_uninstall"] < 1)
                            progress[e.Package.ID + "_uninstall"] = 1;
                        if (progress[e.Package.ID + "_preinstall"] < 1)
                            progress[e.Package.ID + "_preinstall"] = 1;
                        if (progress[e.Package.ID + "_download"] < 1)
                            progress[e.Package.ID + "_download"] = 1;
                        if(progress[e.Package.ID + "_install"] < 1)
                            progress[e.Package.ID + "_install"] = 1;
                        break;
                    case StatusType.Parsing:
                    case StatusType.Installing:
                        if (!progress.ContainsKey(e.Package.ID + "_install"))
                            break;
                        progress[e.Package.ID + "_install"] = e.Progress;
                        if(progress[e.Package.ID + "_uninstall"] < 1)
                            progress[e.Package.ID + "_uninstall"] = 1;
                        if(progress[e.Package.ID + "_preinstall"] < 1)
                            progress[e.Package.ID + "_preinstall"] = 1;
                        if(progress[e.Package.ID + "_download"] < 1)
                            progress[e.Package.ID + "_download"] = 1;
                        break;
                    case StatusType.Downloading:
                        if (progress.ContainsKey(e.Package.ID + "_download"))
                        {
                            if (progress[e.Package.ID + "_preinstall"] < 1)
                                progress[e.Package.ID + "_preinstall"] = 1;
                            progress[e.Package.ID + "_download"] = e.Progress;
                        }
                        break;
                    case StatusType.CheckingDependencies:
                    case StatusType.InstallingDependencies:
                        if (progress.ContainsKey(e.Package.ID + "_preinstall"))
                            progress[e.Package.ID + "_preinstall"] = e.Progress;
                        break;
                    case StatusType.Uninstalling:
                        SetStatus(string.Format("Package {0}/{1}: Updating...", i, setup.Packages.Count()));
                        if (progress.ContainsKey(e.Package.ID + "_uninstall"))
                            progress[e.Package.ID + "_uninstall"] = e.Progress;
                        break;
                }
                SetProgress(progress.Sum(v => v.Value));
                foreach (var j in progress.Keys)
                {
                    Debug.WriteLine(string.Format("{0}: {1}", j, progress[j]));
                }
            };
            SetProgress(0, setup.Packages.Count() * 4);
            SetStatus("Updating...");
            foreach (var package in setup.Packages)
            {
                i++;
                Debug.WriteLine(string.Format("Current package: {0}", package.ID, null));

                SetProgress(progress.Sum(v => v.Value));

                if (package.IsInstalled)
                {
                    SetStatus(string.Format("Package {0}/{1}: Checking for updates...", i, setup.Packages.Count()));
                    package.Update();
                }
                else
                {
                    SetStatus(string.Format("Package {0}/{1}: Installing...", i, setup.Packages.Count()));
                    package.Install();
                }
            }
            foreach (var package in from p in setup.Packages where !cache.GetAllCachedPackageIDs().Contains(p.ID) select p)
            {
                repository.UninstallPackage(package.ID);
            }

            SetStatus("Update finished");
            SetProgress(1, 1);

            App.StartupClasspath = setup.GetStartupClasspath();
            App.StartupLibrarypath = setup.GetStartupLibrarypath();
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

        public void Fade(FrameworkElement c, double targetOpacity, /*EasingFunctionBase f = null,*/ double ms = 500.0, EventHandler onFinish = null)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                Storyboard storyboard = new Storyboard();
                TimeSpan duration = TimeSpan.FromMilliseconds(ms);

                DoubleAnimation animation = new DoubleAnimation();
                animation.From = c.Opacity;
                animation.To = targetOpacity;
                animation.Duration = new Duration(duration);
                /*
                if ((animation.EasingFunction = f) == null)
                {
                    var easing = new SineEase();
                    easing.EasingMode = EasingMode.EaseOut;
                    animation.EasingFunction = easing;
                }
                 */

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
            Fade(LoginPanel, 1, 1000.0);
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
