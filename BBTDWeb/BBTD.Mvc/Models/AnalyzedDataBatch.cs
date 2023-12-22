using System.Collections.Generic;

namespace BBTD.Mvc.Models
{
    public class AnalyzedDataBatch
    {
        public AnalyzedDataBatch()
        {
            Records = new();
        }
        public int BatchNumber { get; set; }
        public string BarcodeType { get; set; }
        public int ItemCount { get; set; }
        public int Size { get; set; }
        public int Distance { get; set; }
        public List<AnalysisRecord> Records { get; set; }
    }
}
