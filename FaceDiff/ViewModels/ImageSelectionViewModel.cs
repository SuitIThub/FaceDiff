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
        private string _resolvedFolderModeSignature;
        private bool _isFolderMode;
        private bool _isLoadingImages;
        private bool _isLoadIndeterminate;
        private int _loadProgress;
        private int _loadTotal = 1;
        private string _loadStatusText;
        private int _baseImageCount;
        private int _comparisonImageCount;

        private static readonly IReadOnlyDictionary<string, string> EmptyTemplateParams = new Dictionary<string, string>();

        private static IReadOnlyDictionary<string, string> TemplateParams(UserSettings s)
        {
            if (s?.TemplateParameters == null)
                return EmptyTemplateParams;
            return s.TemplateParameters;
        }

        private string Interpolate(string value) => TemplateInterpolation.Apply(value ?? "", TemplateParams(Settings));

        private string FolderAwarePreview(string template) =>
            TemplateInterpolation.PreviewFolderAware(template ?? "", TemplateParams(Settings));

        private string[] FolderModeTemplates() =>
            new[] { _baseFolderPath, _comparisonFolderPath, _baseFilter, _regexPattern };

        private string GetFolderModeSignature()
        {
            if (!TemplateInterpolation.TryParseFolderMode(TemplateParams(Settings), out var key, out var root, FolderModeTemplates()))
                return "";
            return key + "\0" + root;
        }

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

            var newFolderSig = GetFolderModeSignature();
            bool folderModeChanged = !string.Equals(newFolderSig, _resolvedFolderModeSignature, StringComparison.Ordinal);
            _resolvedFolderModeSignature = newFolderSig;

            if (folderModeChanged || !string.IsNullOrEmpty(newFolderSig))
            {
                _resolvedBaseFolderPath = FolderAwarePreview(_baseFolderPath);
                _resolvedComparisonFolderPath = FolderAwarePreview(_comparisonFolderPath);
                _resolvedBaseFilter = FolderAwarePreview(_baseFilter);
                _resolvedRegexPattern = FolderAwarePreview(_regexPattern);
                ReloadAllImages();
                return;
            }

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

            if (basePathChanged || comparisonPathChanged)
                ReloadAllImages();
            else if (baseFilterChanged || regexChanged)
                _ = RebuildFilterAndMatchAsync();
        }

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif" };

        private static readonly Color[] MatchColors =
        {
            Color.FromRgb(66, 133, 244), Color.FromRgb(234, 67, 53), Color.FromRgb(251, 188, 4),
            Color.FromRgb(52, 168, 83), Color.FromRgb(255, 109, 0), Color.FromRgb(171, 71, 188),
            Color.FromRgb(0, 172, 193), Color.FromRgb(124, 179, 66), Color.FromRgb(255, 167, 38),
            Color.FromRgb(141, 110, 99), Color.FromRgb(38, 166, 154), Color.FromRgb(236, 64, 122),
            Color.FromRgb(103, 58, 183), Color.FromRgb(0, 150, 136), Color.FromRgb(255, 87, 34),
            Color.FromRgb(63, 81, 181), Color.FromRgb(205, 220, 57), Color.FromRgb(233, 30, 99),
            Color.FromRgb(0, 188, 212), Color.FromRgb(139, 195, 74), Color.FromRgb(121, 85, 72),
            Color.FromRgb(255, 193, 7), Color.FromRgb(33, 150, 243), Color.FromRgb(76, 175, 80),
            Color.FromRgb(244, 67, 54), Color.FromRgb(156, 39, 176), Color.FromRgb(255, 152, 0),
            Color.FromRgb(96, 125, 139), Color.FromRgb(0, 137, 123), Color.FromRgb(183, 28, 28),
            Color.FromRgb(49, 27, 146), Color.FromRgb(0, 105, 92), Color.FromRgb(230, 81, 0),
            Color.FromRgb(26, 35, 126), Color.FromRgb(46, 125, 50), Color.FromRgb(173, 20, 87),
            Color.FromRgb(0, 131, 143), Color.FromRgb(158, 157, 36), Color.FromRgb(191, 54, 12),
            Color.FromRgb(69, 90, 100), Color.FromRgb(106, 27, 154), Color.FromRgb(2, 119, 189),
            Color.FromRgb(190, 81, 209), Color.FromRgb(216, 67, 21),
        };

        public ImageSelectionViewModel()
        {
            BaseImages = new RangeObservableCollection<BaseImageModel>();
            ComparisonImages = new RangeObservableCollection<ComparisonImageModel>();
            Categories = new ObservableCollection<ImageCategoryGroup>();
            ParameterRows = new ObservableCollection<ParameterRowViewModel>();
            BrowseBaseFolderCommand = new RelayCommand(BrowseBaseFolder);
            BrowseComparisonFolderCommand = new RelayCommand(BrowseComparisonFolder);
            ApplyFilterCommand = new RelayCommand(() => _ = RebuildFilterAndMatchAsync());
            ApplyRegexCommand = new RelayCommand(() => _ = RebuildFilterAndMatchAsync());
            AddParameterCommand = new RelayCommand(AddParameterRow);
            EnableAllCategoriesCommand = new RelayCommand(() => SetAllCategoriesEnabled(true));
            DisableAllCategoriesCommand = new RelayCommand(() => SetAllCategoriesEnabled(false));
        }

        public RangeObservableCollection<BaseImageModel> BaseImages { get; }
        public RangeObservableCollection<ComparisonImageModel> ComparisonImages { get; }
        public ObservableCollection<ImageCategoryGroup> Categories { get; }
        public ObservableCollection<ParameterRowViewModel> ParameterRows { get; }

        public bool IsFolderMode
        {
            get => _isFolderMode;
            private set
            {
                if (SetProperty(ref _isFolderMode, value))
                    OnPropertyChanged(nameof(IsFlatMode));
            }
        }

        public bool IsFlatMode => !_isFolderMode;

        public int BaseImageCount
        {
            get => _baseImageCount;
            private set => SetProperty(ref _baseImageCount, value);
        }

        public int ComparisonImageCount
        {
            get => _comparisonImageCount;
            private set => SetProperty(ref _comparisonImageCount, value);
        }

        public bool IsLoadingImages
        {
            get => _isLoadingImages;
            private set => SetProperty(ref _isLoadingImages, value);
        }

        public int LoadProgress
        {
            get => _loadProgress;
            private set => SetProperty(ref _loadProgress, value);
        }

        public int LoadTotal
        {
            get => _loadTotal;
            private set => SetProperty(ref _loadTotal, value);
        }

        public string LoadStatusText
        {
            get => _loadStatusText;
            private set => SetProperty(ref _loadStatusText, value);
        }

        public bool IsLoadIndeterminate
        {
            get => _isLoadIndeterminate;
            private set => SetProperty(ref _isLoadIndeterminate, value);
        }

        public ICommand BrowseBaseFolderCommand { get; }
        public ICommand BrowseComparisonFolderCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ApplyRegexCommand { get; }
        public ICommand AddParameterCommand { get; }
        public ICommand EnableAllCategoriesCommand { get; }
        public ICommand DisableAllCategoriesCommand { get; }

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

        public string BaseFolderPathPreview => FolderAwarePreview(_baseFolderPath);

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

        public string ComparisonFolderPathPreview => FolderAwarePreview(_comparisonFolderPath);

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

        public string BaseFilterPreview => FolderAwarePreview(_baseFilter);

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

        public string RegexPatternPreview => FolderAwarePreview(_regexPattern);

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
            _resolvedFolderModeSignature = GetFolderModeSignature();
            if (!string.IsNullOrEmpty(_resolvedFolderModeSignature) && Categories.Count == 0)
                ReloadAllImages();
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

            // Avoid O(n) UI updates across huge comparison sets.
            if (ComparisonImageCount > 400)
                return;

            if (model == null || model.MatchedComparisons.Count == 0)
            {
                foreach (var c in ComparisonImages)
                    c.IsDimmed = false;
                return;
            }

            var matched = new HashSet<ComparisonImageModel>(model.MatchedComparisons);
            foreach (var c in ComparisonImages)
                c.IsDimmed = !matched.Contains(c);
        }

        public void OnBaseImageUnhover()
        {
            _hoveredBaseImage = null;
            if (ComparisonImageCount > 400)
                return;
            foreach (var c in ComparisonImages)
                c.IsDimmed = false;
        }

        private List<BaseImageModel> _allBaseImages = new List<BaseImageModel>();
        private List<ComparisonImageModel> _allComparisonImages = new List<ComparisonImageModel>();
        private int _loadGeneration;

        private bool _suppressCategoryEnabled;

        private void OnCategoryEnabledChanged()
        {
            if (_suppressCategoryEnabled)
                return;
            _ = UpdateCompletionAsync();
        }

        private void SetAllCategoriesEnabled(bool enabled)
        {
            if (!IsFolderMode || Categories.Count == 0)
                return;

            _suppressCategoryEnabled = true;
            try
            {
                foreach (var group in Categories)
                    group.SetEnabledSilent(enabled);
            }
            finally
            {
                _suppressCategoryEnabled = false;
            }

            _ = UpdateCompletionAsync();
        }

        private void OnCategoryExpandedChanged(ImageCategoryGroup group)
        {
            // ItemsSource swaps via Display*; virtualization + LazyThumbnail handle the rest.
        }

        private bool TryGetFolderRoot(out string rootPath) =>
            TemplateInterpolation.TryParseFolderMode(TemplateParams(Settings), out _, out rootPath, FolderModeTemplates());

        private void BeginLoading(string status, int total, bool indeterminate = false)
        {
            IsLoadingImages = true;
            LoadStatusText = status ?? "";
            LoadTotal = Math.Max(total, 1);
            LoadProgress = 0;
            IsLoadIndeterminate = indeterminate;
        }

        private void EndLoading(int generation)
        {
            if (generation != _loadGeneration)
                return;
            IsLoadingImages = false;
            IsLoadIndeterminate = false;
            LoadStatusText = "";
        }

        private void ReloadAllImages()
        {
            ThumbnailLoadQueue.CancelPending();
            if (TryGetFolderRoot(out var rootPath))
                _ = LoadFolderModeAsync(rootPath);
            else
                _ = LoadFlatModeAsync();
        }

        private void LoadBaseImages()
        {
            if (TryGetFolderRoot(out var rootPath))
                _ = LoadFolderModeAsync(rootPath);
            else
                _ = LoadFlatModeAsync();
        }

        private void LoadComparisonImages()
        {
            if (TryGetFolderRoot(out var rootPath))
                _ = LoadFolderModeAsync(rootPath);
            else
                _ = LoadFlatModeAsync();
        }

        private bool IsCurrent(int generation) => generation == _loadGeneration;

        private async Task LoadFlatModeAsync()
        {
            int generation = ++_loadGeneration;
            ThumbnailLoadQueue.CancelPending();
            IsFolderMode = false;
            Categories.Clear();
            BeginLoading("Scanning folders...", 1, indeterminate: true);

            string baseTemplate = _baseFolderPath;
            string compTemplate = _comparisonFolderPath;
            string filterTemplate = _baseFilter;
            string regexTemplate = _regexPattern;
            var parameters = new Dictionary<string, string>(TemplateParams(Settings), StringComparer.OrdinalIgnoreCase);

            var scanned = await Task.Run(() =>
            {
                var bases = EnumerateImages(baseTemplate, parameters, null);
                var comps = EnumerateImages(compTemplate, parameters, null)
                    .Select(b => new ComparisonImageModel
                    {
                        FilePath = b.FilePath,
                        FileName = b.FileName
                    }).ToList();
                return (bases, comps);
            }).ConfigureAwait(true);

            if (!IsCurrent(generation)) return;

            _allBaseImages = scanned.bases;
            _allComparisonImages = scanned.comps;

            BeginLoading("Filtering & matching...", 1, indeterminate: true);
            await ApplyFilterMatchToUiAsync(generation, filterTemplate, regexTemplate, parameters, folderMode: false)
                .ConfigureAwait(true);
            EndLoading(generation);
        }

        private async Task LoadFolderModeAsync(string rootPath)
        {
            int generation = ++_loadGeneration;
            ThumbnailLoadQueue.CancelPending();

            var previousEnabled = Categories.ToDictionary(c => c.Name, c => c.IsEnabled, StringComparer.OrdinalIgnoreCase);

            IsFolderMode = true;
            Categories.Clear();
            BeginLoading("Scanning folders...", 1, indeterminate: true);

            string baseTemplate = _baseFolderPath;
            string compTemplate = _comparisonFolderPath;
            string filterTemplate = _baseFilter;
            string regexTemplate = _regexPattern;
            var parameters = new Dictionary<string, string>(TemplateParams(Settings), StringComparer.OrdinalIgnoreCase);
            string folderKey = TemplateInterpolation.TryParseFolderMode(parameters, out var key, out _, FolderModeTemplates()) ? key : null;

            var scanned = await Task.Run(() =>
            {
                var names = TemplateInterpolation.GetCategoryNames(rootPath).ToList();
                var allBases = new List<BaseImageModel>();
                var allComps = new List<ComparisonImageModel>();
                var groups = new List<(string Name, List<BaseImageModel> Bases, List<ComparisonImageModel> Comps)>();

                foreach (var name in names)
                {
                    var catParams = TemplateInterpolation.WithCategory(parameters, folderKey, name);
                    var bases = EnumerateImages(baseTemplate, catParams, name);
                    var comps = EnumerateImages(compTemplate, catParams, name)
                        .Select(b => new ComparisonImageModel
                        {
                            FilePath = b.FilePath,
                            FileName = b.FileName,
                            Category = name
                        }).ToList();

                    allBases.AddRange(bases);
                    allComps.AddRange(comps);
                    groups.Add((name, bases, comps));
                }

                return (allBases, allComps, groups);
            }).ConfigureAwait(true);

            if (!IsCurrent(generation)) return;

            _allBaseImages = scanned.allBases;
            _allComparisonImages = scanned.allComps;

            Categories.Clear();
            foreach (var g in scanned.groups)
            {
                if (!IsCurrent(generation)) return;

                bool enabled = !previousEnabled.TryGetValue(g.Name, out var prevEn) || prevEn;
                var group = new ImageCategoryGroup(g.Name, OnCategoryEnabledChanged, OnCategoryExpandedChanged)
                {
                    IsEnabled = enabled,
                    IsExpanded = false
                };
                group.ComparisonImages.ReplaceAll(g.Comps);
                group.ComparisonCount = g.Comps.Count;
                Categories.Add(group);
            }

            if (!IsCurrent(generation)) return;

            BeginLoading("Filtering & matching...", 1, indeterminate: true);
            await ApplyFilterMatchToUiAsync(generation, filterTemplate, regexTemplate, parameters, folderMode: true)
                .ConfigureAwait(true);
            EndLoading(generation);
        }

        private static List<BaseImageModel> EnumerateImages(
            string pathTemplate,
            IReadOnlyDictionary<string, string> parameters,
            string category)
        {
            var result = new List<BaseImageModel>();
            if (string.IsNullOrWhiteSpace(pathTemplate))
                return result;

            string path = TemplateInterpolation.Apply(pathTemplate, parameters);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return result;

            foreach (var file in Directory.EnumerateFiles(path)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f))
            {
                result.Add(new BaseImageModel
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    Category = category
                });
            }

            return result;
        }

        private async Task RebuildFilterAndMatchAsync()
        {
            int generation = ++_loadGeneration;
            ThumbnailLoadQueue.CancelPending();
            BeginLoading("Filtering & matching...", 1, indeterminate: true);

            var parameters = new Dictionary<string, string>(TemplateParams(Settings), StringComparer.OrdinalIgnoreCase);
            await ApplyFilterMatchToUiAsync(generation, _baseFilter, _regexPattern, parameters, IsFolderMode)
                .ConfigureAwait(true);
            EndLoading(generation);
        }

        private async Task ApplyFilterMatchToUiAsync(
            int generation,
            string filterTemplate,
            string regexTemplate,
            Dictionary<string, string> parameters,
            bool folderMode)
        {
            if (!IsCurrent(generation)) return;

            // Detach from UI before background mutation of shared model instances.
            BaseImages.ReplaceAll(Array.Empty<BaseImageModel>());
            ComparisonImages.ReplaceAll(Array.Empty<ComparisonImageModel>());
            if (folderMode)
            {
                foreach (var group in Categories)
                {
                    group.BaseImages.ReplaceAll(Array.Empty<BaseImageModel>());
                    group.RaiseDisplayChanged();
                }
            }

            string folderKey = TemplateInterpolation.TryParseFolderMode(parameters, out var key, out _, FolderModeTemplates()) ? key : null;
            var allBases = _allBaseImages;
            var allComps = _allComparisonImages;
            var categoryNames = folderMode
                ? Categories.Select(c => c.Name).ToList()
                : new List<string> { null };

            var work = await Task.Run(() =>
            {
                var filteredBases = new List<BaseImageModel>();
                var filteredComps = new List<ComparisonImageModel>();
                var perCategory = new Dictionary<string, (List<BaseImageModel> Bases, List<ComparisonImageModel> Comps)>(StringComparer.OrdinalIgnoreCase);

                foreach (var cat in categoryNames)
                {
                    var catParams = cat == null
                        ? (IReadOnlyDictionary<string, string>)parameters
                        : TemplateInterpolation.WithCategory(parameters, folderKey, cat);

                    string filter = TemplateInterpolation.Apply(filterTemplate ?? "", catParams);
                    string pattern = TemplateInterpolation.Apply(regexTemplate ?? "", catParams);

                    var catBases = allBases.Where(b =>
                        cat == null
                            ? string.IsNullOrEmpty(b.Category)
                            : string.Equals(b.Category, cat, StringComparison.OrdinalIgnoreCase)).ToList();
                    var catComps = allComps.Where(c =>
                        cat == null
                            ? string.IsNullOrEmpty(c.Category)
                            : string.Equals(c.Category, cat, StringComparison.OrdinalIgnoreCase)).ToList();

                    List<BaseImageModel> bases;
                    if (string.IsNullOrWhiteSpace(filter))
                        bases = catBases;
                    else
                    {
                        try
                        {
                            var fr = new Regex(filter, RegexOptions.IgnoreCase);
                            bases = catBases.Where(i => fr.IsMatch(i.FileName)).ToList();
                        }
                        catch { bases = new List<BaseImageModel>(); }
                    }

                    foreach (var b in bases)
                    {
                        b.MatchedComparisons.Clear();
                        b.MatchGroup = null;
                        b.HighlightColor = Colors.Transparent;
                    }
                    foreach (var c in catComps)
                    {
                        c.MatchGroup = null;
                        c.HighlightColor = Colors.Transparent;
                    }

                    if (!string.IsNullOrWhiteSpace(pattern))
                    {
                        try
                        {
                            var regex = new Regex(pattern);
                            int colorStart = 0;
                            MatchWithRegex(bases, catComps, regex, ref colorStart);
                        }
                        catch { /* invalid regex */ }
                    }

                    filteredBases.AddRange(bases);
                    filteredComps.AddRange(catComps);
                    if (cat != null)
                        perCategory[cat] = (bases, catComps);
                }

                return (filteredBases, filteredComps, perCategory);
            }).ConfigureAwait(true);

            if (!IsCurrent(generation) || folderMode != IsFolderMode) return;

            if (folderMode)
            {
                foreach (var group in Categories)
                {
                    if (work.perCategory.TryGetValue(group.Name, out var data))
                    {
                        group.BaseImages.ReplaceAll(data.Bases);
                        group.ComparisonImages.ReplaceAll(data.Comps);
                        group.BaseCount = data.Bases.Count;
                        group.ComparisonCount = data.Comps.Count;
                        group.MatchCount = data.Bases.Count(b => b.MatchedComparisons.Count > 0);
                        if (group.IsExpanded)
                        {
                            group.RaiseDisplayChanged();
                        }
                    }
                    else
                    {
                        group.BaseImages.ReplaceAll(Array.Empty<BaseImageModel>());
                        group.ComparisonImages.ReplaceAll(Array.Empty<ComparisonImageModel>());
                        group.BaseCount = 0;
                        group.ComparisonCount = 0;
                        group.MatchCount = 0;
                    }
                }

                BaseImages.ReplaceAll(work.filteredBases);
                ComparisonImages.ReplaceAll(work.filteredComps);
            }
            else
            {
                BaseImages.ReplaceAll(work.filteredBases);
                ComparisonImages.ReplaceAll(work.filteredComps);
            }

            BaseImageCount = work.filteredBases.Count;
            ComparisonImageCount = work.filteredComps.Count;

            await UpdateCompletionAsync(generation).ConfigureAwait(true);
        }

        private void MatchWithRegex(
            IList<BaseImageModel> baseList,
            IList<ComparisonImageModel> compList,
            Regex regex,
            ref int colorStart)
        {
            var baseGroups = new Dictionary<string, List<BaseImageModel>>();
            foreach (var b in baseList)
            {
                var m = regex.Match(b.FileName);
                if (m.Success && m.Groups.Count > 1)
                {
                    b.MatchGroup = m.Groups[1].Value;
                    if (!baseGroups.TryGetValue(b.MatchGroup, out var list))
                    {
                        list = new List<BaseImageModel>();
                        baseGroups[b.MatchGroup] = list;
                    }
                    list.Add(b);
                }
            }

            var compsByGroup = new Dictionary<string, List<ComparisonImageModel>>();
            foreach (var c in compList)
            {
                var m = regex.Match(c.FileName);
                if (m.Success && m.Groups.Count > 1)
                {
                    c.MatchGroup = m.Groups[1].Value;
                    if (!compsByGroup.TryGetValue(c.MatchGroup, out var list))
                    {
                        list = new List<ComparisonImageModel>();
                        compsByGroup[c.MatchGroup] = list;
                    }
                    list.Add(c);
                }
            }

            foreach (var key in baseGroups.Keys.OrderBy(k => k))
            {
                var color = MatchColors[colorStart % MatchColors.Length];
                colorStart++;

                compsByGroup.TryGetValue(key, out var matchedComps);
                matchedComps ??= new List<ComparisonImageModel>();

                foreach (var c in matchedComps)
                    c.HighlightColor = color;

                foreach (var b in baseGroups[key])
                {
                    b.HighlightColor = color;
                    foreach (var c in matchedComps)
                        b.MatchedComparisons.Add(c);
                }
            }
        }

        private async Task UpdateCompletionAsync(int? expectedGeneration = null)
        {
            int generation = expectedGeneration ?? _loadGeneration;
            bool showProgress = expectedGeneration == null;
            if (showProgress)
                BeginLoading("Updating selection...", 1, indeterminate: true);

            bool folderMode = IsFolderMode;
            var enabledNames = folderMode
                ? Categories.Where(c => c.IsEnabled).Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : null;

            var basesSnap = BaseImages.ToList();
            var compsSnap = ComparisonImages.ToList();

            var result = await Task.Run(() =>
            {
                IEnumerable<BaseImageModel> eligibleBases = basesSnap;
                IEnumerable<ComparisonImageModel> eligibleComps = compsSnap;

                if (folderMode)
                {
                    eligibleBases = basesSnap.Where(b => enabledNames.Contains(b.Category ?? ""));
                    eligibleComps = compsSnap.Where(c => enabledNames.Contains(c.Category ?? ""));
                }

                var sessionBases = eligibleBases.Where(b => b.MatchedComparisons.Count > 0).ToList();
                var sessionComps = eligibleComps.Where(c => !string.IsNullOrEmpty(c.MatchGroup)).ToList();
                return (sessionBases, sessionComps, hasMatches: sessionBases.Count > 0);
            }).ConfigureAwait(true);

            if (!IsCurrent(generation) || folderMode != IsFolderMode)
            {
                if (showProgress)
                    EndLoading(generation);
                return;
            }

            IsCompleted = result.hasMatches;
            Session.BaseImages.ReplaceAll(result.sessionBases);
            Session.ComparisonImages.ReplaceAll(result.sessionComps);

            if (showProgress)
                EndLoading(generation);
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
