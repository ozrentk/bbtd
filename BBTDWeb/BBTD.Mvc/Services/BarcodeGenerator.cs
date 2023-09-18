using System.Drawing.Imaging;
using System.Drawing;
using ZXing.Common;
using ZXing;
using ZXing.QrCode;
using System.IO;

namespace BBTD.Mvc.Services
{
    public interface IBarcodeGenerator
    {
        byte[] GenerateBarcode(string jsonPerson, int barcodeSize, BarcodeFormat barcodeType);
    }

    public class BarcodeGenerator : IBarcodeGenerator
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public byte[] GenerateBarcode(string jsonPerson, int barcodeSize, BarcodeFormat barcodeType)
        {
            MemoryStream ms;
            Bitmap pixelData;
            //byte[] imageData;

            var options = new EncodingOptions
            {
                Width = barcodeSize,
                Margin = 0,
                NoPadding = true,
                PureBarcode = true,
            };

            if (barcodeType != BarcodeFormat.PDF_417)
            {
                options.Height = barcodeSize;
            }

            var barcodeWriter = new BarcodeWriter<Bitmap>
            {
                Format = barcodeType,
                Options = options,
                Renderer = new ZXing.Windows.Compatibility.BitmapRenderer(),
            };

            ms = new MemoryStream();
            pixelData = barcodeWriter.Write(jsonPerson);
            pixelData.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }
    }
}
