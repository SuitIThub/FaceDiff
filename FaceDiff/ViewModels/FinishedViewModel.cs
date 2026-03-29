using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FaceDiff.Core;
using FaceDiff.Models;

namespace FaceDiff.ViewModels
{
    public class FinishedViewModel : StepViewModel
    {
        private int _totalBaseImages;
        private int _totalComparisons;
        private int _acceptedCount;
        private int _deniedCount;
        private bool _hasDenied;

        public FinishedViewModel()
        {
            ResultItems = new ObservableCollection<ProcessResult>();
            RetryDeniedCommand = new RelayCommand(OnRetryDenied, () => _hasDenied);
        }

        public ObservableCollection<ProcessResult> ResultItems { get; }

        public int TotalBaseImages
        {
            get => _totalBaseImages;
            set => SetProperty(ref _totalBaseImages, value);
        }

        public int TotalComparisons
        {
            get => _totalComparisons;
            set => SetProperty(ref _totalComparisons, value);
        }

        public int AcceptedCount
        {
            get => _acceptedCount;
            set => SetProperty(ref _acceptedCount, value);
        }

        public int DeniedCount
        {
            get => _deniedCount;
            set => SetProperty(ref _deniedCount, value);
        }

        public bool HasDenied
        {
            get => _hasDenied;
            set => SetProperty(ref _hasDenied, value);
        }

        public ICommand RetryDeniedCommand { get; }

        /// <summary>
        /// Action set by MainViewModel to handle the retry navigation.
        /// </summary>
        public System.Action RetryAction { get; set; }

        public override void OnNavigatedTo()
        {
            ResultItems.Clear();
            foreach (var r in Session.Results)
                ResultItems.Add(r);

            TotalBaseImages = Session.Results.Count;
            TotalComparisons = Session.Results.Sum(r => r.ComparisonCount);
            AcceptedCount = Session.Results.Count(r => r.Accepted);
            DeniedCount = Session.Results.Count(r => !r.Accepted);
            HasDenied = DeniedCount > 0;
            IsCompleted = AcceptedCount > 0;
        }

        private void OnRetryDenied()
        {
            RetryAction?.Invoke();
        }
    }
}
