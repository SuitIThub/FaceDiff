using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using FaceDiff.Core;
using FaceDiff.Models;
using FaceDiff.Services;

namespace FaceDiff.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private int _currentStepIndex;
        private readonly SessionData _session = new SessionData();
        private readonly UserSettings _settings;

        public MainViewModel()
        {
            _settings = UserSettings.Load();
            Steps = new ObservableCollection<StepViewModel>();
            InitializeSteps();
            UpdateAlignmentStepEnabled();
            Steps[0].OnNavigatedTo();
        }

        public UserSettings Settings => _settings;

        public void SaveSettings() => _settings.Save();

        private void InitializeSteps()
        {
            var step1 = new ImageSelectionViewModel { Title = "1. Image Selection", IsEnabled = true, Session = _session, Settings = _settings };
            var step2 = new BasePreparationViewModel { Title = "2. Face Detection", IsEnabled = false, Session = _session, Settings = _settings };
            var step3 = new DiffGenerationViewModel { Title = "3. Diff Generation", IsEnabled = false, Session = _session, Settings = _settings };
            var step4 = new FinishedViewModel { Title = "4. Finished", IsEnabled = false, Session = _session, Settings = _settings };
            var step5 = new AlignmentViewModel { Title = "5. Alignment", IsEnabled = false, Session = _session, Settings = _settings };
            step4.RetryAction = RetryDenied;

            step1.PropertyChanged += OnStepPropertyChanged;
            step2.PropertyChanged += OnStepPropertyChanged;
            step3.PropertyChanged += OnStepPropertyChanged;
            step4.PropertyChanged += OnStepPropertyChanged;
            step5.PropertyChanged += OnStepPropertyChanged;

            Steps.Add(step1);
            Steps.Add(step2);
            Steps.Add(step3);
            Steps.Add(step4);
            Steps.Add(step5);
        }

        public ObservableCollection<StepViewModel> Steps { get; }

        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (value < 0 || value >= Steps.Count) return;
                if (!Steps[value].IsEnabled) return;

                if (_currentStepIndex >= 0 && _currentStepIndex < Steps.Count)
                    Steps[_currentStepIndex].OnNavigatedFrom();

                SetProperty(ref _currentStepIndex, value);
                Steps[value].OnNavigatedTo();
            }
        }

        public StepViewModel CurrentStep => Steps.Count > 0 ? Steps[_currentStepIndex] : null;

        private void OnStepPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(StepViewModel.IsCompleted)) return;

            for (int i = 1; i < Steps.Count - 1; i++)
            {
                Steps[i].IsEnabled = Steps[i - 1].IsCompleted;
            }

            UpdateAlignmentStepEnabled();
        }

        private void UpdateAlignmentStepEnabled()
        {
            var step5 = Steps[Steps.Count - 1] as AlignmentViewModel;
            if (step5 == null) return;

            bool step1Done = Steps[0].IsCompleted;
            bool step3Enabled = Steps[2].IsEnabled;
            bool step4Done = Steps[Steps.Count - 2].IsCompleted;
            bool destHasFiles = false;

            string destPath = _settings.DestinationPath;
            if (!string.IsNullOrEmpty(destPath) && System.IO.Directory.Exists(destPath))
            {
                try { destHasFiles = System.IO.Directory.EnumerateFiles(destPath).Any(); }
                catch { }
            }

            step5.IsEnabled = step4Done || (step1Done && destHasFiles);
            step5.ShowDestinationField = !step3Enabled;
        }

        public void NavigateToStep(int index)
        {
            CurrentStepIndex = index;
        }

        public void RetryDenied()
        {
            var step2 = Steps[1] as BasePreparationViewModel;
            step2?.LoadDeniedImages();

            Steps[1].IsCompleted = false;
            Steps[2].IsCompleted = false;
            Steps[3].IsCompleted = false;

            CurrentStepIndex = 1;
        }
    }
}
