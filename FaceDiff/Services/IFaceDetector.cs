using System.Collections.Generic;
using System.Threading.Tasks;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    public interface IFaceDetector
    {
        string Name { get; }
        bool IsAvailable { get; }
        Task<List<FaceDetectionResult>> DetectFacesAsync(string imagePath);
    }
}
