﻿using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.IO;
using System.Collections.Generic;

namespace dok.image.binarization.win
{
    public class ImageUtilities
    {
        public static Mat ReadImage(string path, LoadImageType imageType = LoadImageType.AnyColor)
        {
            var img = CvInvoke.Imread(path, imageType);
            return img;
        }

        public static IEnumerable<Mat> ReadImageFolder(string path)
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
    }
}