using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp.CPlusPlus;

namespace dok.image.binarization.win.internalclasses
{
    internal static class InternalConverter
    {
        internal static IEnumerable<double> AsEnumerable(this Scalar source, int channels)
        {
            var enumerable = new List<double>(channels);
            for (int i = 0; i < channels; i++)
            {
                enumerable.Add(source[i]);
            }
            return enumerable;
        }
        
        internal static IEnumerable<Point> MapImageCoordinatesToPoints(IEnumerable<ImageCoordinate> imageCoordinates)
        {
            return imageCoordinates.Select(ic => ic.ToPoint());
        }

        internal static IEnumerable<IEnumerable<Point>> MapContoursToPoints(IEnumerable<Contour> contours)
        {
            return contours.Select(MapContourToPoints);
        }

        internal static IEnumerable<Point> MapContourToPoints(Contour c)
        {
            return c.Points.Select(ic => ic.ToPoint());
        }

        internal static Func<IEnumerable<Point>, IEnumerable<ImageCoordinate>> MapPointsToImageCoordinates(ImageDimensions dimensions)
        {
            return p => p.Select(point => new ImageCoordinate
            {
                OriginalImageDimensions = dimensions,
                OriginalImageXCoordinate = point.X,
                OriginalImageYCoordinate = point.Y
            });
        }

        internal static int[][] ToArray(this Point[] points)
        {
            return points
                .Select(p => new[] { p.X, p.Y })
                .ToArray();
        }

        internal static Point[] ToPointArray(this Contour roi)
        {
            var pointArray = roi.Points
                .Select(ToPoint)
                .ToArray();

            return pointArray;
        }

        internal static Point ToPoint(this ImageCoordinate imageCoordinate)
        {
            return new Point(imageCoordinate.OriginalImageXCoordinate, imageCoordinate.OriginalImageYCoordinate);
        }

        internal static ImageDimensions ToImageDimensions(this Size size)
        {
            return new ImageDimensions
            {
                Width = size.Width,
                Height = size.Height
            };
        }
    }
}
