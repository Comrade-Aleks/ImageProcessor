﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
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
        private string[] sourceLanguageTags = new string[2];
        private string[] targetLanguageTags = new string[2];
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



        // Main function to process the image: capture, preprocess, OCR, and translate
        private async Task GetImage()
        {
            try
            {
                savedFilePath = await StartRegionSelector();

                // Ensure image file exists to continue with processing the image
                if (!string.IsNullOrEmpty(savedFilePath) || File.Exists(savedFilePath)) {await ProcessIMG();}
                else {OutputTextBox.Text = "Error: Captured image file not found.";}
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Error: {ex.Message}";
            }
        }

        // Captures the selected region of the screen
        private async Task<string> StartRegionSelector()
        {
            // Show the region selection form
            using (var regionSelector = new RegionSelectorForm())
            {
                // Wait until the region is selected
                if (regionSelector.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Save the captured image to the Img folder
                    return await regionSelector.SaveCapturedRegionAsync(fileName);
                }
            }
            return string.Empty;
        }

        // Get selected languages from ComboBoxes
        private void GetLanguageTags()
        {
            var selectedItem = FromLanguageComboBox.SelectedItem as ComboBoxItem;
            sourceLanguageTags = (selectedItem?.Tag as List<string>)?.ToArray() ?? new string[2];

            selectedItem = ToLanguageComboBox.SelectedItem as ComboBoxItem;
            targetLanguageTags = (selectedItem?.Tag as List<string>)?.ToArray() ?? new string[2];

            // Make sure source language is selected
            if (string.IsNullOrEmpty(sourceLanguageTags[0]))
            {
                OutputTextBox.Text = "Please select a source language.";
                return;
            }
        }
        private async Task ProcessIMG()
        {
            string preprocessedImagePath = Path.Combine(Path.GetDirectoryName(savedFilePath) ?? "", $"preprocessed_{Path.GetFileName(savedFilePath)}");
            CleanAndPreprocessImage(savedFilePath, preprocessedImagePath);

            // Orient the image
            OrientIMG(preprocessedImagePath);

            // Perform OCR (extract text from image)
            GetLanguageTags();
            recognizedText = await PerformOcrAsync(preprocessedImagePath, sourceLanguageTags[0]);

            // Display results in TextBox
            OutputTextBox.Text = $"Recognized Text:\n{recognizedText}\n\nTranslated Text:\n{translatedText}";
        }

        private void OrientIMG(string preprocessedImagePath)
        {
            // Initialize Orientation Detector
            var orientationDetector = new SmartOrientationDetector(AppDomain.CurrentDomain.BaseDirectory + "tessdata");

            // Check if Auto Orientation is enabled
            if (AutoOrientationCheckBox.IsChecked == true)
            {
                // Detect best orientation
                int bestAngle = orientationDetector.DetectBestOrientation(preprocessedImagePath);

                // Rotate the image to correct its orientation and then save it
                if (bestAngle != 0)
                {
                    ManualOrientationSlider.Value = bestAngle;
                    File.Copy(orientationDetector.RotateImage(preprocessedImagePath, bestAngle), preprocessedImagePath, true);
                }
            }
            else
            {
                double bestAngle = ManualOrientationSlider.Value ?? 0;
                File.Copy(orientationDetector.RotateImage(preprocessedImagePath, bestAngle), preprocessedImagePath, true);
            }
        }

        // Applies image preprocessing (resize, grayscale, threshold)
        private void CleanAndPreprocessImage(string inputPath, string outputPath)
        {
            try
            {
                using (SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(inputPath))
                {
                    // Step 1: Resize for better OCR performance
                    image.Mutate(x => x.Resize((int)(image.Width * ResizeFactor), (int)(image.Height * ResizeFactor)));

                    // Step 2: Convert to grayscale and apply thresholding
                    image.Mutate(x => x.Grayscale().BinaryThreshold(GlobalThreshold));

                    // Step 3: Save the preprocessed image (before detecting orientation)
                    image.Save(outputPath);
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Image processing error: {ex.Message}";
            }
        }

        // Performs OCR (Optical Character Recognition) on the processed image
        private async Task<string> PerformOcrAsync(string imagePath, string tessLanguageTag)
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
                    using var engine = new TesseractEngine(tessdataPath, tessLanguageTag, EngineMode.Default);
                    using var img = Pix.LoadFromFile(imagePath);
                    using var page = engine.Process(img);
                    string text = page.GetText();

                    // Remove spaces for certain languages (e.g., Japanese)
                    if (tessLanguageTag == "jpn")
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

        // Translates the extracted text using Google Translator
        private async Task<string> TranslateTextAsync()
        {
            if (string.IsNullOrEmpty(targetLanguageTags[1])) 
            {
                OutputTextBox.Text = "Please select a target language.";
                return "Missing target language.";
            }
            try
            {
                var translator = new GoogleTranslator();
                var result = await Task.Run(async () => await translator.TranslateAsync(recognizedText, targetLanguageTags[1], sourceLanguageTags[1]));

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

        //////////////////////////////// EVENT HANDLERS //////////////////////////////////

        // This function is triggered when the "Select Region" button is clicked
        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            await GetImage();
        }

        // This function is triggered when the "Translate" button is clicked
        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            // Translate extracted text
            GetLanguageTags();
            await TranslateTextAsync();
        }

        // Automatically reprocess the image when preprocessing settings change
        private async void PreprocessingSettingsChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsLoaded) return;

            if (sender == ResizeFactorSlider)
            {
                ResizeFactor = e.NewValue is double newValue ? newValue : 1.0;
            }
            else if (sender == ThresholdSlider)
            {
                GlobalThreshold = e.NewValue is double newThreshold ? (float)newThreshold : 0.5f;
            }

            // Only reprocess if there's existing text in the output
            if (!string.IsNullOrEmpty(OutputTextBox.Text) && !OutputTextBox.Text.StartsWith("Processing..."))
            {
                await ProcessIMG();
            }
        }


        private void FileUploadBorder_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files?.Length > 0)
                {
                    string filePath = files[0];
                    System.Windows.Forms.MessageBox.Show($"File uploaded: {filePath}", "File Upload", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
        }

        private void FileUploadBorder_Click(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Select an Image File"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                System.Windows.Forms.MessageBox.Show($"File selected: {filePath}", "File Selection", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        private void AutoCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Auto Capture feature not yet implemented.", "Auto Capture", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        }
    }
}
