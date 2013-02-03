using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ModernMinas.Launcher
{
    /// <summary>
    /// Interaktionslogik für MinecraftStatusWindow.xaml
    /// </summary>
    public partial class MinecraftStatusWindow : Window
    {
        public MinecraftStatusWindow()
        {
            InitializeComponent();

            this.Focusable = false;
        }

        public void SetStatus(string text)
        {
            this.Status.Dispatcher.Invoke(new Action(delegate
            {
                this.Status.Content = text.Replace("_", "__"); // fix accidental mnemonics
                this.Status.UpdateLayout();
            }));
        }

        public void SetVisible(bool visible = true)
        {
            this.Status.Dispatcher.Invoke(new Action(() =>
            {
                if (visible)
                    this.Visibility = System.Windows.Visibility.Visible;
                else
                    this.Visibility = System.Windows.Visibility.Hidden;
            }));
        }
    }
}
