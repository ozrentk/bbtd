using System.ComponentModel.DataAnnotations;

namespace BBTD.Mvc.Models
{
    public class BarcodeSlideshowData
    {
        public int DataCount { get; set; }
        public int BarcodeSize { get; set; }

        [Display(Name = "Barcode Type")]
        public ZXing.BarcodeFormat BarcodeType { get; set; }
        public int TimeoutMilliseconds { get; set; }

        [Display(Name = "Distance")]
        public int DistanceFromScreen { get; set; }
    }
}
