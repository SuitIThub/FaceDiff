using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FaceDiff.Controls
{
    /// <summary>
    /// Virtualizing wrap panel with fixed item size — only realizes visible children.
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(VirtualizingWrapPanel),
                new FrameworkPropertyMetadata(138.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(VirtualizingWrapPanel),
                new FrameworkPropertyMetadata(158.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        private Size _extent;
        private Size _viewport;
        private Point _offset;
        private ScrollViewer _owner;
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll = true;

        private int ItemsCount
        {
            get
            {
                var items = ItemsControl.GetItemsOwner(this)?.Items;
                return items?.Count ?? 0;
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (ItemContainerGenerator is ItemContainerGenerator generator)
                generator.ItemsChanged += (_, __) => InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? ItemWidth : Math.Max(availableSize.Width, ItemWidth);
            double height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
            _viewport = new Size(width, height);

            int count = ItemsCount;
            int cols = Math.Max(1, (int)(width / ItemWidth));
            int rows = count == 0 ? 0 : (int)Math.Ceiling(count / (double)cols);
            _extent = new Size(width, rows * ItemHeight);

            _owner?.InvalidateScrollInfo();

            var generator = ItemContainerGenerator;
            if (generator == null || count == 0)
            {
                RemoveInternalChildRange(0, InternalChildren.Count);
                return new Size(width, height);
            }

            int firstVisibleRow = Math.Max(0, (int)(_offset.Y / ItemHeight));
            int visibleRows = Math.Max(1, (int)Math.Ceiling(_viewport.Height / ItemHeight) + 2);
            int firstIndex = Math.Min(count - 1, firstVisibleRow * cols);
            firstIndex = Math.Max(0, firstIndex);
            int lastIndex = Math.Min(count - 1, firstIndex + visibleRows * cols - 1);

            CleanUpItems(generator, firstIndex, lastIndex);

            var startPos = generator.GeneratorPositionFromIndex(firstIndex);
            int childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;
            using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = firstIndex; itemIndex <= lastIndex; itemIndex++, childIndex++)
                {
                    bool newlyRealized;
                    var child = (UIElement)generator.GenerateNext(out newlyRealized);
                    if (child == null)
                        break;

                    if (newlyRealized)
                    {
                        if (childIndex >= InternalChildren.Count)
                            AddInternalChild(child);
                        else if (InternalChildren[childIndex] != child)
                            InsertInternalChild(childIndex, child);
                        generator.PrepareItemContainer(child);
                    }
                    else if (!InternalChildren.Contains(child))
                    {
                        InsertInternalChild(Math.Min(childIndex, InternalChildren.Count), child);
                    }

                    child.Measure(new Size(ItemWidth, ItemHeight));
                }
            }

            return new Size(width, height > 0 ? height : Math.Min(_extent.Height, ItemHeight * 3));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _viewport = finalSize;
            int cols = Math.Max(1, (int)(finalSize.Width / ItemWidth));
            var generator = ItemContainerGenerator;
            if (generator == null)
                return finalSize;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
                if (itemIndex < 0)
                    continue;

                int row = itemIndex / cols;
                int col = itemIndex % cols;
                double x = col * ItemWidth;
                double y = row * ItemHeight - _offset.Y;
                child.Arrange(new Rect(x, y, ItemWidth, ItemHeight));
            }

            _owner?.InvalidateScrollInfo();
            return finalSize;
        }

        private void CleanUpItems(IItemContainerGenerator generator, int firstIndex, int lastIndex)
        {
            for (int i = InternalChildren.Count - 1; i >= 0; i--)
            {
                var pos = new GeneratorPosition(i, 0);
                int itemIndex = generator.IndexFromGeneratorPosition(pos);
                if (itemIndex >= 0 && (itemIndex < firstIndex || itemIndex > lastIndex))
                {
                    generator.Remove(pos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        public bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set => _canHorizontallyScroll = value;
        }

        public bool CanVerticallyScroll
        {
            get => _canVerticallyScroll;
            set => _canVerticallyScroll = value;
        }

        public double ExtentWidth => _extent.Width;
        public double ExtentHeight => _extent.Height;
        public double ViewportWidth => _viewport.Width;
        public double ViewportHeight => _viewport.Height;
        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y;

        public ScrollViewer ScrollOwner
        {
            get => _owner;
            set => _owner = value;
        }

        public void LineUp() => SetVerticalOffset(VerticalOffset - ItemHeight);
        public void LineDown() => SetVerticalOffset(VerticalOffset + ItemHeight);
        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - ItemWidth);
        public void LineRight() => SetHorizontalOffset(HorizontalOffset + ItemWidth);
        public void PageUp() => SetVerticalOffset(VerticalOffset - Math.Max(ItemHeight, ViewportHeight));
        public void PageDown() => SetVerticalOffset(VerticalOffset + Math.Max(ItemHeight, ViewportHeight));
        public void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        public void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);
        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - ItemHeight * 3);
        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + ItemHeight * 3);
        public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - ItemWidth * 3);
        public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + ItemWidth * 3);

        public void SetHorizontalOffset(double offset)
        {
            offset = Clamp(offset, 0, Math.Max(0, ExtentWidth - ViewportWidth));
            if (Math.Abs(_offset.X - offset) < 0.1) return;
            _offset.X = offset;
            InvalidateArrange();
            _owner?.InvalidateScrollInfo();
        }

        public void SetVerticalOffset(double offset)
        {
            offset = Clamp(offset, 0, Math.Max(0, ExtentHeight - ViewportHeight));
            if (Math.Abs(_offset.Y - offset) < 0.1) return;
            _offset.Y = offset;
            InvalidateMeasure();
            _owner?.InvalidateScrollInfo();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle) => rectangle;

        private static double Clamp(double v, double min, double max) =>
            v < min ? min : (v > max ? max : v);
    }
}
