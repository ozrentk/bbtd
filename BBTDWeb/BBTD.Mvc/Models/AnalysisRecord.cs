using System;
using System.ComponentModel;

namespace BBTD.Mvc.Models
{
    public class AnalysisRecord
    {
        [DisplayName("Record number")]
        public int RecordNumber { get; set; }

        [DisplayName("Started at")]
        public DateTime? StartedAt { get; set; }

        [DisplayName("Total time")]
        public int? TotalTime { get; set; }

        [DisplayName("Image generation time")]
        public int? ImageGenerationTime { get; set; }

        [DisplayName("Image loading time")]
        public int? ImageLoadingTime { get; set; }

        [DisplayName("Barcode reading time")]
        public int? BarcodeReadingTime { get; set; }
    }
}
