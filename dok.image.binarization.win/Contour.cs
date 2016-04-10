using System.Collections.Generic;
using System.Linq;

namespace dok.image.binarization.win
{
    public class Contour
    {
        public ImageDimensions OriginalImageDimensions { get; set; }
        public HashSet<ImageCoordinate> Points { get; set; }
        public IEnumerable<IEnumerable<int>> PointsArray => Points.Select(imageCoordinate => imageCoordinate.ToArray()).ToArray();

        private bool _hasArea = false;
        public bool HasArea => _hasArea;
        public double Area { get; set; }

        public Contour ScaleWith(ImageDimensions targetDimensions)
        {
            var xFactor = targetDimensions.Width / OriginalImageDimensions.Width;
            var yFactor = targetDimensions.Height / OriginalImageDimensions.Height;
            var totalFactor = xFactor * yFactor;
            var area = Area * totalFactor;

            var points = Points.Select(p => p.ScaleTo(targetDimensions));
            return new Contour(points, targetDimensions, area);
        }

        public Contour(IEnumerable<ImageCoordinate> points) : this(points, null, null)
        {
        }

        public Contour(IEnumerable<ImageCoordinate> points, ImageDimensions dimensions, double? area)
        {
            Points = new HashSet<ImageCoordinate>(points);

            OriginalImageDimensions = dimensions;

            if (area != null)
            {
                Area = area.Value;
                _hasArea = true;
            }
        }

        public Contour(int [][] points, ImageDimensions dimensions) : this(points, dimensions, null)
        {            
        }

        public Contour(int[][] points, ImageDimensions dimensions, double? area)
        {
            Points = new HashSet<ImageCoordinate>(points
                .Select(p => new ImageCoordinate
                {
                    OriginalImageDimensions = dimensions,
                    OriginalImageXCoordinate = p[0],
                    OriginalImageYCoordinate = p[1]
                }));

            OriginalImageDimensions = dimensions;

            if (area != null)
            {
                Area = area.Value;
                _hasArea = true;
            }
        }
    }
}
