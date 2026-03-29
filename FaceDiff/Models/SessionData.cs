using System.Collections.ObjectModel;

namespace FaceDiff.Models
{
    public class SessionData
    {
        public ObservableCollection<BaseImageModel> BaseImages { get; set; }
            = new ObservableCollection<BaseImageModel>();

        public ObservableCollection<ComparisonImageModel> ComparisonImages { get; set; }
            = new ObservableCollection<ComparisonImageModel>();

        public ObservableCollection<ProcessResult> Results { get; set; }
            = new ObservableCollection<ProcessResult>();

        public string ComparisonFolderPath { get; set; }
        public string DestinationPath { get; set; }
    }
}
