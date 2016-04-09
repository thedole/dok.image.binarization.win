namespace dok.image.binarization.win
{
    public class ImageCoordinate
    {
        public ImageDimensions OriginalImageDimensions { get; set; }
        public int OriginalImageXCoordinate { get; set; }
        public int OriginalImageYCoordinate { get; set; }

        public double ProportionalX => ((double)OriginalImageXCoordinate / (double)OriginalImageDimensions.Width);
        public double ProportionalY => ((double)OriginalImageYCoordinate / (double)OriginalImageDimensions.Height);

        public override int GetHashCode()
        {
            var xComponent = (int)(int.MaxValue * ProportionalX);
            var yComponent = (int)(int.MaxValue * ProportionalY);

            return int.MinValue + xComponent + yComponent;
        }

        public ImageCoordinate ScaleTo(ImageDimensions newImageDimension)
        {
            var newX = (int)(newImageDimension.Width * ProportionalX);
            var newY = (int)(newImageDimension.Height * ProportionalY);

            return new ImageCoordinate
            {
                OriginalImageDimensions = newImageDimension,
                OriginalImageXCoordinate = newX,
                OriginalImageYCoordinate = newY
            };
        }

        public int[] ToArray()
        {
            return new int[] { OriginalImageXCoordinate, OriginalImageYCoordinate };
        }
    }
}
