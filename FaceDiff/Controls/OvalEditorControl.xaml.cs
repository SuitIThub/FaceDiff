using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FaceDiff.Controls
{
    public partial class OvalEditorControl : UserControl
    {
        private enum DragMode { None, Creating, Moving, Resizing }

        private DragMode _mode;
        private string _resizeHandle;
        private double _resizeAnchor;
        private Point _dragStart;
        private double _startCX, _startCY;

        private bool _zoomFollowOval;

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register(nameof(ImagePath), typeof(string), typeof(OvalEditorControl),
                new PropertyMetadata(null, OnImagePathChanged));

        public static readonly DependencyProperty OvalCenterXProperty =
            DependencyProperty.Register(nameof(OvalCenterX), typeof(double), typeof(OvalEditorControl),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnOvalChanged));

        public static readonly DependencyProperty OvalCenterYProperty =
            DependencyProperty.Register(nameof(OvalCenterY), typeof(double), typeof(OvalEditorControl),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnOvalChanged));

        public static readonly DependencyProperty OvalRadiusXProperty =
            DependencyProperty.Register(nameof(OvalRadiusX), typeof(double), typeof(OvalEditorControl),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnOvalChanged));

        public static readonly DependencyProperty OvalRadiusYProperty =
            DependencyProperty.Register(nameof(OvalRadiusY), typeof(double), typeof(OvalEditorControl),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnOvalChanged));

        public OvalEditorControl()
        {
            InitializeComponent();
            _zoomFollowOval = true;
        }

        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        public double OvalCenterX
        {
            get => (double)GetValue(OvalCenterXProperty);
            set => SetValue(OvalCenterXProperty, value);
        }

        public double OvalCenterY
        {
            get => (double)GetValue(OvalCenterYProperty);
            set => SetValue(OvalCenterYProperty, value);
        }

        public double OvalRadiusX
        {
            get => (double)GetValue(OvalRadiusXProperty);
            set => SetValue(OvalRadiusXProperty, value);
        }

        public double OvalRadiusY
        {
            get => (double)GetValue(OvalRadiusYProperty);
            set => SetValue(OvalRadiusYProperty, value);
        }

        private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (OvalEditorControl)d;
            ctrl.LoadImage();
        }

        private static void OnOvalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (OvalEditorControl)d;
            ctrl.UpdateOvalVisual();
            if (ctrl._zoomFollowOval && ctrl.HasOval && ctrl._mode == DragMode.None)
                ctrl.ApplyZoomToOval();
        }

        private void LoadImage()
        {
            if (string.IsNullOrEmpty(ImagePath)) return;

            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(ImagePath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                BackgroundImage.Source = bi;
                EditorCanvas.Width = bi.PixelWidth;
                EditorCanvas.Height = bi.PixelHeight;

                UpdateOvalVisual();

                // If we already have an oval when loading the image and zoom-follow is enabled,
                // automatically zoom to the oval once so manual editing starts focused.
                if (_zoomFollowOval && HasOval)
                    ApplyZoomToOval();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
            }
        }

        private void UpdateOvalVisual()
        {
            double rx = OvalRadiusX;
            double ry = OvalRadiusY;
            double cx = OvalCenterX;
            double cy = OvalCenterY;

            if (rx <= 0 || ry <= 0)
            {
                OvalShape.Visibility = Visibility.Collapsed;
                HandleTop.Visibility = Visibility.Collapsed;
                HandleBottom.Visibility = Visibility.Collapsed;
                HandleLeft.Visibility = Visibility.Collapsed;
                HandleRight.Visibility = Visibility.Collapsed;
                return;
            }

            OvalShape.Visibility = Visibility.Visible;
            HandleTop.Visibility = Visibility.Visible;
            HandleBottom.Visibility = Visibility.Visible;
            HandleLeft.Visibility = Visibility.Visible;
            HandleRight.Visibility = Visibility.Visible;

            OvalShape.Width = rx * 2;
            OvalShape.Height = ry * 2;
            Canvas.SetLeft(OvalShape, cx - rx);
            Canvas.SetTop(OvalShape, cy - ry);

            Canvas.SetLeft(HandleTop, cx - 6);
            Canvas.SetTop(HandleTop, cy - ry - 6);

            Canvas.SetLeft(HandleBottom, cx - 6);
            Canvas.SetTop(HandleBottom, cy + ry - 6);

            Canvas.SetLeft(HandleLeft, cx - rx - 6);
            Canvas.SetTop(HandleLeft, cy - 6);

            Canvas.SetLeft(HandleRight, cx + rx - 6);
            Canvas.SetTop(HandleRight, cy - 6);
        }

        private bool HasOval => OvalRadiusX > 0 && OvalRadiusY > 0;

        private bool IsInsideOval(Point pt)
        {
            if (!HasOval) return false;
            double dx = pt.X - OvalCenterX;
            double dy = pt.Y - OvalCenterY;
            return (dx * dx) / (OvalRadiusX * OvalRadiusX)
                 + (dy * dy) / (OvalRadiusY * OvalRadiusY) <= 1.0;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_mode != DragMode.None) return;

            _dragStart = e.GetPosition(EditorCanvas);

            if (HasOval && IsInsideOval(_dragStart))
            {
                _mode = DragMode.Moving;
                _startCX = OvalCenterX;
                _startCY = OvalCenterY;
            }
            else
            {
                _mode = DragMode.Creating;
                _startCX = _dragStart.X;
                _startCY = _dragStart.Y;
                OvalCenterX = _dragStart.X;
                OvalCenterY = _dragStart.Y;
                OvalRadiusX = 1;
                OvalRadiusY = 1;
            }

            EditorCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            OvalCenterX = 0;
            OvalCenterY = 0;
            OvalRadiusX = 0;
            OvalRadiusY = 0;
            e.Handled = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mode == DragMode.None) return;

            var pos = e.GetPosition(EditorCanvas);

            switch (_mode)
            {
                case DragMode.Creating:
                    OvalCenterX = _startCX;
                    OvalCenterY = _startCY;
                    OvalRadiusX = Math.Max(5, Math.Abs(pos.X - _startCX));
                    OvalRadiusY = Math.Max(5, Math.Abs(pos.Y - _startCY));
                    break;

                case DragMode.Moving:
                    double dx = pos.X - _dragStart.X;
                    double dy = pos.Y - _dragStart.Y;
                    OvalCenterX = _startCX + dx;
                    OvalCenterY = _startCY + dy;
                    break;

                case DragMode.Resizing:
                    double newRadius;
                    switch (_resizeHandle)
                    {
                        case "Top":
                            newRadius = Math.Max(5, (_resizeAnchor - pos.Y) / 2.0);
                            OvalRadiusY = newRadius;
                            OvalCenterY = _resizeAnchor - newRadius;
                            break;
                        case "Bottom":
                            newRadius = Math.Max(5, (pos.Y - _resizeAnchor) / 2.0);
                            OvalRadiusY = newRadius;
                            OvalCenterY = _resizeAnchor + newRadius;
                            break;
                        case "Left":
                            newRadius = Math.Max(5, (_resizeAnchor - pos.X) / 2.0);
                            OvalRadiusX = newRadius;
                            OvalCenterX = _resizeAnchor - newRadius;
                            break;
                        case "Right":
                            newRadius = Math.Max(5, (pos.X - _resizeAnchor) / 2.0);
                            OvalRadiusX = newRadius;
                            OvalCenterX = _resizeAnchor + newRadius;
                            break;
                    }
                    break;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _mode = DragMode.None;
            EditorCanvas.ReleaseMouseCapture();
            if (_zoomFollowOval && HasOval)
                ApplyZoomToOval();
        }

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mode = DragMode.Resizing;

            if (sender == HandleTop)
            {
                _resizeHandle = "Top";
                _resizeAnchor = OvalCenterY + OvalRadiusY;
            }
            else if (sender == HandleBottom)
            {
                _resizeHandle = "Bottom";
                _resizeAnchor = OvalCenterY - OvalRadiusY;
            }
            else if (sender == HandleLeft)
            {
                _resizeHandle = "Left";
                _resizeAnchor = OvalCenterX + OvalRadiusX;
            }
            else if (sender == HandleRight)
            {
                _resizeHandle = "Right";
                _resizeAnchor = OvalCenterX - OvalRadiusX;
            }

            EditorCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void ZoomToOval_Click(object sender, RoutedEventArgs e)
        {
            if (!HasOval) return;
            _zoomFollowOval = true;
            ApplyZoomToOval();
        }

        private void ApplyZoomToOval()
        {
            if (OvalRadiusX <= 0 || OvalRadiusY <= 0) return;

            double margin = Math.Max(OvalRadiusX, OvalRadiusY) * 0.5;
            double viewWidth = ImageScrollViewer.ViewportWidth;
            double viewHeight = ImageScrollViewer.ViewportHeight;

            double ovalWidth = (OvalRadiusX * 2 + margin * 2);
            double ovalHeight = (OvalRadiusY * 2 + margin * 2);

            double scale = Math.Min(viewWidth / ovalWidth, viewHeight / ovalHeight);
            scale = Math.Max(0.5, Math.Min(scale, 5));

            var transform = new ScaleTransform(scale, scale);
            EditorCanvas.LayoutTransform = transform;

            EditorCanvas.UpdateLayout();

            double scrollX = (OvalCenterX - OvalRadiusX - margin) * scale;
            double scrollY = (OvalCenterY - OvalRadiusY - margin) * scale;
            ImageScrollViewer.ScrollToHorizontalOffset(Math.Max(0, scrollX));
            ImageScrollViewer.ScrollToVerticalOffset(Math.Max(0, scrollY));
        }

        private void FitImage_Click(object sender, RoutedEventArgs e)
        {
            _zoomFollowOval = false;
            EditorCanvas.LayoutTransform = Transform.Identity;
            ImageScrollViewer.ScrollToHome();
        }
    }
}
