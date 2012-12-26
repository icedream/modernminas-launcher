﻿using System;
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
        }

        public void Set(FrameworkElement elem, DependencyProperty prop, object value)
        {
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

        public int MaximumRam
        {
            get
            {
                return Convert.ToInt32(Get(this.MaximalRam, TextBox.TextProperty).ToString());
            }
            set
            {
                Set(this.MaximalRam, TextBox.TextProperty, value.ToString());
            }
        }

        public int MinimumRam
        {
            get
            {
                return Convert.ToInt32(Get(this.MinimalRam, TextBox.TextProperty).ToString());
            }
            set
            {
                Set(this.MinimalRam, TextBox.TextProperty, value.ToString());
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

        private void MaximalRam_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int i = -1;
            if (!int.TryParse(e.Text, out i))
            {
                e.Handled = false;
                return;
            }
            if (i < MinimumRam)
            {
                e.Handled = false;
                return;
            }
            e.Handled = true;
        }

        private void MinimalRam_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int i = -1;
            if (!int.TryParse(e.Text, out i))
            {
                e.Handled = false;
                return;
            }
            if (i > MaximumRam)
            {
                e.Handled = false;
                return;
            }
            e.Handled = true;
        }
    }
}
