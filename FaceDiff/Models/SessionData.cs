using System;
using System.Collections.ObjectModel;
using FaceDiff.Core;

namespace FaceDiff.Models
{
    public class SessionData
    {
        public event Action TemplateParametersChanged;

        public void RaiseTemplateParametersChanged() => TemplateParametersChanged?.Invoke();

        public RangeObservableCollection<BaseImageModel> BaseImages { get; set; }
            = new RangeObservableCollection<BaseImageModel>();

        public RangeObservableCollection<ComparisonImageModel> ComparisonImages { get; set; }
            = new RangeObservableCollection<ComparisonImageModel>();

        public ObservableCollection<ProcessResult> Results { get; set; }
            = new ObservableCollection<ProcessResult>();

        public string ComparisonFolderPath { get; set; }
        public string DestinationPath { get; set; }
    }
}
