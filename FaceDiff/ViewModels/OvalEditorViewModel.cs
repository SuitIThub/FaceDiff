using FaceDiff.Core;
using FaceDiff.Models;

namespace FaceDiff.ViewModels
{
    public class OvalEditorViewModel : ViewModelBase
    {
        private string _imagePath;
        private OvalRegion _oval;
        private bool _isZoomed;
        private double _imageWidth;
        private double _imageHeight;

        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        public OvalRegion Oval
        {
            get => _oval;
            set => SetProperty(ref _oval, value);
        }

        public bool IsZoomed
        {
            get => _isZoomed;
            set => SetProperty(ref _isZoomed, value);
        }

        public double ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }

        public double ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }

        public void UpdateOvalCenter(double cx, double cy)
        {
            if (Oval == null) Oval = new OvalRegion();
            Oval.CenterX = cx;
            Oval.CenterY = cy;
            OnPropertyChanged(nameof(Oval));
        }

        public void UpdateOvalRadii(double rx, double ry)
        {
            if (Oval == null) Oval = new OvalRegion();
            Oval.RadiusX = rx;
            Oval.RadiusY = ry;
            OnPropertyChanged(nameof(Oval));
        }
    }
}
