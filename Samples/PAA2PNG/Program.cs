using BIS.PAA;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PAA2PNG
{
    class Program
    {
        static void Main(string[] args)
        {
            var filePath = (args.Length > 0) ? args[0] : "";
            var ext = Path.GetExtension(filePath);
            var isPAA = ext.Equals(".paa", System.StringComparison.OrdinalIgnoreCase);
            var isPAC = ext.Equals(".pac", System.StringComparison.OrdinalIgnoreCase);

            if (File.Exists(filePath) && (isPAA || isPAC))
            {
                //get raw pixel color data in ARGB32 format
                var paaStream = File.OpenRead(filePath);
                var paa = new PAA(paaStream, isPAC);
                var pixels = PAA.GetARGB32PixelData(paa, paaStream);

                //We use WPF stuff here to create the actual image file, so this is Windows only

                //create a BitmapSource 
                var colors = paa.Palette.Colors.Select(c => Color.FromRgb(c.R8, c.G8, c.B8)).ToList();
                var bitmapPalette = (colors.Count > 0) ? new BitmapPalette(colors) : null;
                var bms = BitmapSource.Create(paa.Width, paa.Height, 300, 300, PixelFormats.Bgra32, bitmapPalette, pixels, paa.Width * 4);

                //save as png
                var pngFilePath = Path.ChangeExtension(filePath, ".png");
                var pngStream = File.OpenWrite(pngFilePath);
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(bms));
                pngEncoder.Save(pngStream);

            }
        }
    }
}
