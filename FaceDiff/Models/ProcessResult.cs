using System.Collections.Generic;

namespace FaceDiff.Models
{
    public class ProcessResult
    {
        public BaseImageModel BaseImage { get; set; }
        public List<string> GeneratedDiffPaths { get; set; } = new List<string>();
        public int ComparisonCount { get; set; }
        public bool Accepted { get; set; }
    }
}
