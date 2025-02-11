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
using System.Windows.Forms;
using ImageProcessor;

namespace ImageProcessor
{
    public partial class MainWindow : Window
    {
        private double ResizeFactor = 2.0;
        private float GlobalThreshold = 0.5f;
        string fileName = "captured_region.png";
        private string savedFilePath = "";
        private string sourceLanguage = "";
        private string targetLanguage = "";
        private string recognizedText = "";
        private string translatedText = "";
        public MainWindow()
        {
            InitializeComponent();
            var languageLoader = new LanguageLoading();
            languageLoader.LoadLanguages(FromLanguageComboBox, ToLanguageComboBox);

            FromLanguageComboBox.SelectionChanged += languageLoader.ComboBox_SelectionChanged;
            ToLanguageComboBox.SelectionChanged += languageLoader.ComboBox_SelectionChanged;
        }

        // This function is triggered when the "Select Region" button is clicked
        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            await TakeImage();
        }

        // Main function to process the image: capture, preprocess, OCR, and translate
        private async Task TakeImage()
        {
            try
            {
                // Show the region selection form
                using (var regionSelector = new RegionSelectorForm())
                {
                    if (regionSelector.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Save the captured region to the Img folder
                        
                        savedFilePath = await regionSelector.SaveCapturedRegionAsync(fileName);

                        // Ensure file path is valid
                        if (string.IsNullOrEmpty(savedFilePath) || !File.Exists(savedFilePath))
                        {
                            OutputTextBox.Text = "Error: Captured image file not found.";
                            return;
                        }
                        // Get selected languages from ComboBoxes
                        sourceLanguage = ((ComboBoxItem)FromLanguageComboBox.SelectedItem)?.Tag?.ToString() ?? "";
                        targetLanguage = ((ComboBoxItem)ToLanguageComboBox.SelectedItem)?.Tag?.ToString() ?? "";

                        // Validate language selection
                        if (string.IsNullOrEmpty(sourceLanguage))
                        {
                            OutputTextBox.Text = "Please select a source language.";
                            return;
                        }
                        await ProcessIMG();

                    }
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Error: {ex.Message}";
            }
        }
        private async Task ProcessIMG()
        {


            // Define preprocessed image path
            string preprocessedImagePath = Path.Combine(
                Path.GetDirectoryName(savedFilePath) ?? "",
                $"preprocessed_{Path.GetFileName(savedFilePath)}"
            );

            // Apply image preprocessing
            CleanAndPreprocessImage(savedFilePath, preprocessedImagePath);

            // Perform OCR (extract text from image)
            recognizedText = await PerformOcrAsync(preprocessedImagePath, sourceLanguage);

            // Display results in TextBox
            OutputTextBox.Text = $"Recognized Text:\n{recognizedText}\n\nTranslated Text:\n{translatedText}";
        }

        // Applies image preprocessing (resize, grayscale, threshold)
        private void CleanAndPreprocessImage(string inputPath, string outputPath)
        {
            try
            {
                using (SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(inputPath))
                {
                    image.Mutate(x => x.Resize((int)(image.Width * ResizeFactor), (int)(image.Height * ResizeFactor))
                                       .Grayscale()
                                       .BinaryThreshold(GlobalThreshold));
                    image.Save(outputPath);
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Image processing error: {ex.Message}";
            }
        }

        // Performs OCR (Optical Character Recognition) on the processed image
        private async Task<string> PerformOcrAsync(string imagePath, string language)
        {
            string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            // Ensure tessdata folder exists
            if (!Directory.Exists(tessdataPath))
            {
                OutputTextBox.Text = $"Error: tessdata folder not found at {tessdataPath}";
                return "";
            }

            try
            {
                return await Task.Run(() =>
                {
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
                });
            }
            catch (Exception ex)
            {
                return $"OCR Error: {ex.Message}";
            }
        }

        // This function is triggered when the "Translate" button is clicked
        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            // Translate extracted text
            await TranslateTextAsync();
        }

        // Translates the extracted text using Google Translator
        private async Task<string> TranslateTextAsync()
        {
            sourceLanguage = ((ComboBoxItem)FromLanguageComboBox.SelectedItem)?.Tag?.ToString() ?? "";
            targetLanguage = ((ComboBoxItem)ToLanguageComboBox.SelectedItem)?.Tag?.ToString() ?? "";

            if (string.IsNullOrEmpty(targetLanguage)) 
            {
                OutputTextBox.Text = "Please select a target language.";
                return "Missing target language.";
            }
            try
            {

                var translator = new GoogleTranslator();
                var result = await Task.Run(async () => await translator.TranslateAsync(recognizedText, targetLanguage, sourceLanguage));

                translatedText = result.Translation;

                // Display results in TextBox
                OutputTextBox.Text = $"Recognized Text:\n{recognizedText}\n\nTranslated Text:\n{translatedText}";
                return translatedText;
            }
            catch (Exception ex)
            {
                return $"Translation failed: {ex.Message}";
            }
        }

        // Automatically reprocess the image when preprocessing settings change
        private async void PreprocessingSettingsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            ResizeFactor = ResizeFactorSlider.Value;
            GlobalThreshold = (float)ThresholdSlider.Value;

            // Only reprocess if there's existing text in the output
            if (!string.IsNullOrEmpty(OutputTextBox.Text) && !OutputTextBox.Text.StartsWith("Processing..."))
            {
                await ProcessIMG();
            }
        }
    }
}
