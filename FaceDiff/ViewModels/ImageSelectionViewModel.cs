using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using FaceDiff.Core;
using FaceDiff.Models;
using FaceDiff.Services;

namespace FaceDiff.ViewModels
{
    public class ImageSelectionViewModel : StepViewModel
    {
        private string _baseFolderPath;
        private string _comparisonFolderPath;
        private string _baseFilter;
        private string _regexPattern;
        private BaseImageModel _hoveredBaseImage;

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif" };

        private static readonly Color[] MatchColors =
        {
            Color.FromRgb(66, 133, 244),
            Color.FromRgb(234, 67, 53),
            Color.FromRgb(251, 188, 4),
            Color.FromRgb(52, 168, 83),
            Color.FromRgb(255, 109, 0),
            Color.FromRgb(171, 71, 188),
            Color.FromRgb(0, 172, 193),
            Color.FromRgb(124, 179, 66),
            Color.FromRgb(255, 167, 38),
            Color.FromRgb(141, 110, 99),
            Color.FromRgb(38, 166, 154),
            Color.FromRgb(236, 64, 122),
            Color.FromRgb(103, 58, 183),
            Color.FromRgb(0, 150, 136),
            Color.FromRgb(255, 87, 34),
            Color.FromRgb(63, 81, 181),
            Color.FromRgb(205, 220, 57),
            Color.FromRgb(233, 30, 99),
            Color.FromRgb(0, 188, 212),
            Color.FromRgb(139, 195, 74),
            Color.FromRgb(121, 85, 72),
            Color.FromRgb(255, 193, 7),
            Color.FromRgb(33, 150, 243),
            Color.FromRgb(76, 175, 80),
            Color.FromRgb(244, 67, 54),
            Color.FromRgb(156, 39, 176),
            Color.FromRgb(255, 152, 0),
            Color.FromRgb(96, 125, 139),
            Color.FromRgb(0, 137, 123),
            Color.FromRgb(183, 28, 28),
            Color.FromRgb(49, 27, 146),
            Color.FromRgb(0, 105, 92),
            Color.FromRgb(230, 81, 0),
            Color.FromRgb(26, 35, 126),
            Color.FromRgb(46, 125, 50),
            Color.FromRgb(173, 20, 87),
            Color.FromRgb(0, 131, 143),
            Color.FromRgb(158, 157, 36),
            Color.FromRgb(191, 54, 12),
            Color.FromRgb(69, 90, 100),
            Color.FromRgb(106, 27, 154),
            Color.FromRgb(2, 119, 189),
            Color.FromRgb(190, 81, 209),
            Color.FromRgb(216, 67, 21),
        };

        public ImageSelectionViewModel()
        {
            BaseImages = new ObservableCollection<BaseImageModel>();
            ComparisonImages = new ObservableCollection<ComparisonImageModel>();
            BrowseBaseFolderCommand = new RelayCommand(BrowseBaseFolder);
            BrowseComparisonFolderCommand = new RelayCommand(BrowseComparisonFolder);
            ApplyFilterCommand = new RelayCommand(ApplyBaseFilter);
            ApplyRegexCommand = new RelayCommand(() => ApplyRegexMatching());
        }

        public ObservableCollection<BaseImageModel> BaseImages { get; }
        public ObservableCollection<ComparisonImageModel> ComparisonImages { get; }

        public ICommand BrowseBaseFolderCommand { get; }
        public ICommand BrowseComparisonFolderCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ApplyRegexCommand { get; }

        public string BaseFolderPath
        {
            get => _baseFolderPath;
            set
            {
                if (SetProperty(ref _baseFolderPath, value))
                {
                    if (Settings != null) Settings.BaseFolderPath = value;
                    LoadBaseImages();
                }
            }
        }

        public string ComparisonFolderPath
        {
            get => _comparisonFolderPath;
            set
            {
                if (SetProperty(ref _comparisonFolderPath, value))
                {
                    if (Settings != null) Settings.ComparisonFolderPath = value;
                    Session.ComparisonFolderPath = value;
                    LoadComparisonImages();
                }
            }
        }

        public string BaseFilter
        {
            get => _baseFilter;
            set
            {
                if (SetProperty(ref _baseFilter, value) && Settings != null)
                    Settings.BaseFilter = value;
            }
        }

        public string RegexPattern
        {
            get => _regexPattern;
            set
            {
                if (SetProperty(ref _regexPattern, value) && Settings != null)
                    Settings.RegexPattern = value;
            }
        }

        private bool _settingsLoaded;

        public override void OnNavigatedTo()
        {
            if (_settingsLoaded || Settings == null) return;
            _settingsLoaded = true;

            _baseFilter = Settings.BaseFilter;
            OnPropertyChanged(nameof(BaseFilter));
            _regexPattern = Settings.RegexPattern;
            OnPropertyChanged(nameof(RegexPattern));

            if (!string.IsNullOrEmpty(Settings.BaseFolderPath))
                BaseFolderPath = Settings.BaseFolderPath;
            if (!string.IsNullOrEmpty(Settings.ComparisonFolderPath))
                ComparisonFolderPath = Settings.ComparisonFolderPath;
        }

        public void OnBaseImageHover(BaseImageModel model)
        {
            if (_hoveredBaseImage == model) return;
            _hoveredBaseImage = model;

            if (model == null || model.MatchedComparisons.Count == 0)
            {
                foreach (var c in ComparisonImages)
                    c.IsDimmed = false;
                return;
            }

            var matched = new System.Collections.Generic.HashSet<ComparisonImageModel>(model.MatchedComparisons);
            foreach (var c in ComparisonImages)
                c.IsDimmed = !matched.Contains(c);
        }

        public void OnBaseImageUnhover()
        {
            _hoveredBaseImage = null;
            foreach (var c in ComparisonImages)
                c.IsDimmed = false;
        }

        private List<BaseImageModel> _allBaseImages = new List<BaseImageModel>();

        private async void LoadBaseImages()
        {
            _allBaseImages.Clear();
            BaseImages.Clear();

            if (string.IsNullOrWhiteSpace(_baseFolderPath) || !Directory.Exists(_baseFolderPath))
                return;

            var files = Directory.GetFiles(_baseFolderPath)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                var model = new BaseImageModel
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file)
                };
                _allBaseImages.Add(model);
            }

            ApplyBaseFilter();

            foreach (var img in BaseImages.ToList())
            {
                img.Thumbnail = await ThumbnailService.CreateThumbnailAsync(img.FilePath);
            }
        }

        private void ApplyBaseFilter()
        {
            BaseImages.Clear();
            var filtered = string.IsNullOrWhiteSpace(_baseFilter)
                ? _allBaseImages
                : _allBaseImages.Where(i => i.FileName.IndexOf(_baseFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            foreach (var img in filtered)
                BaseImages.Add(img);

            ApplyRegexMatching();
        }

        private async void LoadComparisonImages()
        {
            ComparisonImages.Clear();

            if (string.IsNullOrWhiteSpace(_comparisonFolderPath) || !Directory.Exists(_comparisonFolderPath))
                return;

            var files = Directory.GetFiles(_comparisonFolderPath)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                var model = new ComparisonImageModel
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file)
                };
                ComparisonImages.Add(model);
            }

            ApplyRegexMatching();

            foreach (var img in ComparisonImages.ToList())
            {
                img.Thumbnail = await ThumbnailService.CreateThumbnailAsync(img.FilePath);
            }
        }

        private void ApplyRegexMatching()
        {
            foreach (var b in BaseImages)
            {
                b.MatchedComparisons.Clear();
                b.MatchGroup = null;
                b.HighlightColor = Colors.Transparent;
            }
            foreach (var c in ComparisonImages)
            {
                c.MatchGroup = null;
                c.HighlightColor = Colors.Transparent;
            }

            if (string.IsNullOrWhiteSpace(_regexPattern))
            {
                UpdateCompletion();
                return;
            }

            Regex regex;
            try { regex = new Regex(_regexPattern); }
            catch { UpdateCompletion(); return; }

            var baseGroups = new Dictionary<string, List<BaseImageModel>>();
            foreach (var b in BaseImages)
            {
                var m = regex.Match(b.FileName);
                if (m.Success && m.Groups.Count > 1)
                {
                    b.MatchGroup = m.Groups[1].Value;
                    if (!baseGroups.ContainsKey(b.MatchGroup))
                        baseGroups[b.MatchGroup] = new List<BaseImageModel>();
                    baseGroups[b.MatchGroup].Add(b);
                }
            }

            foreach (var c in ComparisonImages)
            {
                var m = regex.Match(c.FileName);
                if (m.Success && m.Groups.Count > 1)
                {
                    c.MatchGroup = m.Groups[1].Value;
                }
            }

            var groupKeys = baseGroups.Keys.OrderBy(k => k).ToList();
            int colorIdx = 0;
            foreach (var key in groupKeys)
            {
                var color = MatchColors[colorIdx % MatchColors.Length];
                colorIdx++;

                foreach (var b in baseGroups[key])
                    b.HighlightColor = color;

                var matchedComps = ComparisonImages.Where(c => c.MatchGroup == key).ToList();
                foreach (var c in matchedComps)
                    c.HighlightColor = color;

                foreach (var b in baseGroups[key])
                {
                    foreach (var c in matchedComps)
                        b.MatchedComparisons.Add(c);
                }
            }

            UpdateCompletion();
        }




        private void UpdateCompletion()
        {
            bool hasMatches = BaseImages.Any(b => b.MatchedComparisons.Count > 0);
            IsCompleted = hasMatches;

            if (hasMatches)
            {
                Session.BaseImages.Clear();
                Session.ComparisonImages.Clear();
                foreach (var b in BaseImages.Where(b => b.MatchedComparisons.Count > 0))
                    Session.BaseImages.Add(b);
                foreach (var c in ComparisonImages.Where(c => !string.IsNullOrEmpty(c.MatchGroup)))
                    Session.ComparisonImages.Add(c);
            }
        }

        private void BrowseBaseFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Base Images Folder";
                if (!string.IsNullOrEmpty(_baseFolderPath))
                    dialog.SelectedPath = _baseFolderPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    BaseFolderPath = dialog.SelectedPath;
            }
        }

        private void BrowseComparisonFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Comparison Images Folder";
                if (!string.IsNullOrEmpty(_comparisonFolderPath))
                    dialog.SelectedPath = _comparisonFolderPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    ComparisonFolderPath = dialog.SelectedPath;
            }
        }



    }

    public class RelayCommand<T> : RelayCommand
    {
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
            : base(o => execute((T)o), canExecute != null ? new Predicate<object>(o => canExecute((T)o)) : null)
        {
        }
    }
}
