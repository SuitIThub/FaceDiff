using System.Drawing;

namespace FaceDiff.Models
{
    public class FaceDetectionResult
    {
        public Rectangle FaceRect { get; set; }
        public double Confidence { get; set; }
        public string DetectorName { get; set; }
        public OvalRegion Oval { get; set; }

        public double Score => Confidence * FaceRect.Width * FaceRect.Height;
    }
}
