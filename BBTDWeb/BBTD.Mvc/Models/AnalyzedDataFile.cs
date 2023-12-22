using System.Collections.Generic;

namespace BBTD.Mvc.Models
{
    public class AnalyzedDataFile
    {
        public AnalyzedDataFile()
        {
            Batches = new();
        }
        public string DatasetName { get; set; }
        public string[] FileNames { get; set; }
        public List<AnalyzedDataBatch> Batches { get; set; }
    }
}
