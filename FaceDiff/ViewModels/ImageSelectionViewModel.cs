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
        private string _resolvedBaseFolderPath;
        private string _resolvedComparisonFolderPath;
        private string _resolvedBaseFilter;
        private string _resolvedRegexPattern;

        private static readonly IReadOnlyDictionary<string, string> EmptyTemplateParams = new Dictionary<string, string>();

        private static IReadOnlyDictionary<string, string> TemplateParams(UserSettings s)
        {
            if (s?.TemplateParameters == null)
                return EmptyTemplateParams;
            return s.TemplateParameters;
        }

        private string Interpolate(string value) => TemplateInterpolation.Apply(value ?? "", TemplateParams(Settings));

        private void RaiseInterpolationPreviews()
        {
            OnPropertyChanged(nameof(BaseFolderPathPreview));
            OnPropertyChanged(nameof(ComparisonFolderPathPreview));
            OnPropertyChanged(nameof(BaseFilterPreview));
            OnPropertyChanged(nameof(RegexPatternPreview));
        }

        private static bool StringEqualsIgnoreCase(string a, string b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        protected override void OnTemplateParametersChanged()
        {
            RaiseInterpolationPreviews();

            // Template parameter edits can change the resolved base/comparison paths and the regex/filter.
            // Re-run the same logic that normally runs when the corresponding TextBoxes change.
            var newResolvedBasePath = Interpolate(_baseFolderPath);
            var newResolvedComparisonPath = Interpolate(_comparisonFolderPath);
            var newResolvedFilter = Interpolate(_baseFilter);
            var newResolvedRegexPattern = Interpolate(_regexPattern);

            bool basePathChanged = !StringEqualsIgnoreCase(newResolvedBasePath, _resolvedBaseFolderPath);
            bool comparisonPathChanged = !StringEqualsIgnoreCase(newResolvedComparisonPath, _resolvedComparisonFolderPath);
            bool baseFilterChanged = !string.Equals(newResolvedFilter, _resolvedBaseFilter, StringComparison.Ordinal);
            bool regexChanged = !string.Equals(newResolvedRegexPattern, _resolvedRegexPattern, StringComparison.Ordinal);

            _resolvedBaseFolderPath = newResolvedBasePath;
            _resolvedComparisonFolderPath = newResolvedComparisonPath;
            _resolvedBaseFilter = newResolvedFilter;
            _resolvedRegexPattern = newResolvedRegexPattern;

            if (basePathChanged)
            {
                LoadBaseImages();
                // LoadBaseImages calls ApplyBaseFilter which triggers ApplyRegexMatching.
            }
            else if (baseFilterChanged)
            {
                // Only the filter/pattern changed: we already have _allBaseImages for the base directory.
                ApplyBaseFilter();
            }

            if (comparisonPathChanged)
            {
                // LoadComparisonImages calls ApplyRegexMatching.
                LoadComparisonImages();
            }

            // If we didn't reload either collection, but only the regex changed, re-run matching.
            if (!basePathChanged && !comparisonPathChanged && regexChanged && !baseFilterChanged)
                ApplyRegexMatching();
        }

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
            ParameterRows = new ObservableCollection<ParameterRowViewModel>();
            BrowseBaseFolderCommand = new RelayCommand(BrowseBaseFolder);
            BrowseComparisonFolderCommand = new RelayCommand(BrowseComparisonFolder);
            ApplyFilterCommand = new RelayCommand(ApplyBaseFilter);
            ApplyRegexCommand = new RelayCommand(() => ApplyRegexMatching());
            AddParameterCommand = new RelayCommand(AddParameterRow);
        }

        public ObservableCollection<BaseImageModel> BaseImages { get; }
        public ObservableCollection<ComparisonImageModel> ComparisonImages { get; }
        public ObservableCollection<ParameterRowViewModel> ParameterRows { get; }

        public ICommand BrowseBaseFolderCommand { get; }
        public ICommand BrowseComparisonFolderCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ApplyRegexCommand { get; }
        public ICommand AddParameterCommand { get; }

        public string BaseFolderPath
        {
            get => _baseFolderPath;
            set
            {
                if (SetProperty(ref _baseFolderPath, value))
                {
                    if (Settings != null) Settings.BaseFolderPath = value;
                    RaiseInterpolationPreviews();
                    LoadBaseImages();
                }
            }
        }

        public string BaseFolderPathPreview => Interpolate(_baseFolderPath);

        public string ComparisonFolderPath
        {
            get => _comparisonFolderPath;
            set
            {
                if (SetProperty(ref _comparisonFolderPath, value))
                {
                    if (Settings != null) Settings.ComparisonFolderPath = value;
                    Session.ComparisonFolderPath = value;
                    RaiseInterpolationPreviews();
                    LoadComparisonImages();
                }
            }
        }

        public string ComparisonFolderPathPreview => Interpolate(_comparisonFolderPath);

        public string BaseFilter
        {
            get => _baseFilter;
            set
            {
                if (!SetProperty(ref _baseFilter, value))
                    return;
                if (Settings != null)
                    Settings.BaseFilter = value;
                RaiseInterpolationPreviews();
            }
        }

        public string BaseFilterPreview => Interpolate(_baseFilter);

        public string RegexPattern
        {
            get => _regexPattern;
            set
            {
                if (!SetProperty(ref _regexPattern, value))
                    return;
                if (Settings != null)
                    Settings.RegexPattern = value;
                RaiseInterpolationPreviews();
            }
        }

        public string RegexPatternPreview => Interpolate(_regexPattern);

        private bool _settingsLoaded;

        public override void OnNavigatedTo()
        {
            if (Settings == null) return;
            if (!_settingsLoaded)
            {
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

            LoadParameterRows();
        }

        private void EnsureTemplateParameters()
        {
            if (Settings == null) return;
            if (Settings.TemplateParameters == null)
                Settings.TemplateParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private void LoadParameterRows()
        {
            if (Settings == null) return;
            ParameterRows.Clear();
            EnsureTemplateParameters();
            foreach (var kv in Settings.TemplateParameters.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                ParameterRows.Add(new ParameterRowViewModel(this, kv.Key, kv.Value));
            if (ParameterRows.Count == 0)
                ParameterRows.Add(new ParameterRowViewModel(this, "", ""));
        }

        internal void SyncParametersFromRows()
        {
            if (Settings == null) return;
            EnsureTemplateParameters();
            Settings.TemplateParameters.Clear();
            foreach (var r in ParameterRows)
            {
                if (string.IsNullOrWhiteSpace(r.Key))
                    continue;
                var k = r.Key.Trim();
                Settings.TemplateParameters[k] = r.Value ?? "";
            }

            Session?.RaiseTemplateParametersChanged();
        }

        public void RemoveParameterRow(ParameterRowViewModel row)
        {
            ParameterRows.Remove(row);
            if (ParameterRows.Count == 0)
                ParameterRows.Add(new ParameterRowViewModel(this, "", ""));
            SyncParametersFromRows();
        }

        private void AddParameterRow()
        {
            ParameterRows.Add(new ParameterRowViewModel(this, "", ""));
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

            string basePath = Interpolate(_baseFolderPath);
            if (string.IsNullOrWhiteSpace(_baseFolderPath) || string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
                return;

            var files = Directory.GetFiles(basePath)
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
            string filter = Interpolate(_baseFilter);
            List<BaseImageModel> filtered;
            if (string.IsNullOrWhiteSpace(filter))
            {
                filtered = _allBaseImages;
            }
            else
            {
                try
                {
                    var regex = new Regex(filter, RegexOptions.IgnoreCase);
                    filtered = _allBaseImages.Where(i => regex.IsMatch(i.FileName)).ToList();
                }
                catch
                {
                    // Invalid regex: show no base images until pattern is corrected.
                    filtered = new List<BaseImageModel>();
                }
            }

            foreach (var img in filtered)
                BaseImages.Add(img);

            ApplyRegexMatching();
        }

        private async void LoadComparisonImages()
        {
            ComparisonImages.Clear();

            string compPath = Interpolate(_comparisonFolderPath);
            if (string.IsNullOrWhiteSpace(_comparisonFolderPath) || string.IsNullOrWhiteSpace(compPath) || !Directory.Exists(compPath))
                return;

            var files = Directory.GetFiles(compPath)
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

            string pattern = Interpolate(_regexPattern);
            if (string.IsNullOrWhiteSpace(pattern))
            {
                UpdateCompletion();
                return;
            }

            Regex regex;
            try { regex = new Regex(pattern); }
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
            else
            {
                // Prevent downstream steps from using stale session data when the pattern resolves to no matches.
                Session.BaseImages.Clear();
                Session.ComparisonImages.Clear();
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

    public class ParameterRowViewModel : ViewModelBase
    {
        private readonly ImageSelectionViewModel _owner;
        private string _key;
        private string _value;

        public ParameterRowViewModel(ImageSelectionViewModel owner, string key, string value)
        {
            _owner = owner;
            _key = key;
            _value = value;
            RemoveCommand = new RelayCommand(() => _owner.RemoveParameterRow(this));
        }

        public string Key
        {
            get => _key;
            set
            {
                if (SetProperty(ref _key, value))
                    _owner.SyncParametersFromRows();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                    _owner.SyncParametersFromRows();
            }
        }

        public ICommand RemoveCommand { get; }
    }

    public class RelayCommand<T> : RelayCommand
    {
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
            : base(o => execute((T)o), canExecute != null ? new Predicate<object>(o => canExecute((T)o)) : null)
        {
        }
    }
}
