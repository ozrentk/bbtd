using System.ComponentModel.DataAnnotations;
using ZXing;

namespace BBTD.Mvc.Models
{
    public class SetupData
    {
        [Display(Name = "Server URL")]
        public string ServerUrl { get; set; }

        [Display(Name = "Barcode Type")]
        public BarcodeFormat BarcodeType { get; set; }

        [Display(Name = "Number of Barcode Items")]
        public int NumberOfItems { get; set; }

        [Display(Name = "Barcode Size [px]")]
        public int BarcodeSize { get; set; }

        [Display(Name = "Server barcode read timeout [ms]")]
        public int ServerTimeoutMilliseconds { get; set; }

        [Display(Name = "Screen width [mm]")]
        public int? ScreenWidthMm { get; set; }

        [Display(Name = "Screen height [mm]")]
        public int? ScreenHeightMm { get; set; }

        [Display(Name = "Screen width [px]")]
        public int? ScreenWidthPx { get; set; }

        [Display(Name = "Screen height [px]")]
        public int? ScreenHeightPx { get; set; }

        [Display(Name = "Distance")]
        public int DistanceFromScreen { get; set; }

        public bool IsRefresh { get; set; }
    }
}
