using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetStatus()
        {
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.BottomContentPanel.Height = 60;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Fade(LoginPanel, 0, null, 250, (a, b) => {
                LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
                ProgressPanel.Visibility = System.Windows.Visibility.Visible;
                ProgressPanel.Opacity = 0;
                Fade(ProgressPanel, 1, null, 250);
            });
        }

        public void Fade(FrameworkElement c, double targetOpacity, EasingFunctionBase f = null, double ms = 500.0, EventHandler onFinish = null)
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
