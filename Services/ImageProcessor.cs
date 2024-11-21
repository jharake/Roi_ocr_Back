using OpenCvSharp;
using OpenCvSharp.Features2D;
using System.Collections.Generic;
using System.Linq;

namespace Roi_ocr.Services
{
    public class ImageProcessor
    {
        public string ClassifyImage(string imagePath, List<(string ImagePath, string ClassName)> storedImages)
        {
            // Load the input image
            var inputImage = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            inputImage = ScaleImage(inputImage, 500);

            // Extract features from the input image
            var inputDescriptors = ExtractFeatures(inputImage);

            var similarityScores = new List<(string ClassName, int Score)>();

            foreach (var (storedImagePath, className) in storedImages)
            {
                // Load the stored image
                var storedImage = Cv2.ImRead(storedImagePath, ImreadModes.Grayscale);
                storedImage = ScaleImage(storedImage, 500);

                // Extract features from the stored image
                var storedDescriptors = ExtractFeatures(storedImage);

                // Compare features and calculate the similarity score
                int matches = CompareFeatures(inputDescriptors, storedDescriptors);
                similarityScores.Add((className, matches));
            }

            // Determine the best match based on the highest score
            var bestMatch = similarityScores.OrderByDescending(s => s.Score).FirstOrDefault();

            // Apply a threshold for classification
            if (bestMatch.Score > 10) // Adjust threshold as needed
            {
                return bestMatch.ClassName;
            }

            return "Input image is classified as a new document.";
        }

        private Mat ExtractFeatures(Mat image)
        {
            var sift = SIFT.Create();
            KeyPoint[] keypoints;
            Mat descriptors = new Mat();
            sift.DetectAndCompute(image, null, out keypoints, descriptors);
            return descriptors;
        }

        private int CompareFeatures(Mat features1, Mat features2)
        {
            var bf = new BFMatcher(NormTypes.L2, crossCheck: true);
            var matches = bf.Match(features1, features2);
            return matches.Length; // Return the number of matches
        }

        private Mat ScaleImage(Mat image, int width)
        {
            var aspectRatio = (double)image.Height / image.Width;
            var newHeight = (int)(width * aspectRatio);
            return image.Resize(new OpenCvSharp.Size(width, newHeight));
        }
    }
}
