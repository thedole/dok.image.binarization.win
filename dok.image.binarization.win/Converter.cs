using System.Collections.Generic;
using System.Linq;

namespace dok.image.binarization.win
{
    public class Converter
    {
        internal static IEnumerable<int[]> MapImageCoordinatesToIntArray(IEnumerable<ImageCoordinate> imageCoordinates)
        {
            return imageCoordinates.Select(ic => ic.ToArray());
        }
    }
}
