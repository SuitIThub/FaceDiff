using System.Windows;
using FaceDiff.Models;
using FaceDiff.Services;

namespace FaceDiff.Controls
{
    public static class LazyThumbnail
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(LazyThumbnail),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement fe)) return;

            if ((bool)e.NewValue)
                fe.Loaded += OnLoaded;
            else
                fe.Loaded -= OnLoaded;
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe)) return;

            switch (fe.DataContext)
            {
                case BaseImageModel b:
                    ThumbnailLoadQueue.EnqueueBase(b);
                    break;
                case ComparisonImageModel c:
                    ThumbnailLoadQueue.EnqueueComparison(c);
                    break;
            }
        }
    }
}
