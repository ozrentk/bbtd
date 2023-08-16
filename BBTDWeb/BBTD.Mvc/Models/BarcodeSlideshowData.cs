namespace BBTD.Mvc.Models
{
    public class BarcodeSlideshowData
    {
        public int DataCount { get; set; }
        public int BarcodeId { get; set; }
        public int BarcodeSize { get; set; }
        public ZXing.BarcodeFormat BarcodeType { get; set; }
    }
}
