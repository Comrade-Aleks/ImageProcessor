using System;
using System.Linq;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;

namespace ImageProcessor
{
    public class OrientationDetector
    {
        private readonly string tessdataPath;
        private readonly string language;

        public OrientationDetector(string tessdataPath, string language = "eng")
        {
            this.tessdataPath = tessdataPath;
            this.language = language;
        }

        public double DetectOrientation(string imagePath)
        {
            double bestAngle = 0;
            int maxValidChars = 0;
            float highestConfidence = 0;
            string bestExtractedText = "";

            using (var engine = new TesseractEngine(tessdataPath, language, EngineMode.Default))
            {
                // First, try OSD (Orientation and Script Detection)
                using (var img = Pix.LoadFromFile(imagePath))
                using (var page = engine.Process(img, PageSegMode.AutoOsd))
                {
                    try
                    {
                        page.DetectBestOrientation(out int osdOrientation, out float confidence);
                        if (confidence > 0.8f && osdOrientation % 90 == 0)
                        {
                            return osdOrientation;
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore errors and proceed with manual detection
                    }
                }

                // Test 0° and 180° first
                double[] initialAngles = { 0, 180 };
                foreach (double angle in initialAngles)
                {
                    EvaluateRotation(engine, imagePath, angle, ref bestAngle, ref maxValidChars, ref highestConfidence, ref bestExtractedText);
                }

                // If confidence is low, check finer angles
                if (highestConfidence < 0.85f)
                {
                    for (double angle = -90; angle <= 90; angle += 5)
                    {
                        if (angle == 0 || angle == 180) continue; // Skip already tested angles
                        EvaluateRotation(engine, imagePath, angle, ref bestAngle, ref maxValidChars, ref highestConfidence, ref bestExtractedText);
                    }
                }
            }
            return bestAngle;
        }

        private void EvaluateRotation(TesseractEngine engine, string imagePath, double angle, ref double bestAngle, ref int maxValidChars, ref float highestConfidence, ref string bestExtractedText)
        {
            string rotatedImagePath = RotateImage(imagePath, angle);
            using (var img = Pix.LoadFromFile(rotatedImagePath))
            using (var page = engine.Process(img, PageSegMode.SingleWord)) // Use SingleWord mode for better small text recognition
            {
                string extractedText = page.GetText().Trim();
                float avgConfidence = page.GetMeanConfidence();
                int validCharCount = extractedText.Count(char.IsLetterOrDigit);

                if (validCharCount > maxValidChars || (validCharCount == maxValidChars && avgConfidence > highestConfidence))
                {
                    maxValidChars = validCharCount;
                    highestConfidence = avgConfidence;
                    bestAngle = angle;
                    bestExtractedText = extractedText;
                }
            }
            File.Delete(rotatedImagePath); // Cleanup temp file
        }

        public string RotateImage(string imagePath, double angle)
        {
            using (var image = Image.Load<Rgba32>(imagePath))
            {
                image.Mutate(x =>
                {
                    x.Rotate((float)angle)
                     .Grayscale()
                     .BinaryThreshold(0.5f);
                });

                string tempFilePath = Path.Combine(Path.GetTempPath(), $"rotated_{Guid.NewGuid()}.png");
                image.Save(tempFilePath);
                return tempFilePath;
            }
        }
    }
}
