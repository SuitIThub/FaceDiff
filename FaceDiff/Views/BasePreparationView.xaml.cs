using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FaceDiff.Models;
using FaceDiff.ViewModels;

namespace FaceDiff.Views
{
    public partial class BasePreparationView : UserControl
    {
        public BasePreparationView()
        {
            InitializeComponent();
        }

        private void BaseCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsUnderButton(e.OriginalSource as DependencyObject))
                return;

            if (sender is FrameworkElement fe
                && fe.DataContext is BaseImageModel model
                && DataContext is BasePreparationViewModel vm)
            {
                vm.ToggleInclude(model);
            }
        }

        private static bool IsUnderButton(DependencyObject source)
        {
            while (source != null)
            {
                if (source is Button)
                    return true;
                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }
    }
}
