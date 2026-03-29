using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    public class CompositeFaceDetector
    {
        private readonly List<IFaceDetector> _detectors;

        public CompositeFaceDetector()
        {
            _detectors = new List<IFaceDetector>
            {
                new DnnFaceDetector(),
                new HaarFaceDetector(),
                new DlibHogFaceDetector()
            };
        }

        /// <summary>
        /// Runs all available detectors and returns the single best result
        /// ranked by confidence * face_area.
        /// </summary>
        public async Task<FaceDetectionResult> DetectBestFaceAsync(string imagePath)
        {
            var available = _detectors.Where(d => d.IsAvailable).ToList();
            if (available.Count == 0)
                return null;

            var tasks = available.Select(d => d.DetectFacesAsync(imagePath));
            var allResults = await Task.WhenAll(tasks);
            var flat = allResults.SelectMany(r => r).ToList();

            if (flat.Count == 0)
                return null;

            return flat.OrderByDescending(r => r.Score).First();
        }
    }
}
