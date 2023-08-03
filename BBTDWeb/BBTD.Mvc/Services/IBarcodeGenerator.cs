using System.Drawing.Imaging;
using System.Drawing;
using ZXing.Common;
using ZXing;

namespace BBTD.Mvc.Services
{
    public interface IBarcodeGenerator
    {
        byte[] GenerateBarcode(string jsonPerson);
    }

    public class BarcodeGenerator : IBarcodeGenerator
    {
        public byte[] GenerateBarcode(string jsonPerson)
        {
            MemoryStream ms;
            Bitmap pixelData;
            byte[] imageData;

            var barcodeWriter = new BarcodeWriter<Bitmap>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions { Height = 200, Width = 200, Margin = 0 },
                Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
            };
            ms = new MemoryStream();
            pixelData = barcodeWriter.Write(jsonPerson);
            pixelData.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }
    }
}
