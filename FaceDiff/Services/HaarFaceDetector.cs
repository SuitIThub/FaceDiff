using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    public class HaarFaceDetector : IFaceDetector
    {
        public string Name => "Haar Cascade";

        public bool IsAvailable
        {
            get
            {
                try
                {
                    return System.IO.File.Exists(ModelDownloadService.HaarCascadePath);
                }
                catch { return false; }
            }
        }

        public Task<List<FaceDetectionResult>> DetectFacesAsync(string imagePath)
        {
            return Task.Run(() =>
            {
                var results = new List<FaceDetectionResult>();
                try
                {
                    using (var cascade = new Emgu.CV.CascadeClassifier(ModelDownloadService.HaarCascadePath))
                    using (var img = new Emgu.CV.Mat(imagePath))
                    using (var gray = new Emgu.CV.Mat())
                    {
                        Emgu.CV.CvInvoke.CvtColor(img, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                        Emgu.CV.CvInvoke.EqualizeHist(gray, gray);

                        var faces = cascade.DetectMultiScale(
                            gray,
                            scaleFactor: 1.1,
                            minNeighbors: 5,
                            minSize: new Size(30, 30));

                        foreach (var face in faces)
                        {
                            var oval = new OvalRegion
                            {
                                CenterX = face.X + face.Width / 2.0,
                                CenterY = face.Y + face.Height / 2.0,
                                RadiusX = face.Width / 2.0,
                                RadiusY = face.Height / 2.0 * 1.2
                            };

                            results.Add(new FaceDetectionResult
                            {
                                FaceRect = face,
                                Confidence = 0.7,
                                DetectorName = Name,
                                Oval = oval
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HaarFaceDetector] Error: {ex.Message}");
                }
                return results;
            });
        }
    }
}
