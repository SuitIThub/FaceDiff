using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FaceDiff.Core;
using FaceDiff.Models;
using FaceDiff.Services;

namespace FaceDiff.ViewModels
{
    public class DiffGenerationViewModel : StepViewModel
    {
        private readonly ImageDiffService _diffService = new ImageDiffService();
        private string _destinationPath;
        private bool _isProcessing;
        private bool _isWaitingForDecision;
        private int _currentBaseIndex;
        private BaseImageModel _currentBaseImage;
        private BitmapImage _currentBaseDisplay;
        private double _threshold = 10;
        private string _statusText;
        private string _destinationError;
        private bool _areButtonsActivated;

        public DiffGenerationViewModel()
        {
            DiffResults = new ObservableCollection<DiffResultItem>();
            BrowseDestinationCommand = new RelayCommand(BrowseDestination);
            StartCommand = new RelayCommand(async () => await StartProcessingAsync(),
                () => !_isProcessing && !string.IsNullOrEmpty(_destinationPath) && string.IsNullOrEmpty(_destinationError));
            AcceptCommand = new RelayCommand(OnAccept, () => _isWaitingForDecision);
            DenyCommand = new RelayCommand(OnDeny, () => _isWaitingForDecision);
        }

        public ObservableCollection<DiffResultItem> DiffResults { get; }

        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                if (SetProperty(ref _destinationPath, value))
                {
                    if (Settings != null) Settings.DestinationPath = value;
                    Session.DestinationPath = value;
                    ValidateDestination();
                }
            }
        }

        public string DestinationError
        {
            get => _destinationError;
            set
            {
                if (SetProperty(ref _destinationError, value))
                    OnPropertyChanged(nameof(HasDestinationError));
            }
        }

        public bool HasDestinationError => !string.IsNullOrEmpty(_destinationError);

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public bool IsWaitingForDecision
        {
            get => _isWaitingForDecision;
            set
            {
                if (SetProperty(ref _isWaitingForDecision, value) && value)
                    AreButtonsActivated = false;
            }
        }

        public bool AreButtonsActivated
        {
            get => _areButtonsActivated;
            set => SetProperty(ref _areButtonsActivated, value);
        }

        public BaseImageModel CurrentBaseImage
        {
            get => _currentBaseImage;
            set => SetProperty(ref _currentBaseImage, value);
        }

        public BitmapImage CurrentBaseDisplay
        {
            get => _currentBaseDisplay;
            set => SetProperty(ref _currentBaseDisplay, value);
        }

        public double Threshold
        {
            get => _threshold;
            set
            {
                if (SetProperty(ref _threshold, value) && Settings != null)
                    Settings.Threshold = value;
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ICommand BrowseDestinationCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand DenyCommand { get; }

        private TaskCompletionSource<bool> _decisionTcs;

        public override void OnNavigatedTo()
        {
            _currentBaseIndex = 0;
            Session.Results.Clear();

            if (Settings != null)
            {
                if (string.IsNullOrEmpty(_destinationPath) && !string.IsNullOrEmpty(Settings.DestinationPath))
                    DestinationPath = Settings.DestinationPath;
                if (Math.Abs(_threshold - Settings.Threshold) > 0.01)
                {
                    _threshold = Settings.Threshold;
                    OnPropertyChanged(nameof(Threshold));
                }
            }
        }

        private async Task StartProcessingAsync()
        {
            IsProcessing = true;

            for (_currentBaseIndex = 0; _currentBaseIndex < Session.BaseImages.Count; _currentBaseIndex++)
            {
                var baseImg = Session.BaseImages[_currentBaseIndex];
                CurrentBaseImage = baseImg;
                StatusText = $"Processing {baseImg.FileName} ({_currentBaseIndex + 1}/{Session.BaseImages.Count})";

                CurrentBaseDisplay = await ThumbnailService.CreateThumbnailAsync(baseImg.FilePath, 1200);
                DiffResults.Clear();

                string subDir = Path.Combine("_temp", Path.GetFileNameWithoutExtension(baseImg.FileName));
                string tempDir = Path.Combine(_destinationPath, subDir);

                var result = new ProcessResult
                {
                    BaseImage = baseImg,
                    ComparisonCount = baseImg.MatchedComparisons.Count
                };

                foreach (var comp in baseImg.MatchedComparisons)
                {
                    string outName = Path.GetFileName(comp.FilePath);

                    try
                    {
                        string diffPath = await _diffService.GenerateDiffAsync(
                            baseImg.FilePath, comp.FilePath,
                            baseImg.Oval, _threshold,
                            tempDir, outName);

                        var thumb = await ThumbnailService.CreateThumbnailAsync(diffPath);
                        DiffResults.Add(new DiffResultItem
                        {
                            FilePath = diffPath,
                            ComparisonFileName = comp.FileName,
                            Thumbnail = thumb
                        });

                        result.GeneratedDiffPaths.Add(diffPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Diff error: {ex.Message}");
                    }
                }

                _decisionTcs = new TaskCompletionSource<bool>();
                IsWaitingForDecision = true;
                bool accepted = await _decisionTcs.Task;
                IsWaitingForDecision = false;

                result.Accepted = accepted;

                if (accepted)
                {
                    Directory.CreateDirectory(_destinationPath);

                    foreach (var diffPath in result.GeneratedDiffPaths)
                    {
                        string dest = Path.Combine(_destinationPath, Path.GetFileName(diffPath));
                        if (File.Exists(dest))
                            File.Delete(dest);
                        if (File.Exists(diffPath))
                            File.Move(diffPath, dest);
                    }
                }

                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }

                Session.Results.Add(result);
            }

            string tempRoot = Path.Combine(_destinationPath, "_temp");
            if (Directory.Exists(tempRoot))
            {
                try { Directory.Delete(tempRoot, true); } catch { }
            }

            IsProcessing = false;
            IsCompleted = true;
            StatusText = "All images processed.";
        }

        private void OnAccept()
        {
            _decisionTcs?.TrySetResult(true);
        }

        private void OnDeny()
        {
            _decisionTcs?.TrySetResult(false);
        }

        private void BrowseDestination()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Destination Folder";
                if (!string.IsNullOrEmpty(_destinationPath))
                    dialog.SelectedPath = _destinationPath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    DestinationPath = dialog.SelectedPath;
            }
        }

        private void ValidateDestination()
        {
            DestinationError = null;

            if (string.IsNullOrEmpty(_destinationPath) || string.IsNullOrEmpty(Session.ComparisonFolderPath))
                return;

            string dest = Path.GetFullPath(_destinationPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string comp = Path.GetFullPath(Session.ComparisonFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(dest, comp, StringComparison.OrdinalIgnoreCase))
                DestinationError = "Destination folder must be different from the comparison images folder.";
        }
    }

    public class DiffResultItem : ViewModelBase
    {
        private string _filePath;
        private string _comparisonFileName;
        private BitmapImage _thumbnail;

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string ComparisonFileName
        {
            get => _comparisonFileName;
            set => SetProperty(ref _comparisonFileName, value);
        }

        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }
    }
}
