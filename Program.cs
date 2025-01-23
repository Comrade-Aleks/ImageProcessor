using System;
using System.IO;
using System.Threading.Tasks;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using GTranslate.Translators;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

namespace ProjectTesseract
{
    public class Program
    {
        // Configurable Variables
        private const double ResizeFactor = 2.0;  // Scale factor for resizing the image
        private const float GlobalThreshold = 0.5f; // Threshold value (0-1 for ImageSharp)
        private const string TargetLanguage = "en"; // Target language for translation
        private const string SourceLanguage = "nor"; // Source language for translation
        private const string TessdataName = "nor"; // Tessdata package name (cant be same because of differences (for now))

        static async Task Main(string[] args)
        {
            Program obj = new Program();
            await obj.ConvertImageToTextAsync();
        }

        public async Task ConvertImageToTextAsync()
        {
            string imagePath = "C:\\Users\\Aleksander\\Desktop\\nor.png";
            string preprocessedImagePath = "C:\\Users\\Aleksander\\Desktop\\preprocessed.png";

            // Preprocess the image to clean up and focus on text
            CleanAndPreprocessImage(imagePath, preprocessedImagePath);

            try
            {
                // Set the path to the tessdata directory
                string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                // Initialize Tesseract with languages
                using (var engine = new TesseractEngine(tessdataPath, SourceLanguage, EngineMode.Default)) // change back to TessdataName if not working
                {
                    using (var img = Pix.LoadFromFile(preprocessedImagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            // Extract the recognized text
                            string plainText = page.GetText();
                            Console.WriteLine("Full Recognized Text:");
                            Console.WriteLine(plainText);

                            // Translate the text
                            string translatedText = await TranslateTextAsync(plainText);
                            Console.WriteLine("\nTranslated Text:");
                            Console.WriteLine(translatedText);
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

        private async Task<string> TranslateTextAsync(string text)
        {
            try
            {
                // Create an instance of the Google Translator
                var translator = new GoogleTranslator();

                // Translate the text using both source and target languages
                var result = await translator.TranslateAsync(text, TargetLanguage, SourceLanguage);

                // Extract the translated text from the result object
                return result.Translation; // Use the "Translation" property
            }
            catch (Exception ex)
            {
                Console.WriteLine("Translation Error: " + ex.Message);
                return "Translation failed.";
            }
        }
    }
}
