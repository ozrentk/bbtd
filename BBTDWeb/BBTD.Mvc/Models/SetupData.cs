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

        [Display(Name = "Barcode Size")]
        public int BarcodeSize { get; set; }

        [Display(Name = "Barcode read timeout [ms]")]
        public int TimeoutMilliseconds { get; set; }

        public bool IsRefresh { get; set; }
    }
}
