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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.Threading.Tasks;

namespace ModernMinas.Launcher
{
    /// <summary>
    /// Interaktionslogik für OptionsDialog.xaml
    /// </summary>
    public partial class OptionsDialog : Window
    {
        public OptionsDialog()
        {
            InitializeComponent();

            RefreshBoxes();
        }

        public void RefreshBoxes()
        {
            for (
                var fs = FileSize.FromMegabytes(256);
                fs < FileSize.FromGigabytes(8);
                fs += FileSize.FromMegabytes(
                    fs < FileSize.FromGigabytes(1) ? 128
                    : fs < FileSize.FromGigabytes(2) ? 256
                    : fs < FileSize.FromGigabytes(4) ? 512
                    : 1024
                )
            )
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.MaximalRam.Items.Add(new FileSize(fs));
                    Console.WriteLine("Added {0} to list", fs);
                }));
            }
        }

        public void Set(FrameworkElement elem, DependencyProperty prop, object value)
        {
            Console.WriteLine("Set {0}.{1} = {2}", elem.Name, prop.Name, value);
            elem.Dispatcher.Invoke(new Action(() => {
                elem.SetValue(prop, value);
            }));
        }

        public object Get(FrameworkElement elem, DependencyProperty prop)
        {
            return elem.Dispatcher.Invoke(new Func<object>(() =>
            {
                return elem.GetValue(prop);
            }));
        }

        public FileSize MaximumRam
        {
            get
            {
                return (FileSize)Get(this.MaximalRam, ComboBox.SelectedItemProperty);
            }
            set
            {
                Set(this.MaximalRam, ComboBox.SelectedItemProperty, value);
            }
        }

        public bool ShouldApply
        {
            get;
            set;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldApply = sender == OKButton;
            Close();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var p = JavaPath.CreateJava("-version");
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    string versionInfo = string.Empty;
                    versionInfo += string.Format("Java path: {0}", p.StartInfo.FileName);
                    versionInfo += Environment.NewLine;
                    versionInfo += Environment.NewLine;
                    versionInfo += p.StandardError.ReadToEnd();
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.JavaDetails.Text = versionInfo;
                    }));
                }
                catch(JavaNotFoundException)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.JavaDetails.Text = "Didn't find a working Java installation. The client will not start.";
                    }));
                }
            });
        }
    }
}
