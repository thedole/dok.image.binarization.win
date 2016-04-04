using Emgu.CV;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace dok.image.binarization.win.Tests
{
    [TestClass()]
    public class ImageUtilitiesTests
    {
        [TestMethod()]
        public void ReadImageTest()
        {
            var testImageName = Directory.EnumerateFileSystemEntries($".{Path.DirectorySeparatorChar}testImages").FirstOrDefault();
            var image = ImageUtilities.ReadImage(testImageName);
            Assert.IsInstanceOfType(image, typeof(Mat));
        }


        [TestMethod()]
        public void True()
        {
            Assert.IsTrue(true);
        }
    }
}
