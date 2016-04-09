using System.Collections.Generic;
using System.Linq;

namespace dok.image.binarization.win
{
    public class RegionOfInterest
    {
        public HashSet<ImageCoordinate> Points { get; set; }

        public RegionOfInterest ScaleWith(ImageDimensions targetDimensions)
        {
            return new RegionOfInterest(Points.Select(p => p.ScaleTo(targetDimensions)));
        }

        public RegionOfInterest(IEnumerable<ImageCoordinate> points)
        {
            Points = new HashSet<ImageCoordinate>(points);
        }

        public RegionOfInterest(int [][] points, ImageDimensions dimensions)
        {
            Points = new HashSet<ImageCoordinate> (points
                .Select(p => new ImageCoordinate {
                    OriginalImageDimensions = dimensions,
                    OriginalImageXCoordinate = p[0],
                    OriginalImageYCoordinate = p[1]
                }));
        }
    }
}
