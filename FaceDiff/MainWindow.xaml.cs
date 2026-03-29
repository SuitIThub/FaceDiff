using System;
using System.Threading.Tasks;
using System.Windows;
using FaceDiff.Interop;
using FaceDiff.Services;
using FaceDiff.ViewModels;

namespace FaceDiff
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowDarkMode.TryApplyDarkTitleBar(this);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                await UpdateService.CheckOnStartupAsync(this, vm);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SaveSettings();
        }
    }
}
