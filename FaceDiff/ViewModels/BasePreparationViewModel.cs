using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using FaceDiff.Core;
using FaceDiff.Models;
using FaceDiff.Services;

namespace FaceDiff.ViewModels
{
    public class BasePreparationViewModel : StepViewModel
    {
        private readonly CompositeFaceDetector _detector = new CompositeFaceDetector();
        private bool _isDetecting;
        private bool _isEditingOval;
        private BaseImageModel _editingImage;
        private OvalEditorViewModel _ovalEditor;
        private string _statusText;
        private int _detectionProgress;
        private int _detectionTotal;
        private double _ovalScale = 1.3;

        public BasePreparationViewModel()
        {
            DisplayImages = new ObservableCollection<BaseImageModel>();
            RunDetectionCommand = new RelayCommand(async () => await RunDetectionAsync(), () => !_isDetecting);
            EditOvalCommand = new RelayCommand<object>(OnEditOval);
            ConfirmOvalCommand = new RelayCommand(OnConfirmOval);
            CancelOvalCommand = new RelayCommand(OnCancelOval);
        }

        public ObservableCollection<BaseImageModel> DisplayImages { get; }

        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetProperty(ref _isDetecting, value);
        }

        public bool IsEditingOval
        {
            get => _isEditingOval;
            set => SetProperty(ref _isEditingOval, value);
        }

        public BaseImageModel EditingImage
        {
            get => _editingImage;
            set => SetProperty(ref _editingImage, value);
        }

        public OvalEditorViewModel OvalEditor
        {
            get => _ovalEditor;
            set => SetProperty(ref _ovalEditor, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public int DetectionProgress
        {
            get => _detectionProgress;
            set => SetProperty(ref _detectionProgress, value);
        }

        public int DetectionTotal
        {
            get => _detectionTotal;
            set => SetProperty(ref _detectionTotal, value);
        }

        public double OvalScale
        {
            get => _ovalScale;
            set
            {
                if (SetProperty(ref _ovalScale, value))
                {
                    if (Settings != null) Settings.OvalScale = value;
                    _ = ReapplyOvalScaleAsync();
                }
            }
        }

        public ICommand RunDetectionCommand { get; }
        public ICommand EditOvalCommand { get; }
        public ICommand ConfirmOvalCommand { get; }
        public ICommand CancelOvalCommand { get; }

        public override void OnNavigatedTo()
        {
            if (Settings != null && Math.Abs(_ovalScale - Settings.OvalScale) > 0.001)
            {
                _ovalScale = Settings.OvalScale;
                OnPropertyChanged(nameof(OvalScale));
            }

            DisplayImages.Clear();
            foreach (var img in Session.BaseImages)
                DisplayImages.Add(img);

            DetectionTotal = DisplayImages.Count;

            if (DisplayImages.Any(i => i.DetectionStatus == DetectionStatus.None))
                _ = RunDetectionAsync();
        }

        private async Task RunDetectionAsync()
        {
            IsDetecting = true;
            DetectionProgress = 0;

            foreach (var img in DisplayImages)
            {
                if (img.DetectionStatus == DetectionStatus.ManualOverride)
                {
                    DetectionProgress++;
                    continue;
                }

                StatusText = $"Detecting face in {img.FileName}...";
                var result = await _detector.DetectBestFaceAsync(img.FilePath);

                if (result != null)
                {
                    img.DetectedOval = result.Oval;
                    img.Oval = ScaleOval(result.Oval, _ovalScale);
                    img.DetectionStatus = DetectionStatus.AutoDetected;
                    img.FaceThumbnail = await ThumbnailService.CreateFaceThumbnailAsync(img.FilePath, img.Oval);
                }
                else
                {
                    img.DetectionStatus = DetectionStatus.NoFaceFound;
                    img.FaceThumbnail = img.Thumbnail;
                }

                DetectionProgress++;
            }

            StatusText = "Detection complete.";
            IsDetecting = false;
            CheckCompletion();
        }

        private void OnEditOval(object param)
        {
            if (!(param is BaseImageModel model)) return;

            EditingImage = model;
            OvalEditor = new OvalEditorViewModel
            {
                ImagePath = model.FilePath,
                Oval = model.Oval?.Clone() ?? new OvalRegion()
            };
            IsEditingOval = true;
        }

        private async void OnConfirmOval()
        {
            if (EditingImage == null || OvalEditor == null) return;

            EditingImage.Oval = OvalEditor.Oval;
            EditingImage.DetectionStatus = DetectionStatus.ManualOverride;
            EditingImage.FaceThumbnail = await ThumbnailService.CreateFaceThumbnailAsync(
                EditingImage.FilePath, OvalEditor.Oval);

            IsEditingOval = false;
            EditingImage = null;
            OvalEditor = null;
            CheckCompletion();
        }

        private void OnCancelOval()
        {
            IsEditingOval = false;
            EditingImage = null;
            OvalEditor = null;
        }

        private static OvalRegion ScaleOval(OvalRegion source, double scale)
        {
            return new OvalRegion
            {
                CenterX = source.CenterX,
                CenterY = source.CenterY,
                RadiusX = source.RadiusX * scale,
                RadiusY = source.RadiusY * scale,
                Angle = source.Angle
            };
        }

        private async Task ReapplyOvalScaleAsync()
        {
            foreach (var img in DisplayImages)
            {
                if (img.DetectionStatus != DetectionStatus.AutoDetected || img.DetectedOval == null)
                    continue;

                img.Oval = ScaleOval(img.DetectedOval, _ovalScale);
                img.FaceThumbnail = await ThumbnailService.CreateFaceThumbnailAsync(img.FilePath, img.Oval);
            }
        }

        private void CheckCompletion()
        {
            IsCompleted = DisplayImages.All(img => img.HasOval);
        }

        public void LoadDeniedImages()
        {
            var denied = Session.Results
                .Where(r => !r.Accepted)
                .Select(r => r.BaseImage)
                .ToList();

            Session.BaseImages.Clear();
            foreach (var img in denied)
            {
                img.DetectionStatus = DetectionStatus.None;
                img.Oval = null;
                img.FaceThumbnail = null;
                Session.BaseImages.Add(img);
            }
        }
    }
}
