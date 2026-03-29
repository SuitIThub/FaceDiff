using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FaceDiff.Models;
using FaceDiff.ViewModels;

namespace FaceDiff.Views
{
    public partial class ImageSelectionView : UserControl
    {
        public ImageSelectionView()
        {
            InitializeComponent();
        }

        private void BaseImage_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement fe
                && fe.DataContext is BaseImageModel model
                && DataContext is ImageSelectionViewModel vm)
            {
                vm.OnBaseImageHover(model);
            }
        }

        private void BaseImage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is ImageSelectionViewModel vm)
                vm.OnBaseImageUnhover();
        }

        private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ImageSelectionViewModel vm)
                vm.ApplyFilterCommand.Execute(null);
        }

        private void RegexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ImageSelectionViewModel vm)
                vm.ApplyRegexCommand.Execute(null);
        }
    }
}
