using System;
using System.Collections.ObjectModel;

namespace FaceDiff.Models
{
    public class SessionData
    {
        public event Action TemplateParametersChanged;

        public void RaiseTemplateParametersChanged() => TemplateParametersChanged?.Invoke();

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
