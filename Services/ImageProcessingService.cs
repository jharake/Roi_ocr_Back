using OpenCvSharp;
using Roi_ocr.Models;
using System.Collections.Generic;

namespace Roi_ocr.Services
{
    public class ImageProcessingService
    {
        public Point GetReferenceImageTransformation(Mat documentImage, string referenceImagePath)
        {

            Mat referenceImage = Cv2.ImRead(referenceImagePath);

            if (referenceImage.Empty())
            {
                throw new ArgumentException("Reference image could not be loaded. Check the file path.");
            }
            
            if (referenceImagePath == "./e5raj/reference.jpg")
            {
                double percentage = 0.1; // Resize reference image to 10% of the main image
                int newWidth = (int)(documentImage.Width * percentage);
                int newHeight = (int)(referenceImage.Height * ((double)newWidth / referenceImage.Width));
                Cv2.Resize(referenceImage, referenceImage, new Size(newWidth, newHeight));
            }

            Mat result = new Mat();

            Cv2.MatchTemplate(documentImage, referenceImage, result, TemplateMatchModes.CCoeffNormed);

            // get best location
            double minVal, maxVal;
            Point minLoc, maxLoc;
            Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);
            Point topLeft = maxLoc;

            // bottom right
            int width = referenceImage.Width;
            int height = referenceImage.Height;
            Point bottomRight = new Point(topLeft.X + width, topLeft.Y + height);

            Console.WriteLine($"Bottom-right: {bottomRight}");

            return bottomRight;
        }


        public Dictionary<string, Mat> ExtractROIs(Mat documentImage, DocumentTemplate template, Point bottomRight)
        {
            double padding; 
            if (template.Type == "e5raj_aid") { 
                padding = 0.01; 
            }
            else
            {
                padding = 0.02;
            }
            
            var rois = new Dictionary<string, Mat>();
            int imageHeight = documentImage.Height - bottomRight.Y;
            int imageWidth = documentImage.Width - bottomRight.X;

            foreach (var field in template.Indices)
            {
                var (x1, y1, x2, y2) = field.Value;
                int x = (int)(x1 * imageWidth + bottomRight.X);
                int y = (int)(y1 * imageHeight + bottomRight.Y);
                int width = (int)((x2 - x1) * imageWidth);
                int height = (int)((y2 - y1) * imageHeight);

                //padding
                x = Math.Max(0, x - (int)(padding * documentImage.Width));
                y = Math.Max(0, y - (int)(padding * documentImage.Height));
                width = Math.Min(documentImage.Width - x, width + 2 * (int)(padding * documentImage.Width));
                height = Math.Min(documentImage.Height - y, height + 2 * (int)(padding * documentImage.Height));

                Rect roiRect = new Rect(x, y, width, height);
                Mat roi = new Mat(documentImage, roiRect);
                rois[field.Key] = roi;
            }

            return rois;
        }
    }
}
