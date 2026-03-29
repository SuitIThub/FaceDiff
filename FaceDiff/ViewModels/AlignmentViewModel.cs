using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FaceDiff.Core;
using FaceDiff.Models;
using FaceDiff.Services;

namespace FaceDiff.ViewModels
{
    public class AlignmentViewModel : StepViewModel
    {
        /// <summary>
        /// Maps preview vertical shift to the same numbers as Ren'Py layer overrides for <c>ypos</c>.
        /// Without this, aligning visually produced ~2× the override (e.g. -0.04 vs -0.02) for the same look.
        /// </summary>
        private const double YOverridePreviewScale = 2.0;

        private BaseImageModel _selectedBaseImage;
        private string _selectedOutfitPath;
        private string _selectedDiffPath;
        private double _alignX;
        private double _alignY;
        private BitmapImage _outfitDisplay;
        private BitmapImage _diffDisplay;
        private double _previewWidth = 400;
        private double _previewHeight = 600;
        private double _renderedWidth;
        private double _renderedHeight;
        private double _diffOffsetXPixels;
        private double _diffOffsetYPixels;
        private bool _isXAxisActive;
        private double _zoomLevel = 1.0;
        private string _destinationPath;
        private bool _showDestinationField;
        private int _viewportWidth = 1920;
        private int _viewportHeight = 1080;
        private string _debugInfo = "";
        private double _outfitImageLeft;
        private double _diffImageLeft;
        private double _diffImageTop;

        public AlignmentViewModel()
        {
            ProcessedBaseImages = new ObservableCollection<BaseImageModel>();
            OutfitImages = new ObservableCollection<string>();
            DiffImages = new ObservableCollection<string>();
            ZoomInCommand = new RelayCommand(() => ZoomLevel = Math.Min(_zoomLevel * 1.5, 10.0));
            ZoomOutCommand = new RelayCommand(() => ZoomLevel = Math.Max(_zoomLevel / 1.5, 0.25));
            ZoomFitCommand = new RelayCommand(() => ZoomLevel = 1.0);
            BrowseDestinationCommand = new RelayCommand(BrowseDestination);
            UseImageHeightAsViewportHeightCommand = new RelayCommand(
                UseImageHeightAsViewportHeight,
                () => _outfitDisplay != null);
        }

        public ObservableCollection<BaseImageModel> ProcessedBaseImages { get; }
        public ObservableCollection<string> OutfitImages { get; }
        public ObservableCollection<string> DiffImages { get; }

        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ZoomFitCommand { get; }
        public ICommand BrowseDestinationCommand { get; }
        public ICommand UseImageHeightAsViewportHeightCommand { get; }

        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                if (SetProperty(ref _destinationPath, value))
                {
                    if (Settings != null) Settings.DestinationPath = value;
                    Session.DestinationPath = value;
                    if (_selectedBaseImage != null)
                        LoadDiffImages();
                }
            }
        }

        public bool ShowDestinationField
        {
            get => _showDestinationField;
            set => SetProperty(ref _showDestinationField, value);
        }

        public BaseImageModel SelectedBaseImage
        {
            get => _selectedBaseImage;
            set
            {
                if (!SetProperty(ref _selectedBaseImage, value))
                    return;

                string prevOutfitFile = string.IsNullOrEmpty(_selectedOutfitPath)
                    ? null
                    : Path.GetFileName(_selectedOutfitPath);
                string prevDiffFile = string.IsNullOrEmpty(_selectedDiffPath)
                    ? null
                    : Path.GetFileName(_selectedDiffPath);

                LoadOutfitImages();
                LoadDiffImages();
                ClearPreviewSelections();

                if (prevOutfitFile != null)
                {
                    string match = OutfitImages.FirstOrDefault(p =>
                        string.Equals(Path.GetFileName(p), prevOutfitFile, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        SelectedOutfitPath = match;
                }

                if (prevDiffFile != null)
                {
                    string match = DiffImages.FirstOrDefault(p =>
                        string.Equals(Path.GetFileName(p), prevDiffFile, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        SelectedDiffPath = match;
                }
            }
        }

        public string SelectedOutfitPath
        {
            get => _selectedOutfitPath;
            set
            {
                if (SetProperty(ref _selectedOutfitPath, value))
                {
                    OnPropertyChanged(nameof(SelectedOutfitFileName));
                    LoadOutfitDisplay();
                }
            }
        }

        public string SelectedOutfitFileName => string.IsNullOrEmpty(_selectedOutfitPath)
            ? null : Path.GetFileName(_selectedOutfitPath);

        public string SelectedDiffPath
        {
            get => _selectedDiffPath;
            set
            {
                if (SetProperty(ref _selectedDiffPath, value))
                {
                    OnPropertyChanged(nameof(SelectedDiffFileName));
                    LoadDiffDisplay();
                }
            }
        }

        public string SelectedDiffFileName => string.IsNullOrEmpty(_selectedDiffPath)
            ? null : Path.GetFileName(_selectedDiffPath);

        public double AlignX
        {
            get => _alignX;
            set
            {
                if (SetProperty(ref _alignX, value))
                    UpdateDiffOffset();
            }
        }

        public double AlignY
        {
            get => _alignY;
            set
            {
                if (SetProperty(ref _alignY, value))
                    UpdateDiffOffset();
            }
        }

        public bool IsXAxisActive
        {
            get => _isXAxisActive;
            set
            {
                if (SetProperty(ref _isXAxisActive, value))
                    OnPropertyChanged(nameof(ActiveAxisLabel));
            }
        }

        public string ActiveAxisLabel => _isXAxisActive ? "X" : "Y";

        public BitmapImage OutfitDisplay
        {
            get => _outfitDisplay;
            set => SetProperty(ref _outfitDisplay, value);
        }

        public BitmapImage DiffDisplay
        {
            get => _diffDisplay;
            set => SetProperty(ref _diffDisplay, value);
        }

        public double PreviewWidth
        {
            get => _previewWidth;
            set
            {
                if (SetProperty(ref _previewWidth, value))
                    UpdateDiffOffset();
            }
        }

        public double PreviewHeight
        {
            get => _previewHeight;
            set
            {
                if (SetProperty(ref _previewHeight, value))
                    UpdateDiffOffset();
            }
        }

        public double DiffOffsetXPixels
        {
            get => _diffOffsetXPixels;
            set => SetProperty(ref _diffOffsetXPixels, value);
        }

        public double DiffOffsetYPixels
        {
            get => _diffOffsetYPixels;
            set => SetProperty(ref _diffOffsetYPixels, value);
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set => SetProperty(ref _zoomLevel, value);
        }

        public int ViewportWidth
        {
            get => _viewportWidth;
            set
            {
                if (SetProperty(ref _viewportWidth, value))
                {
                    if (Settings != null) Settings.ViewportWidth = value;
                    UpdateDiffOffset();
                }
            }
        }

        public int ViewportHeight
        {
            get => _viewportHeight;
            set
            {
                if (SetProperty(ref _viewportHeight, value))
                {
                    if (Settings != null) Settings.ViewportHeight = value;
                    UpdateDiffOffset();
                }
            }
        }

        public string DebugInfo
        {
            get => _debugInfo;
            set => SetProperty(ref _debugInfo, value);
        }

        /// <summary>Uniform-fit width/height in DIPs (same for outfit and diff).</summary>
        public double RenderedImageWidth => _renderedWidth;

        public double RenderedImageHeight => _renderedHeight;

        public double OutfitImageLeft
        {
            get => _outfitImageLeft;
            private set => SetProperty(ref _outfitImageLeft, value);
        }

        public double DiffImageLeft
        {
            get => _diffImageLeft;
            private set => SetProperty(ref _diffImageLeft, value);
        }

        public double DiffImageTop
        {
            get => _diffImageTop;
            private set => SetProperty(ref _diffImageTop, value);
        }

        public bool HasPreview => _outfitDisplay != null && _diffDisplay != null;

        /// <summary>Wheel alignment: Ctrl = 1/1e6, Shift = 1/1e4, Alt = 1/100 (plain wheel = zoom in view).</summary>
        public void ScrollActiveAxis(int direction, bool shift, bool ctrl, bool alt)
        {
            double step;
            if (ctrl)
                step = 1.0 / 1000000.0;
            else if (shift)
                step = 1.0 / 10000.0;
            else if (alt)
                step = 1.0 / 100.0;
            else
                step = 1.0 / 100.0;

            double delta = direction > 0 ? step : -step;

            if (_isXAxisActive)
                AlignX += delta;
            else
                AlignY += delta;
        }

        public override void OnNavigatedTo()
        {
            ProcessedBaseImages.Clear();

            foreach (var b in Session.BaseImages)
                ProcessedBaseImages.Add(b);

            if (Settings != null)
            {
                if (!string.IsNullOrEmpty(Settings.DestinationPath))
                    _destinationPath = Settings.DestinationPath;
                if (Settings.ViewportWidth > 0)
                    _viewportWidth = Settings.ViewportWidth;
                if (Settings.ViewportHeight > 0)
                    _viewportHeight = Settings.ViewportHeight;
            }
            OnPropertyChanged(nameof(DestinationPath));
            OnPropertyChanged(nameof(ViewportWidth));
            OnPropertyChanged(nameof(ViewportHeight));
        }

        private void BrowseDestination()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Destination Folder (Diff Images)";
                if (!string.IsNullOrEmpty(_destinationPath))
                    dialog.SelectedPath = _destinationPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    DestinationPath = dialog.SelectedPath;
            }
        }

        private void UseImageHeightAsViewportHeight()
        {
            if (_outfitDisplay == null) return;
            ViewportHeight = _outfitDisplay.PixelHeight;
        }

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif" };

        private void LoadOutfitImages()
        {
            OutfitImages.Clear();
            if (_selectedBaseImage == null || Settings == null) return;

            string baseFolderPath = Settings.BaseFolderPath;
            string regexPattern = Settings.RegexPattern;

            if (string.IsNullOrEmpty(baseFolderPath) || !Directory.Exists(baseFolderPath))
                return;
            if (string.IsNullOrEmpty(regexPattern))
                return;

            Regex regex;
            try { regex = new Regex(regexPattern); }
            catch { return; }

            var baseMatch = regex.Match(_selectedBaseImage.FileName);
            if (!baseMatch.Success || baseMatch.Groups.Count <= 1)
                return;

            string targetGroup = baseMatch.Groups[1].Value;

            var allFiles = Directory.GetFiles(baseFolderPath)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f);

            foreach (var file in allFiles)
            {
                var m = regex.Match(Path.GetFileName(file));
                if (m.Success && m.Groups.Count > 1 && m.Groups[1].Value == targetGroup)
                    OutfitImages.Add(file);
            }
        }

        private void LoadDiffImages()
        {
            DiffImages.Clear();
            if (_selectedBaseImage == null) return;

            string destPath = _destinationPath;
            if (string.IsNullOrEmpty(destPath))
                destPath = Settings?.DestinationPath ?? Session.DestinationPath;
            if (string.IsNullOrEmpty(destPath) || !Directory.Exists(destPath))
                return;

            string regexPattern = Settings?.RegexPattern;
            Regex regex = null;
            string targetGroup = null;

            if (!string.IsNullOrEmpty(regexPattern))
            {
                try { regex = new Regex(regexPattern); }
                catch { regex = null; }

                if (regex != null)
                {
                    var baseMatch = regex.Match(_selectedBaseImage.FileName);
                    if (baseMatch.Success && baseMatch.Groups.Count > 1)
                        targetGroup = baseMatch.Groups[1].Value;
                }
            }

            var files = Directory.GetFiles(destPath)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f);

            foreach (var file in files)
            {
                if (regex != null && targetGroup != null)
                {
                    var m = regex.Match(Path.GetFileName(file));
                    if (!m.Success || m.Groups.Count <= 1 || m.Groups[1].Value != targetGroup)
                        continue;
                }
                DiffImages.Add(file);
            }
        }

        private void LoadOutfitDisplay()
        {
            if (string.IsNullOrEmpty(_selectedOutfitPath) || !File.Exists(_selectedOutfitPath))
            {
                OutfitDisplay = null;
                OnPropertyChanged(nameof(HasPreview));
                UpdateDiffOffset();
                CommandManager.InvalidateRequerySuggested();
                return;
            }

            OutfitDisplay = LoadBitmap(_selectedOutfitPath);
            OnPropertyChanged(nameof(HasPreview));
            UpdateDiffOffset();
            CommandManager.InvalidateRequerySuggested();
        }

        private void LoadDiffDisplay()
        {
            if (string.IsNullOrEmpty(_selectedDiffPath) || !File.Exists(_selectedDiffPath))
            {
                DiffDisplay = null;
                OnPropertyChanged(nameof(HasPreview));
                UpdateDiffOffset();
                return;
            }

            DiffDisplay = LoadBitmap(_selectedDiffPath);
            OnPropertyChanged(nameof(HasPreview));
            UpdateDiffOffset();
        }

        /// <summary>Clears outfit/diff selection and bitmaps. AlignX/Y, zoom, and viewport are kept when switching base image group.</summary>
        private void ClearPreviewSelections()
        {
            SelectedOutfitPath = null;
            SelectedDiffPath = null;
            OutfitDisplay = null;
            DiffDisplay = null;
            DiffOffsetXPixels = 0;
            DiffOffsetYPixels = 0;
            OnPropertyChanged(nameof(HasPreview));
        }

        private void UpdateRenderedDimensions()
        {
            if (_outfitDisplay == null)
            {
                _renderedWidth = _previewWidth;
                _renderedHeight = _previewHeight;
                return;
            }

            double imgW = _outfitDisplay.Width;
            double imgH = _outfitDisplay.Height;
            double scale = Math.Min(_previewWidth / imgW, _previewHeight / imgH);
            _renderedWidth = imgW * scale;
            _renderedHeight = imgH * scale;
        }

        private void UpdateDiffOffset()
        {
            UpdateRenderedDimensions();

            if (_outfitDisplay != null)
            {
                double imgW_px = _outfitDisplay.PixelWidth;
                double imgH_px = _outfitDisplay.PixelHeight;
                double imgW_dip = _outfitDisplay.Width;
                double imgH_dip = _outfitDisplay.Height;

                double dipPerPixel = _renderedHeight / imgH_px;

                // Layer override for ypos: Ren'Py applies as fraction of screen height; preview needs YOverridePreviewScale.
                DiffOffsetYPixels = _alignY * _viewportHeight * dipPerPixel * YOverridePreviewScale;
                DiffOffsetXPixels = _alignX * (_viewportWidth - imgW_px) * dipPerPixel;

                double renpyYpx = _alignY * _viewportHeight;
                string diffInfo = _diffDisplay != null
                    ? $"Diff: {_diffDisplay.PixelWidth}x{_diffDisplay.PixelHeight}px DPI:{_diffDisplay.DpiX:F0} | "
                    : "";
                DebugInfo = $"Outfit: {imgW_px}x{imgH_px}px DIP:{imgW_dip:F0}x{imgH_dip:F0} DPI:{_outfitDisplay.DpiX:F0} | " +
                            diffInfo +
                            $"Container: {_previewWidth:F0}x{_previewHeight:F0} | " +
                            $"Rendered: {_renderedWidth:F0}x{_renderedHeight:F0} | " +
                            $"dip/px: {dipPerPixel:F6} | Ren'Py ypos px≈{renpyYpx:F1} (alignY×viewportH) | y preview×{YOverridePreviewScale}";

                UpdateCanvasPlacements();
            }
            else
            {
                DiffOffsetYPixels = _alignY * _previewHeight * YOverridePreviewScale;
                DiffOffsetXPixels = _alignX * _previewWidth;
                DebugInfo = "";
                ResetCanvasPlacements();
            }
        }

        /// <summary>Sync Canvas.Left/Top from the same offsets used for Ren'Py (avoids Uniform+TranslateTransform Y mismatch).</summary>
        private void UpdateCanvasPlacements()
        {
            OnPropertyChanged(nameof(RenderedImageWidth));
            OnPropertyChanged(nameof(RenderedImageHeight));
            OutfitImageLeft = (_previewWidth - _renderedWidth) / 2.0;
            DiffImageLeft = OutfitImageLeft + _diffOffsetXPixels;
            DiffImageTop = _diffOffsetYPixels;
        }

        private void ResetCanvasPlacements()
        {
            OnPropertyChanged(nameof(RenderedImageWidth));
            OnPropertyChanged(nameof(RenderedImageHeight));
            OutfitImageLeft = 0;
            DiffImageLeft = 0;
            DiffImageTop = 0;
        }

        private static BitmapImage LoadBitmap(string path)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch
            {
                return null;
            }
        }
    }
}
