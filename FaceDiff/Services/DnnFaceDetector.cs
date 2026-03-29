using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Dnn;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    public class DnnFaceDetector : IFaceDetector
    {
        public string Name => "DNN SSD";

        public bool IsAvailable
        {
            get
            {
                try
                {
                    return System.IO.File.Exists(ModelDownloadService.DnnProtoPath)
                        && System.IO.File.Exists(ModelDownloadService.DnnModelPath);
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
                    using (var net = DnnInvoke.ReadNetFromCaffe(
                        ModelDownloadService.DnnProtoPath,
                        ModelDownloadService.DnnModelPath))
                    using (var img = CvInvoke.Imread(imagePath))
                    {
                        int h = img.Rows;
                        int w = img.Cols;

                        using (var blob = DnnInvoke.BlobFromImage(
                            img, 1.0, new Size(300, 300),
                            new Emgu.CV.Structure.MCvScalar(104, 177, 123),
                            false, false))
                        {
                            net.SetInput(blob);
                            using (var detections = net.Forward())
                            {
                                var data = new float[detections.Total.ToInt32()];
                                System.Runtime.InteropServices.Marshal.Copy(detections.DataPointer, data, 0, data.Length);

                                int count = data.Length / 7;
                                for (int i = 0; i < count; i++)
                                {
                                    float confidence = data[i * 7 + 2];
                                    if (confidence < 0.5f) continue;

                                    int x1 = (int)(data[i * 7 + 3] * w);
                                    int y1 = (int)(data[i * 7 + 4] * h);
                                    int x2 = (int)(data[i * 7 + 5] * w);
                                    int y2 = (int)(data[i * 7 + 6] * h);

                                    x1 = Math.Max(0, x1);
                                    y1 = Math.Max(0, y1);
                                    x2 = Math.Min(w, x2);
                                    y2 = Math.Min(h, y2);

                                    var rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                                    var oval = new OvalRegion
                                    {
                                        CenterX = (x1 + x2) / 2.0,
                                        CenterY = (y1 + y2) / 2.0,
                                        RadiusX = (x2 - x1) / 2.0,
                                        RadiusY = (y2 - y1) / 2.0 * 1.15
                                    };

                                    results.Add(new FaceDetectionResult
                                    {
                                        FaceRect = rect,
                                        Confidence = confidence,
                                        DetectorName = Name,
                                        Oval = oval
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DnnFaceDetector] Error: {ex.Message}");
                }
                return results;
            });
        }
    }
}
