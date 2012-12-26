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

namespace ModernMinas.Launcher
{
    using API;

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
            this.Dispatcher.Invoke(new Action(delegate()
            {
                this.LoginError.Content = text;
                Fade(this.ProgressPanel, 0, null, 250.0, (a, b) =>
                {
                    //MessageBox.Show(text);
                    this.ProgressPanel.Visibility = System.Windows.Visibility.Collapsed;
                    this.LoginPanel.Visibility = System.Windows.Visibility.Visible;
                    this.LoginPanel.Opacity = 0;
                    this.LoginPanel.Height = this.BottomContentPanel.Height = !string.IsNullOrEmpty(text) ? 84 : 60;
                    this.LoginError.Visibility = !string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
                    Fade(this.LoginPanel, 1);
                });
            }));
        }

        public void SetProgress(int val = -1, int max = 100, int min = 0)
        {
            this.ProgressBar.Dispatcher.Invoke(new Action(delegate()
            {
                if (val >= 0)
                {
                    //Fade(this.ProgressBar, 1, null, 250);
                    this.ProgressBar.Minimum = min;
                    this.ProgressBar.Maximum = max;
                    this.ProgressBar.Value = val;
                    this.ProgressBar.IsIndeterminate = false;
                }
                else
                {
                    //Fade(this.ProgressBar, 0, null, 250);
                    this.ProgressBar.Value = 100;
                    this.ProgressBar.IsIndeterminate = true;
                }
            }));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.BottomContentPanel.Height = 60;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Fade(LoginPanel, 0, null, 250, (a, b) => {
                this.ProgressBar.Opacity = 0;
                LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
                ProgressPanel.Visibility = System.Windows.Visibility.Visible;
                ProgressPanel.Opacity = 0;
                Fade(ProgressPanel, 1, null, 250); //, (c, d) => {
                System.Threading.Tasks.Task.Factory.StartNew(() => UpdateMinecraft());
                //});
            });
        }

        public void UpdateMinecraft()
        {
            try
            {
                // TODO: Minecraft login
                // TODO: Minecraft jar selection/Seperate components selection

                // Connect to update server
                SetProgress();
                SetStatus("Connecting to update server...");
                TcpClient tcp = new TcpClient("minas.mc.modernminas.tk", 25555);
                var ns = tcp.GetStream();
                var updater = new Connection(ns);
                updater.SendProtocolVersion();
                SetStatus("Checking for updates...");
                var repository = updater.RequestFileList();
                updater.Disconnect();
                List<FileInfo> filesToUpdate = new List<FileInfo>();
                CheckUpdateDir(repository, new System.IO.DirectoryInfo("data"), ref filesToUpdate);
                SetError(string.Format("Found {0} updates.", filesToUpdate.Count));
                return;
                // TODO: Minecraft launch
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not finish update.\r\n" +
#if DEBUG
 e.ToString()
#else
 e.Message
#endif
, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetError(e.Message);
                return;
            }
        }

        void CheckUpdateDir(DirectoryInfo remote, System.IO.DirectoryInfo local, ref List<FileInfo> filesToUpdate)
        {
            foreach(var f in remote.Files)
                CheckUpdateFile(f, new System.IO.FileInfo(System.IO.Path.Combine(remote.GetRelativePath(), f.Name)), ref filesToUpdate);
            foreach (var d in remote.Directories)
                CheckUpdateDir(d, local.CreateSubdirectory(d.Name), ref filesToUpdate);
        }

        void CheckUpdateFile(FileInfo remote, System.IO.FileInfo local, ref List<FileInfo> filesToUpdate)
        {
            if (!local.Exists || !local.Length.Equals(remote.Length) || local.LastWriteTimeUtc < remote.LastWriteTimeUtc)
                filesToUpdate.Add(remote);
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
    }
}
