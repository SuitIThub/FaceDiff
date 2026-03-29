using System.Windows.Controls;
using System.Windows.Input;
using FaceDiff.ViewModels;

namespace FaceDiff.Views
{
    public partial class DiffGenerationView : UserControl
    {
        public DiffGenerationView()
        {
            InitializeComponent();
        }

        private void DiffPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DiffGenerationViewModel vm && vm.IsWaitingForDecision)
                vm.AreButtonsActivated = true;
        }
    }
}
