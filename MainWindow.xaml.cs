using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using GTranslate.Translators;

namespace ImageProcessor
{
    public partial class MainWindow : Window
    {
        private const double ResizeFactor = 2.0;
        private const float GlobalThreshold = 0.5f;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the selected source and target languages
                string sourceLanguage = ((ComboBoxItem)FromLanguageComboBox.SelectedItem)?.Tag?.ToString();
                string targetLanguage = ((ComboBoxItem)ToLanguageComboBox.SelectedItem)?.Tag?.ToString();

                if (string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(targetLanguage))
                {
                    OutputTextBox.Text = "Please select both source and target languages.";
                    return;
                }

                // File paths (adjust as needed)
                string imagePath = "C:\\Users\\Aleksander\\Desktop\\nor.png";
                string preprocessedImagePath = "C:\\Users\\Aleksander\\Desktop\\preprocessed.png";

                // Preprocess the image
                CleanAndPreprocessImage(imagePath, preprocessedImagePath);

                // Perform OCR and translation
                string recognizedText = await PerformOcrAsync(preprocessedImagePath, sourceLanguage);
                string translatedText = await TranslateTextAsync(recognizedText, sourceLanguage, targetLanguage);

                // Display the results in the TextBox
                OutputTextBox.Text = $"Recognized Text:\n{recognizedText}\n\nTranslated Text:\n{translatedText}";
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private void CleanAndPreprocessImage(string inputPath, string outputPath)
        {
            // Explicitly specify SixLabors.ImageSharp.Image
            using (SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(inputPath))
            {
                image.Mutate(x => x.Resize((int)(image.Width * ResizeFactor), (int)(image.Height * ResizeFactor)));
                image.Mutate(x => x.Grayscale());
                image.Mutate(x => x.BinaryThreshold(GlobalThreshold));
                image.Save(outputPath);
            }
        }


        private async Task<string> PerformOcrAsync(string imagePath, string language)
        {
            string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            using var engine = new TesseractEngine(tessdataPath, language, EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            string text = page.GetText();

            // Remove spaces for certain languages (e.g., Japanese)
            if (language == "jpn")
            {
                text = text.Replace(" ", "").Replace("\n", "");
            }

            return text;
        }

        private async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var translator = new GoogleTranslator();
                var result = await translator.TranslateAsync(text, targetLanguage, sourceLanguage);
                return result.Translation;
            }
            catch (Exception ex)
            {
                return $"Translation failed: {ex.Message}";
            }
        }
    }
}
