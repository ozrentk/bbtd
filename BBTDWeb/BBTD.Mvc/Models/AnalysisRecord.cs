using System;
using System.ComponentModel;

namespace BBTD.Mvc.Models
{
    public class AnalysisRecord
    {
        [DisplayName("Record number")]
        public int RecordNumber { get; set; }

        [DisplayName("Barcode width [mod]")]
        public int BarcodeWidthMod { get; set; }

        [DisplayName("Barcode height [mod]")]
        public int BarcodeHeightMod { get; set; }

        [DisplayName("Barcode width [px]")]
        public int BarcodeWidthPx { get; set; }

        [DisplayName("Barcode height [px]")]
        public int BarcodeHeightPx { get; set; }

        [DisplayName("Barcode width [mm]")]
        public double BarcodeWidthMm { get; set; }

        [DisplayName("Barcode height [mm]")]
        public double BarcodeHeightMm { get; set; }

        [DisplayName("Module width [px]")]
        public double ModuleWidthPx { get; set; }

        [DisplayName("Module height [px]")]
        public double ModuleHeightPx { get; set; }

        [DisplayName("Module width [mm]")]
        public double ModuleWidthMm { get; set; }

        [DisplayName("Module height [mm]")]
        public double ModuleHeightMm { get; set; }

        [DisplayName("Pixel/mm ratio X")]
        public double PixelMmRatioX { get; set; }

        [DisplayName("Pixel/mm ratio Y")]
        public double PixelMmRatioY { get; set; }

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
