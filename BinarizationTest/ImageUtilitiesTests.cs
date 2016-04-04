using dok.image.binarization.win;
using Emgu.CV;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dok.image.binarization.win.Tests
{
    [TestClass()]
    public class ImageUtilitiesTests
    {
        private static readonly string testImagesFolder = $".{Path.DirectorySeparatorChar}testImages";
        [TestMethod()]
        public void ReadImageTest()
        {
            var testImageName = Directory.EnumerateFileSystemEntries(testImagesFolder).FirstOrDefault();
            var image = ImageUtilities.ReadImage(testImageName);
            Assert.IsInstanceOfType(image, typeof(Mat));
        }


        [TestMethod()]
        public void True()
        {
            Assert.IsTrue(true);
        }

        [TestMethod()]
        public void ReadImageFolderTest()
        {
            var images = ImageUtilities.ReadImageFolder(testImagesFolder);
            Assert.IsInstanceOfType(images, typeof(IEnumerable<Mat>));
            Assert.AreNotEqual(0, images.Count(), "No images read from testImagesFolder");
        }
    }
}
