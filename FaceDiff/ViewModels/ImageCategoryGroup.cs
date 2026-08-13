using System;
using System.Collections;
using System.Collections.ObjectModel;
using FaceDiff.Core;
using FaceDiff.Models;

namespace FaceDiff.ViewModels
{
    public class ImageCategoryGroup : ViewModelBase
    {
        private readonly Action _onEnabledChanged;
        private readonly Action<ImageCategoryGroup> _onExpandedChanged;
        private string _name;
        private bool _isEnabled = true;
        private bool _isExpanded;
        private int _baseCount;
        private int _comparisonCount;
        private int _matchCount;

        public ImageCategoryGroup(
            string name,
            Action onEnabledChanged = null,
            Action<ImageCategoryGroup> onExpandedChanged = null)
        {
            _name = name;
            _onEnabledChanged = onEnabledChanged;
            _onExpandedChanged = onExpandedChanged;
            BaseImages = new RangeObservableCollection<BaseImageModel>();
            ComparisonImages = new RangeObservableCollection<ComparisonImageModel>();
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                    _onEnabledChanged?.Invoke();
            }
        }

        /// <summary>Default collapsed so expanding does not realize thousands of visuals at once.</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value))
                {
                    OnPropertyChanged(nameof(DisplayBaseImages));
                    OnPropertyChanged(nameof(DisplayComparisonImages));
                    _onExpandedChanged?.Invoke(this);
                }
            }
        }

        public int BaseCount
        {
            get => _baseCount;
            set => SetProperty(ref _baseCount, value);
        }

        public int ComparisonCount
        {
            get => _comparisonCount;
            set => SetProperty(ref _comparisonCount, value);
        }

        public int MatchCount
        {
            get => _matchCount;
            set => SetProperty(ref _matchCount, value);
        }

        public RangeObservableCollection<BaseImageModel> BaseImages { get; }
        public RangeObservableCollection<ComparisonImageModel> ComparisonImages { get; }

        /// <summary>Empty when collapsed so ItemsControl does not generate containers.</summary>
        public IEnumerable DisplayBaseImages =>
            _isExpanded ? (IEnumerable)BaseImages : Array.Empty<BaseImageModel>();

        public IEnumerable DisplayComparisonImages =>
            _isExpanded ? (IEnumerable)ComparisonImages : Array.Empty<ComparisonImageModel>();

        public void RaiseDisplayChanged()
        {
            OnPropertyChanged(nameof(DisplayBaseImages));
            OnPropertyChanged(nameof(DisplayComparisonImages));
        }
    }
}
