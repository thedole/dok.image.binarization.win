using System;
using System.IO;
using System.Collections.Generic;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using OpenCvSharp.Extensions;

namespace dok.image.binarization.win
{
    public static class ImageUtilities
    {
        public static Mat ReadImage(string path, LoadMode loadMode = LoadMode.AnyColor)
        {
            var img = Cv2.ImRead(path, loadMode);
            return img;
        }

        public static IEnumerable<Mat> ReadImageFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    throw new ArgumentOutOfRangeException(nameof(path), path, "Folder does not exist");
                }
                var images = new List<Mat>();
                foreach (var fileName in Directory.EnumerateFileSystemEntries(path))
                {
                    if (!File.Exists(fileName))
                    {
                        continue;
                    }

                    if (!IsRecognisedImageFile(fileName))
                    {
                        continue;
                    }

                    images.Add(ReadImage(fileName));
                }

                return images;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static bool IsRecognisedImageFile(string fileName)
        {
            string targetExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(targetExtension))
                return false;
            else
                targetExtension = "*" + targetExtension.ToLowerInvariant();

            List<string> recognisedImageExtensions = new List<string>();

            foreach (System.Drawing.Imaging.ImageCodecInfo imageCodec in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
                recognisedImageExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(";".ToCharArray()));

            foreach (string extension in recognisedImageExtensions)
            {
                if (extension.Equals(targetExtension))
                {
                    return true;
                }
            }
            return false;
        }

        public static Bitmap ToBitmap(this BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                var bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        public static BitmapSource ToBitmapImage(this Bitmap bitmap)
        {
            BitmapSource i = Imaging.CreateBitmapSourceFromHBitmap(
                           bitmap.GetHbitmap(),
                           IntPtr.Zero,
                           Int32Rect.Empty,
                           BitmapSizeOptions.FromEmptyOptions());
            return i;
        }

        public static MeanStdDev MeanStdDev(this Mat source)
        {
            var mean = new Scalar();
            var stdDev = new Scalar(); ;

            Cv2.MeanStdDev(source, out mean, out stdDev);

            return new MeanStdDev
            {
                Mean = mean,
                StdDev = stdDev
            };
        }

        public static int IndexOfBiggestStdDev(this MeanStdDev meanStdDev)
        {
            var stdDev = meanStdDev.StdDev;
            var deviations = new[] { stdDev.Val0, stdDev.Val1, stdDev.Val2, stdDev.Val3 };
            var biggestIndex = 0;
            for (int i = 1; i < deviations.Length; i++)
            {
                if (deviations[i] < deviations[biggestIndex])
                {
                    continue;
                }

                biggestIndex = i;
            }

            return biggestIndex;
        }
        
        private static IplImage Binarize(this IplImage sourceIplImage, int size = 51, double k = 0.2, double r = 128)
        {
            var destinationIplImage = sourceIplImage.EmptyClone();
            Binarizer.SauvolaFast(sourceIplImage, destinationIplImage, size, k, r);
            return destinationIplImage;
        }

        public static Bitmap Binarize(this BitmapImage sourceBitmap, int size = 200, double k = 0.2, double r = 128)
        {
            var sourceMat = sourceBitmap.ToMat();
            var meanStdDev = sourceMat.MeanStdDev();
            var channelIndex = meanStdDev.IndexOfBiggestStdDev();
            var channel = sourceMat.Split()[channelIndex];
            var channelMean = meanStdDev[channelIndex].Item1;
            var actualR = channelMean * 0.3;

            var sourceIplImage = channel
                            .ToIplImage();
            IplImage destinationIplImage = sourceIplImage.Binarize(size: 51, k: 0.2, r: actualR);

            var outputBitmap = destinationIplImage.ToBitmap();
            return outputBitmap;
        }

        public static void SaveToDisk(this BitmapSource bitmapSource, string fileName = "image.png")
        {
            bitmapSource
                .ToMat()
                .SaveImage(fileName);
        }
    }
}
