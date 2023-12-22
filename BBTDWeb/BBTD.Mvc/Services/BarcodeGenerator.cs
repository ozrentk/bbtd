using System.Drawing.Imaging;
using System.Drawing;
using ZXing.Common;
using ZXing;
using ZXing.QrCode;
using System.IO;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;

namespace BBTD.Mvc.Services
{
    public interface IBarcodeGenerator
    {
        byte[] GenerateBarcode(string jsonPerson, int barcodeSize, BarcodeFormat barcodeType);
        (int, int, int, int) GetBarcodeRawSizes(string jsonPerson, int barcodeSize, BarcodeFormat barcodeType);
    }

    public class BitArray2DRenderer : IBarcodeRenderer<bool[,]>
    {
        public bool[,] Render(BitMatrix matrix, BarcodeFormat format, string content)
        {
            return Render(matrix, format, content, null);
        }

        public bool[,] Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
        {
            var result = new bool[matrix.Width, matrix.Height];
            for (var y = 0; y < matrix.Height; y++)
            {
                for (var x = 0; x < matrix.Width; x++)
                {
                    result[x, y] = matrix[x, y] ? true : false;
                }
            }

            return result;
        }
    }

    public class BarcodeGenerator : IBarcodeGenerator
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private static BarcodeWriter<Bitmap> GetBitmapWriter(int barcodeSize, BarcodeFormat barcodeType)
        {
            var options = new EncodingOptions
            {
                Width = barcodeSize,
                Height = barcodeSize, // PDF417 automatically adapts to the ratio
                Margin = 0,
                NoPadding = true,
                PureBarcode = true,
            };

            //if (barcodeType != BarcodeFormat.PDF_417)
            //    options.Height = barcodeSize;

            var barcodeWriter = new BarcodeWriter<Bitmap>
            {
                Format = barcodeType,
                Options = options,
                Renderer = new ZXing.Windows.Compatibility.BitmapRenderer(),
            };
            return barcodeWriter;
        }

        public byte[] GenerateBarcode(string jsonPerson, int barcodeSize, BarcodeFormat barcodeType)
        {
            var barcodeWriter = GetBitmapWriter(barcodeSize, barcodeType);

            MemoryStream ms = new MemoryStream();
            Bitmap pixelData = barcodeWriter.Write(jsonPerson);
            pixelData.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }

        public (int, int, int, int) GetBarcodeRawSizes(string jsonPerson, int barcodeSize, BarcodeFormat barcodeType)
        {
            var bitmapWriter = GetBitmapWriter(barcodeSize, barcodeType);
            var bitmapMatrix = bitmapWriter.Encode(jsonPerson);

            var matrixWriter = GetMatrixWriter(barcodeType);
            var matrixMatrix = matrixWriter.Encode(jsonPerson);

            var mtxMatrixH = (barcodeType != BarcodeFormat.PDF_417) ? matrixMatrix.Height : matrixMatrix.Height / 4;
            return (matrixMatrix.Width, mtxMatrixH, bitmapMatrix.Width, bitmapMatrix.Height);
        }

        private static BarcodeWriter<bool[,]> GetMatrixWriter(BarcodeFormat barcodeType)
        {
            return new BarcodeWriter<bool[,]>
            {
                Format = barcodeType,
                Options = new EncodingOptions
                {
                    Width = 1,
                    Height = (barcodeType != BarcodeFormat.PDF_417) ? 1 : default,
                    Margin = 0,
                    NoPadding = true,
                    PureBarcode = true,
                },
                Renderer = new BitArray2DRenderer(),
            };
        }
    }
}
