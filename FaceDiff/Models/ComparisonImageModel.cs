using FaceDiff.Core;
using System.Windows.Media.Imaging;

namespace FaceDiff.Models
{
    public class ComparisonImageModel : ViewModelBase
    {
        private string _filePath;
        private string _fileName;
        private string _matchGroup;
        private BitmapImage _thumbnail;
        private bool _isHighlighted;
        private bool _isDimmed;
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

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        public bool IsDimmed
        {
            get => _isDimmed;
            set => SetProperty(ref _isDimmed, value);
        }

        public System.Windows.Media.Color HighlightColor
        {
            get => _highlightColor;
            set => SetProperty(ref _highlightColor, value);
        }
    }
}
