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
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ModernMinas.Launcher
{
    static class FadeExtension
    {
        public static void Fade(this FrameworkElement c, double targetOpacity, /*EasingFunctionBase f = null, */double ms = 500.0, EventHandler onFinish = null)
        {
            c.Dispatcher.Invoke(new Action(delegate()
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

                Storyboard.SetTarget(animation, c);
                Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));

                storyboard.Children.Add(animation);

                if (onFinish != null)
                    storyboard.Completed += onFinish;

                storyboard.Begin(c);
            }));
        }
    }
}
