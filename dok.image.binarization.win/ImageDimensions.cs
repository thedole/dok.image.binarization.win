using System.Windows.Media.Imaging;

namespace dok.image.binarization.win
{
    public class ImageDimensions
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int ChannelCount { get; set; }
        public int BitsPerPixel { get; set; }

        public ImageDimensions()
        {

        }
        public ImageDimensions(BitmapSource source)
        {
            Width = source.PixelWidth;
            Height = source.PixelHeight;
        }
    }

}
