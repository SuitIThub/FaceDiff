using System.Windows;
using FaceDiff.ViewModels;

namespace FaceDiff
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SaveSettings();
        }
    }
}
