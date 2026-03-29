using FaceDiff.Core;
using FaceDiff.Models;
using FaceDiff.Services;

namespace FaceDiff.ViewModels
{
    public abstract class StepViewModel : ViewModelBase
    {
        private bool _isEnabled;
        private bool _isCompleted;
        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public SessionData Session { get; set; }
        public UserSettings Settings { get; set; }

        public virtual void OnNavigatedTo() { }
        public virtual void OnNavigatedFrom() { }
    }
}
