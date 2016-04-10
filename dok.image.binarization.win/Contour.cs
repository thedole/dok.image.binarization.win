using System.Collections.Generic;
using System.Linq;

namespace dok.image.binarization.win
{
    public class Contour
    {
        public HashSet<ImageCoordinate> Points { get; set; }
        public IEnumerable<IEnumerable<int>> PointsArray => Points.Select(imageCoordinate => imageCoordinate.ToArray()).ToArray();

        private bool _hasArea = false;
        public bool HasArea => _hasArea;
        public double Area { get; set; }

        public Contour ScaleWith(ImageDimensions targetDimensions)
        {
            return new Contour(Points.Select(p => p.ScaleTo(targetDimensions)));
        }

        public Contour(IEnumerable<ImageCoordinate> points)
        {
            Points = new HashSet<ImageCoordinate>(points);
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

            if (area != null)
            {
                Area = area.Value;
                _hasArea = true;
            }
        }
    }
}
