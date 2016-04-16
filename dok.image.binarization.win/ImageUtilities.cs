using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.Linq;
using dok.image.binarization.win.internalclasses;

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

        public static BitmapSource Match(BitmapSource queryBitmap, BitmapSource trainBitmap)
        {
            using (var queryMat = queryBitmap.ToMat())
            using (var trainMat = trainBitmap.ToMat())
            using (var outMat = trainMat.Clone())
            {
                var largestSize = trainMat.Size().Width > trainMat.Size().Height 
                    ? trainMat.Size().Width 
                    : trainMat.Size().Height;
                var factor = (double)largestSize / 3000d;

                using (var orb = new ORB((int)(10000/* * factor*/)))
                {
                    var keypointsQuery = orb.Detect(queryMat);
                    Mat descriptorsQuery = new Mat();
                    orb.Compute(queryMat, ref keypointsQuery, descriptorsQuery);

                    var keypointsTrain = orb.Detect(trainMat);
                    Mat descriptorsTrain = new Mat();
                    orb.Compute(trainMat, ref keypointsTrain, descriptorsTrain);

                    var bfMatcher = new BFMatcher(NormType.Hamming, crossCheck: false);
                    var matches = bfMatcher.Match(descriptorsQuery, descriptorsTrain);

                    var bestDistance = matches.Min(m => m.Distance);
                    var limit = 20;
                    if (bestDistance > limit)
                    {
                        return ReturnFailedMatch(outMat,"Best distance", bestDistance);
                    }                  
                    matches = matches.Where(m => m.Distance <= limit).ToArray();

                    if (matches.Length < 25)
                    {
                        return ReturnFailedMatch(outMat, "count", matches.Length);
                    }

                    var angles = matches
                        .Select(MatchRelativeAngle(keypointsQuery, keypointsTrain));
                    
                    var limitedAngles = angles
                        .Select(Limit0To360);
                    Scalar mean, stdDev;
                    Cv2.MeanStdDev(InputArray.Create(limitedAngles), out mean, out stdDev);
                    if (stdDev.Val0 > 10)
                    {
                        return ReturnFailedMatch(outMat, "angledev", stdDev.Val0);
                    }

                    var angle = Math.Ceiling(360 - limitedAngles
                        .Average(a => Math.Ceiling(a)));

                    var center = new Point2f(outMat.Width / 2, outMat.Height / 2);
                    var destinationSize = new RotatedRect(center, outMat.Size().ToSize2f(), (float)angle)
                        .BoundingRect()
                        .Size;

                    var transform = Cv2.GetRotationMatrix2D(center, angle, 1);
                    transform.Set<double>(0, 2, transform.At<double>(0, 2) + destinationSize.Width / 2 - center.X);
                    transform.Set<double>(1, 2, transform.At<double>(1, 2) + destinationSize.Height / 2 - center.Y);



                    //var queryPoints = new List<Point2d>(matches.Length);
                    //var trainPoints = new List<Point2d>(matches.Length);

                    //for (int i = 0; i < matches.Length; i++)
                    //{
                    //    queryPoints.Add(new Point2d(keypointsQuery[matches[i].QueryIdx].Pt.X, keypointsQuery[matches[i].QueryIdx].Pt.Y));
                    //    trainPoints.Add(new Point2d(keypointsTrain[matches[i].TrainIdx].Pt.X, keypointsTrain[matches[i].TrainIdx].Pt.Y));
                    //}

                    //Cv2.DrawMatches(queryMat, keypointsQuery, trainMat, keypointsTrain, matches, outMat, Scalar.Green);
                    //var affine = Cv2.GetAffineTransform(trainPoints.Select(p => new Point2f((float)p.X, (float)p.Y)).Take(3), queryPoints.Select(p => new Point2f((float)p.X, (float)p.Y)).Take(3));
                    //var transform = Cv2.FindHomography(trainPoints, queryPoints, HomographyMethod.Ransac);
                    var transformedMat = outMat.WarpAffine(transform, destinationSize);
                    var transformedCenter = new Point2f(transformedMat.Width / 2, transformedMat.Height / 2);
                    //transformedMat.PutText($"{limit}", transformedCenter, FontFace.HersheyPlain, 15, Scalar.Green, 10);
                    transformedMat.PutText($"{bestDistance}", new Point2f(0, transformedCenter.Y - 100), FontFace.HersheyPlain, (int)(10 * factor), Scalar.Green, (int)(15 * factor));
                    transformedMat.PutText($"{mean.Val0:F1}-{stdDev.Val0:F1}", new Point2f(0, transformedCenter.Y + 100), FontFace.HersheyPlain, (int)(10 * factor), Scalar.Green, (int)(15 * factor));

                    return transformedMat.ToBitmapSource();
                }
            }
        }

        private static BitmapSource ReturnFailedMatch(Mat outMat, string paramName, double param)
        {
            var resultMat = outMat.Clone();
            var center = new Point2f(resultMat.Width / 2, resultMat.Height / 2);
            resultMat.PutText($"{param}", center, FontFace.HersheyPlain, 15, Scalar.Green, 10);
            var text = "Zero Matches";
            int baseLine = 0;
            var textSize = Cv2.GetTextSize(text, FontFace.HersheyPlain, 15, 10, out baseLine);
            resultMat.PutText(paramName, new Point(0, textSize.Height * 1.5), FontFace.HersheyPlain, 15, Scalar.Red, 10);
            resultMat.PutText(text, new Point(0, textSize.Height * 2.5), FontFace.HersheyPlain, 15, Scalar.Red, 10);

            return resultMat.ToBitmapSource();
        }

        private static BitmapSource ReturnFailedMatch(Mat outMat, float bestDistance, Scalar mean, Scalar stdDev)
        {
            var resultMat = outMat.Clone();
            var center = new Point2f(resultMat.Width / 2, resultMat.Height / 2);
            resultMat.PutText($"{bestDistance}", new Point2f(0, center.Y), FontFace.HersheyPlain, 15, Scalar.Green, 10);
            resultMat.PutText($"{mean.Val0:F1}-{stdDev.Val0:F1}", new Point2f(0, center.Y + 200), FontFace.HersheyPlain, 15, Scalar.Green, 10);
            var text = "Zero Matches";
            int baseLine = 0;
            var textSize = Cv2.GetTextSize(text, FontFace.HersheyPlain, 15, 10, out baseLine);
            resultMat.PutText(text, new Point(0, textSize.Height + 50), FontFace.HersheyPlain, 15, Scalar.Red, 10);

            return resultMat.ToBitmapSource();
        }

        private static float Limit0To360(float a)
        {
            return (a < 0 ? (360 + a) : a) % 360;
        }

        private static Func<DMatch, float> MatchRelativeAngle(KeyPoint[] keypointsQuery, KeyPoint[] keypointsTrain)
        {
            return (m) =>  keypointsQuery[m.QueryIdx].Angle - keypointsTrain[m.TrainIdx].Angle;
        }

        public static BitmapSource ReadImage(string path, LoadMode loadMode = LoadMode.AnyColor)
        {
            var img = Cv2.ImRead(path, (OpenCvSharp.LoadMode)loadMode);
            return img.ToBitmapSource();
        }

        private static IEnumerable<BitmapSource> ReadImageFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    throw new ArgumentOutOfRangeException(nameof(path), path, "Folder does not exist");
                }
                var images = new List<BitmapSource>();
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
            var stdDev = new Scalar();

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

        public static Bitmap Binarize(this BitmapSource sourceBitmap, int size = 200, double k = 0.2, double r = 128)
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

                    var outputBitmap = destinationIplImage.Clone().ToBitmap();
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

        public static BitmapSource CropToLargestContour(this BitmapSource source)
        {
            using (var sourceMat = source.ToMat())
            {
                var outMat = sourceMat.CropToLargestContour();
                return outMat.ToBitmapSource();
            }
        }

        private static Mat CropToLargestContour(this Mat sourceMat)
        {
            var emptyContour = new Contour(new int[][] {}, sourceMat.Size().ToImageDimensions(), 0);

            var contours = sourceMat.FindContours(MinAreaForRegionOfInterest);
            var largestContour = contours
                .Aggregate(emptyContour, (a, c) => c.Area > a.Area ? c : a)
                .ToPointArray();

            if (largestContour.Count() == 0)
            {
                return sourceMat;
            }

            var boundingBox = Cv2.BoundingRect(largestContour);
            return sourceMat[boundingBox].Clone();
        }

        public static BitmapSource FindAndShowContours(this BitmapSource sourceImage, BitmapSource canvas = null)
        {
            using (var sourceMat = sourceImage.ToMat())
            using (var outMat = (canvas != null) ? canvas.ToMat() : sourceMat)
            {
                var contours = sourceMat.FindContours(MinAreaForRegionOfInterest);

                DrawContours(sourceMat, contours);
                //Cv2.ImShow("Hell hole", sourceMat);

                var bitmapSource = sourceMat
                    .Clone()
                    .ToBitmapSource();

                return bitmapSource;
            }
        }

        private static void DrawContours(Mat sourceMat, IEnumerable<Contour> contours, int width = 10)
        {
            sourceMat.DrawContours(contours.Select(InternalConverter.MapContourToPoints), -1, Scalar.Green, width);
        }

        public static IEnumerable<Contour> FindContours(this BitmapSource bitmapSource)
        {
            var sourceMat = bitmapSource.ToMat();
            var contours = FindContours(sourceMat, MinAreaForRegionOfInterest);
            return contours;
        }

        private static IEnumerable<Contour> FindContours(this Mat sourceMat, double minArea)
        {
            var sourceDimensions = sourceMat
                .Size()
                .ToImageDimensions();

            double threshold;
            using ( var reducedMat = sourceMat.ReduceForProcessing() )
            {
              threshold = GetThreshold(reducedMat);
                using (var binMat = reducedMat.Threshold(threshold, 255, ThresholdType.Binary))
                {
                    binMat.Rectangle(new Rect(0, 0, binMat.Width, binMat.Height), Scalar.Black, 5);
                    using (var edges = binMat.Canny(10, 20))
                    using (var dilated = edges.Dilate(Cv2.GetStructuringElement(StructuringElementShape.Cross, new Size(3, 3))))
                    {
                        //Cv2.ImShow("Binary", binMat);
                        //Cv2.ImShow("Dilated", dilated);

                        var contours = dilated
                            .FindContoursAsArray(ContourRetrieval.List, ContourChain.ApproxSimple)
                            .Select(c =>
                            {
                                var area = Cv2.ContourArea(c, false);
                                var contour = Cv2.ApproxPolyDP(c, 50, true);
                                return new Contour(contour.ToArray(), ProcessingSize, area);
                            });

                        var scaledContours = contours
                            .Select(c => c.ScaleWith(sourceDimensions));

                        return scaledContours;
                    }
                }
            }
        }

        private static double GetThreshold(this Mat reducedMat)
        {
            Mat roiMin, roiMax;
            MaxMinSamples(reducedMat, SampleSize, out roiMin, out roiMax);

            var meanMin = roiMin
                .Mean()
                .AsEnumerable(1);
            var meanMax = roiMax
                .Mean()
                .AsEnumerable(1);
            roiMin.Dispose();
            roiMax.Dispose();

            var meanFull = reducedMat
                .Mean()
                .AsEnumerable(1);
            
            var threshold = (meanMin.First() * 0.5 + meanFull.First() * 0.2 + meanMax.First() * 0.3);
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
