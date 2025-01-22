using System;
using System.IO;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

namespace ProjectTesseract
{
    public class Program
    {
        // Configurable Variables
        private const double ResizeFactor = 2.0;  // Scale factor for resizing the image
        private const float GlobalThreshold = 0.5f; // Threshold value (0-1 for ImageSharp)

        static void Main(string[] args)
        {
            Program obj = new Program();
            obj.ConvertImageToText();
        }

        public void ConvertImageToText()
        {
            string imagePath = "C:\\Users\\Aleksander\\Desktop\\japa.png";
            string preprocessedImagePath = "C:\\Users\\Aleksander\\Desktop\\preprocessed.png";

            // Preprocess the image to clean up and focus on text
            CleanAndPreprocessImage(imagePath, preprocessedImagePath);

            try
            {
                // Set the path to the tessdata directory
                string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                // Initialize Tesseract with languages (e.g., English and Japanese)
                using (var engine = new TesseractEngine(tessdataPath, "jpn", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(preprocessedImagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            // Extract the recognized text
                            string plainText = page.GetText();

                            // Output the recognized text
                            Console.WriteLine("Full Recognized Text:");
                            Console.WriteLine(plainText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private void CleanAndPreprocessImage(string inputPath, string outputPath)
        {
            using (var image = Image.Load<Rgba32>(inputPath))
            {
                // Step 1: Resize
                image.Mutate(x => x.Resize((int)(image.Width * ResizeFactor), (int)(image.Height * ResizeFactor)));

                // Step 2: Convert to grayscale
                image.Mutate(x => x.Grayscale());

                // Step 3: Apply global threshold
                image.Mutate(x => x.BinaryThreshold(GlobalThreshold));

                // Save the preprocessed image
                image.Save(outputPath);
            }
        }
    }
}
