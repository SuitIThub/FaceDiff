using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    public class DlibHogFaceDetector : IFaceDetector
    {
        public string Name => "Dlib HOG";

        public bool IsAvailable
        {
            get
            {
                try
                {
                    var type = Type.GetType("DlibDotNet.Dlib, DlibDotNet");
                    return type != null;
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
                    using (var detector = DlibDotNet.Dlib.GetFrontalFaceDetector())
                    using (var img = DlibDotNet.Dlib.LoadImage<DlibDotNet.RgbPixel>(imagePath))
                    {
                        var faces = detector.Operator(img);
                        foreach (var face in faces)
                        {
                            var rect = new Rectangle(
                                face.Left, face.Top,
                                (int)face.Width, (int)face.Height);

                            var oval = new OvalRegion
                            {
                                CenterX = rect.X + rect.Width / 2.0,
                                CenterY = rect.Y + rect.Height / 2.0,
                                RadiusX = rect.Width / 2.0,
                                RadiusY = rect.Height / 2.0 * 1.2
                            };

                            results.Add(new FaceDetectionResult
                            {
                                FaceRect = rect,
                                Confidence = 0.85,
                                DetectorName = Name,
                                Oval = oval
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DlibHogFaceDetector] Error: {ex.Message}");
                }
                return results;
            });
        }
    }
}
