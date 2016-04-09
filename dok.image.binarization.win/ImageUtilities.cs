using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.Linq;
using dok.image.binarization.win.local;

namespace dok.image.binarization.win
{
    using OpenCvSharp;
    using OpenCvSharp.CPlusPlus;
    using OpenCvSharp.Extensions;

    public static class ImageUtilities
    {
        public static ImageDimensions ProcessingSize = new ImageDimensions { Width = 640, Height = 480 };
        private static readonly double MinAreaForRegionOfInterest = 320 * 240;
        private static readonly Point CenterOfProcessingImage = new Point(160, 120);
        private static readonly Size SampleSize = new Size(10, 10);
        private static readonly Rect CenterSample = new Rect(CenterOfProcessingImage, SampleSize);

        private static Mat ReadImage(string path, LoadMode loadMode = LoadMode.AnyColor)
        {
            var img = Cv2.ImRead(path, loadMode);
            return img;
        }

        private static IEnumerable<Mat> ReadImageFolder(string path)
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

        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
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
            using (var sourceMat = sourceBitmap.ToMat())
            {
                var meanStdDev = sourceMat.MeanStdDev();
                var channelIndex = meanStdDev.IndexOfBiggestStdDev();
                using (var channel = sourceMat.Split()[channelIndex])
                {
                    var channelMean = meanStdDev[channelIndex].Item1;
                    var actualR = channelMean * 0.3;

                    var sourceIplImage = channel
                                    .ToIplImage();
                    IplImage destinationIplImage = sourceIplImage.Binarize(size: 51, k: 0.2, r: actualR);

                    var outputBitmap = destinationIplImage.ToBitmap();
                    return outputBitmap;
                }
            }
        }

        public static void SaveToDisk(this BitmapSource bitmapSource, string fileName = "image.png")
        {
            using (var mat = bitmapSource.ToMat())
            {
                mat.SaveImage(fileName);
            }
        }

        public static Bitmap FindAndShowRegionOfInterest(this BitmapSource sourceImage, BitmapSource canvas = null)
        {
            var sourceMat = sourceImage.ToMat();
            var outMat = canvas != null ? canvas.ToMat() : sourceImage.ToMat().Clone();
            var contours = sourceMat.FindContours(MinAreaForRegionOfInterest);

            sourceMat.DrawContours(contours, -1, Scalar.Green, 10);
            Cv2.ImShow("Hell hole", sourceMat);

            return outMat.ToBitmap();
            
        }

        public static IEnumerable<RegionOfInterest> FindRegionOfInterest(this BitmapSource bitmapSource)
        {
            var sourceMat = bitmapSource.ToMat();

            var contours = FindContours(sourceMat, MinAreaForRegionOfInterest);
            var dimensions = sourceMat.Size().ToImageDimensions();
            var rois = contours
                .Select(MapPointsToImageCoordinates(dimensions))
                .Select(MapImageCoordinatesToIntArray)
                .Select(a => new RegionOfInterest(a.ToArray(), dimensions));
            
            return rois;

        }

        private static IEnumerable<int[]> MapImageCoordinatesToIntArray(IEnumerable<ImageCoordinate> imageCoordinates)
        {
            return imageCoordinates.Select(ic => ic.ToArray());
        }

        private static Func<IEnumerable<Point>, IEnumerable<ImageCoordinate>> MapPointsToImageCoordinates(ImageDimensions dimensions)
        {
            return p => p.Select(point => new ImageCoordinate
            {
                OriginalImageDimensions = dimensions,
                OriginalImageXCoordinate = point.X,
                OriginalImageYCoordinate = point.Y
            });
        }

        private static IEnumerable<IEnumerable<Point>> FindContours(this Mat sourceMat, double minArea)
        {
            var reducedMat = sourceMat.ReduceForProcessing();
            double threshold = GetThreshold(reducedMat);

            var binMat = reducedMat.Threshold(threshold, 255, ThresholdType.Binary);
            //Cv2.ImShow("Binary", binMat);
            var edges = binMat.Canny(10, 20);
            //var edges = reducedMat.Canny(10, 20);
            var dilated = edges.Dilate(Cv2.GetStructuringElement(StructuringElementShape.Cross, new Size(3, 3)));
            //Cv2.ImShow("Dilated", dilated);

            var contours = dilated.FindContoursAsArray(ContourRetrieval.List, ContourChain.ApproxSimple);
            var largestContours = contours
                    .Select(c =>
                    {
                        var area = Cv2.ContourArea(c, false);
                        var contour = Cv2.ApproxPolyDP(c, 50, true);
                        return new Contour { Points = contour, Area = area };
                    })
                        .Where(c => c.Area > MinAreaForRegionOfInterest);


            //var largestContour = contours
            //        .Aggregate(
            //            new Contour { Area = 0 },
            //            (acc, c) =>
            //            {
            //                var area = Cv2.ContourArea(c, false);
            //                return area > acc.Area ? new Contour { Points = c, Area = area } : acc;
            //            });

            //var simplifiedContour = Cv2.ApproxPolyDP(largestContour.Points,  50, true);
            //var simplifiedContourAsArray = simplifiedContour.ToArray();

            var dimensions = reducedMat.GetProcessingSize().ToImageDimensions();
            var contoursAsArray = largestContours.Select(c => c.Points.ToArray());
            var processingSizeRois = contoursAsArray
                .Select(c => new RegionOfInterest(c, dimensions));


            //reducedMat.DrawContours(largestContours.Select(c => c.Points), -1, Scalar.Green, 10);
            //Cv2.ImShow("Hell hole", reducedMat);

            var scaledRois = processingSizeRois
                .Select(c => c.ScaleWith(sourceMat.Size().ToImageDimensions()));

            var scaledAsPoints = scaledRois.Select(c => c.Points.Select(ic => ic.ToPoint()));

            
            return scaledAsPoints;
        }

        private static double GetThreshold(this Mat reducedMat)
        {
            Mat roiMin, roiMax;
            MaxMinSamples(reducedMat, SampleSize, out roiMin, out roiMax);

            var meanStdDevMin = roiMin.MeanStdDev();
            var meanStdDevMax = roiMax.MeanStdDev();
            var threshold = (meanStdDevMin.Mean.Val0 + meanStdDevMax.Mean.Val0) * 0.5;
            return threshold;
        }

        private static void MaxMinSamples(Mat reducedMat, Size sampleSize, out Mat roiMin, out Mat roiMax)
        {
            var minLoc = new Point();
            var maxLoc = new Point();
            reducedMat.MinMaxLoc(out minLoc, out maxLoc);
            minLoc = minLoc.Bound(sampleSize, reducedMat.Size());
            maxLoc = maxLoc.Bound(sampleSize, reducedMat.Size());


            Rect minSample = new Rect(minLoc, SampleSize);
            Rect maxSample = new Rect(maxLoc, SampleSize);

            //var colorImg = reducedMat.CvtColor(ColorConversion.GrayToBgr);
            //colorImg.Rectangle(minSample, Scalar.GreenYellow, 3);
            //colorImg.Rectangle(maxSample, Scalar.Red, 3);
            //Cv2.ImShow("Max and Min Samples", colorImg);

            roiMin = reducedMat[minSample];
            roiMax = reducedMat[maxSample];
        }

        private static Point LowerBoundX(this Point loc, Size sampleSize)
        {
            loc.X = (loc.X - sampleSize.Width) < 0
                ? sampleSize.Width
                : loc.X;
            return loc;
        }

        private static Point UpperBoundX(this Point loc, Size sampleSize, Size imageSize)
        {
            loc.X = (loc.X + sampleSize.Width) > imageSize.Width
                ? imageSize.Width - sampleSize.Width
                : loc.X;
            return loc;
        }

        private static Point BoundX(this Point loc, Size sampleSize, Size imageSize)
        {
            return loc
                .LowerBoundX(sampleSize)
                .UpperBoundX(sampleSize, imageSize);
        }

        private static Point LowerBoundY(this Point loc, Size sampleSize)
        {
            loc.Y = (loc.Y - sampleSize.Height) < 0
                ? sampleSize.Height
                : loc.Y;
            return loc;
        }

        private static Point UpperBoundY(this Point loc, Size sampleSize, Size imageSize)
        {
            loc.Y = (loc.Y + sampleSize.Height) > imageSize.Height
                ? imageSize.Height -sampleSize.Height
                : loc.Y;
            return loc;
        }

        private static Point BoundY(this Point loc, Size sampleSize, Size imageSize)
        {
            return loc
                .LowerBoundY(sampleSize)
                .UpperBoundY(sampleSize, imageSize);
        }

        private static Point Bound(this Point loc, Size sampleSize, Size imageSize)
        {
            return loc
                .BoundX(sampleSize, imageSize)
                .BoundY(sampleSize, imageSize);
        }

        private static int[][] ToArray(this Point[] points)
        {
            return points
                .Select(p => new[] { p.X, p.Y })
                .ToArray();
        }

        private static Point[] ToPointArray(this RegionOfInterest roi)
        {
            var pointArray = roi.Points
                .Select(ToPoint)
                .ToArray();

            return pointArray;
        }

        private static Point ToPoint(this ImageCoordinate imageCoordinate)
        {
            return new Point(imageCoordinate.OriginalImageXCoordinate, imageCoordinate.OriginalImageYCoordinate);
        }

        private static ImageDimensions ToImageDimensions(this Size size)
        {
            return new ImageDimensions {
                Width = size.Width,
                Height = size.Height
            };
        }

        private static Size GetProcessingSize(this Mat sourceMat)
        {
            return sourceMat.Width > sourceMat.Height
                ? new Size(ProcessingSize.Width, ProcessingSize.Height)
                : new Size(ProcessingSize.Height, ProcessingSize.Width);
        }

        private static Mat ReduceForProcessing(this Mat sourceMat)
        {
            var processingSize = sourceMat.GetProcessingSize();

            using (var greyMat = sourceMat.Split()[0]) // Hack for now
            using (var shrinkedMat = greyMat.Resize(processingSize))
            using (var blurredMat = shrinkedMat.MedianBlur(17))
            {
                return blurredMat.Clone();
            }
        }

    }
}
