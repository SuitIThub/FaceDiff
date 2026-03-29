using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FaceDiff.ViewModels;

namespace FaceDiff.Views
{
    public partial class AlignmentView : UserControl
    {
        private bool _isPanning;
        private Point _panStart;
        private double _panStartX;
        private double _panStartY;

        public AlignmentView()
        {
            InitializeComponent();
            PreviewKeyDown += AlignmentView_PreviewKeyDown;
            PreviewKeyUp += AlignmentView_PreviewKeyUp;
            DataContextChanged += (_, __) =>
            {
                if (DataContext is AlignmentViewModel vm)
                    vm.PropertyChanged += Vm_PropertyChanged;
            };
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AlignmentViewModel.ZoomLevel))
            {
                if (sender is AlignmentViewModel vm && vm.ZoomLevel <= 1.0)
                {
                    PreviewTranslate.X = 0;
                    PreviewTranslate.Y = 0;
                }
            }
        }

        private void PreviewContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is AlignmentViewModel vm)
            {
                vm.PreviewWidth = e.NewSize.Width;
                vm.PreviewHeight = e.NewSize.Height;
            }
        }

        private void PreviewBorder_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is not AlignmentViewModel vm) return;

            var mods = Keyboard.Modifiers;
            bool shift = mods.HasFlag(ModifierKeys.Shift);
            bool ctrl = mods.HasFlag(ModifierKeys.Control);
            bool alt = mods.HasFlag(ModifierKeys.Alt);

            // Plain wheel: zoom preview toward cursor. Modifier + wheel: nudge alignment.
            if (!shift && !ctrl && !alt)
            {
                ZoomPreviewTowardMouse(vm, e);
                e.Handled = true;
                return;
            }

            int direction = e.Delta > 0 ? 1 : -1;
            vm.ScrollActiveAxis(direction, shift, ctrl, alt);
            e.Handled = true;
        }

        private void ZoomPreviewTowardMouse(AlignmentViewModel vm, MouseWheelEventArgs e)
        {
            if (PreviewContainer.ActualWidth <= 0 || PreviewContainer.ActualHeight <= 0)
                return;

            Point pos = e.GetPosition(PreviewContainer);
            double oldZoom = vm.ZoomLevel;
            double factor = e.Delta > 0 ? 1.05 : 1.0 / 1.05;
            double newZoom = Math.Clamp(oldZoom * factor, 0.25, 10.0);
            if (Math.Abs(newZoom - oldZoom) < 1e-9)
                return;

            // Keep the canvas point under the cursor fixed: p' = p*z + t  =>  t += p*(z_old - z_new)
            PreviewTranslate.X += pos.X * (oldZoom - newZoom);
            PreviewTranslate.Y += pos.Y * (oldZoom - newZoom);
            vm.ZoomLevel = newZoom;
        }

        private void AlignmentView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab)
                return;

            if (DataContext is AlignmentViewModel vm)
            {
                vm.IsXAxisActive = !vm.IsXAxisActive;
                e.Handled = true;
            }
        }

        private void AlignmentView_PreviewKeyUp(object sender, KeyEventArgs e)
        {
        }

        private void PreviewBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not AlignmentViewModel vm) return;
            if (vm.ZoomLevel <= 1.0) return;

            _isPanning = true;
            _panStart = e.GetPosition(PreviewBorder);
            _panStartX = PreviewTranslate.X;
            _panStartY = PreviewTranslate.Y;
            PreviewBorder.CaptureMouse();
            PreviewBorder.Cursor = Cursors.Hand;
            e.Handled = true;
        }

        private void PreviewBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPanning) return;
            _isPanning = false;
            PreviewBorder.ReleaseMouseCapture();
            PreviewBorder.Cursor = null;
            e.Handled = true;
        }

        private void PreviewBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;

            var current = e.GetPosition(PreviewBorder);
            double dx = current.X - _panStart.X;
            double dy = current.Y - _panStart.Y;

            PreviewTranslate.X = _panStartX + dx;
            PreviewTranslate.Y = _panStartY + dy;
        }

        private void PreviewBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;
            _isPanning = false;
            PreviewBorder.ReleaseMouseCapture();
            PreviewBorder.Cursor = null;
        }
    }
}
