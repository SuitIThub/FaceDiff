using FaceDiff.Core;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace FaceDiff.Models
{
    public enum DetectionStatus
    {
        None,
        AutoDetected,
        ManualOverride,
        NoFaceFound
    }

    public class BaseImageModel : ViewModelBase
    {
        private string _filePath;
        private string _fileName;
        private string _matchGroup;
        private BitmapImage _thumbnail;
        private BitmapImage _faceThumbnail;
        private OvalRegion _oval;
        private DetectionStatus _detectionStatus;
        private bool _isHighlighted;
        private bool _isSelected;
        private System.Windows.Media.Color _highlightColor;

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string MatchGroup
        {
            get => _matchGroup;
            set => SetProperty(ref _matchGroup, value);
        }

        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        public BitmapImage FaceThumbnail
        {
            get => _faceThumbnail;
            set => SetProperty(ref _faceThumbnail, value);
        }

        public OvalRegion Oval
        {
            get => _oval;
            set
            {
                if (SetProperty(ref _oval, value))
                    OnPropertyChanged(nameof(HasOval));
            }
        }

        /// <summary>Raw oval from the face detector before any user scaling is applied.</summary>
        public OvalRegion DetectedOval { get; set; }

        public bool HasOval => _oval != null;

        public DetectionStatus DetectionStatus
        {
            get => _detectionStatus;
            set => SetProperty(ref _detectionStatus, value);
        }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public System.Windows.Media.Color HighlightColor
        {
            get => _highlightColor;
            set => SetProperty(ref _highlightColor, value);
        }

        public ObservableCollection<ComparisonImageModel> MatchedComparisons { get; }
            = new ObservableCollection<ComparisonImageModel>();
    }
}
