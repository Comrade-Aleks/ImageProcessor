using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;

public class SmartOrientationDetector
{
    private readonly string tessdataPath;
    private readonly string language;

    public SmartOrientationDetector(string tessdataPath, string language = "eng")
    {
        this.tessdataPath = tessdataPath;
        this.language = language;
    }

    public int DetectBestOrientation(string imagePath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found.", imagePath);

        int bestAngle = 0;
        float highestScore = 0;
        string bestText = "";

        using (var engine = new TesseractEngine(tessdataPath, language, EngineMode.Default))
        {
            int[] baseAngles = { 0, 90, 180, 270 };
            int bestBaseAngle = FindBestAngle(imagePath, engine, baseAngles, ref highestScore, ref bestText);

            if (highestScore >= 2.5) return bestBaseAngle;

            int[] roughAngles = { 30, 45, 60, 120, 135, 150 };
            int bestTiltAngle = FindBestAngle(imagePath, engine, roughAngles, ref highestScore, ref bestText);

            if (bestTiltAngle != bestBaseAngle && Math.Abs(bestTiltAngle - bestBaseAngle) > 15)
            {
                int[] fineAngles = Enumerable.Range(bestTiltAngle - 7, 15)
                    .Where(a => a >= 0 && a < 360)
                    .ToArray();

                bestAngle = FindBestAngle(imagePath, engine, fineAngles, ref highestScore, ref bestText);
            }
            else
            {
                bestAngle = bestBaseAngle;
            }

            bestAngle = (int)(Math.Round(bestAngle / 15.0) * 15);
        }

        return bestAngle;
    }

    private int FindBestAngle(string imagePath, TesseractEngine engine, int[] angles, ref float highestScore, ref string bestText)
    {
        int bestAngle = angles[0];

        foreach (int angle in angles)
        {
            string rotatedPath = RotateImage(imagePath, angle);

            using (var img = Pix.LoadFromFile(rotatedPath))
            using (var page = engine.Process(img, PageSegMode.Auto))
            {
                string text = page.GetText().Trim();
                float confidence = page.GetMeanConfidence();
                int totalLetters = text.Replace(" ", "").Length;

                float score = (confidence * 2) + (totalLetters * 0.5f);

                if (score > highestScore)
                {
                    highestScore = score;
                    bestAngle = angle;
                    bestText = text;
                }
            }
        }
        return bestAngle;
    }

    public string RotateImage(string imagePath, double angle)
    {
        using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath))
        {
            image.Mutate(x => x.Rotate((float)angle));

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"rotated_{Guid.NewGuid()}.png");
            image.Save(tempFilePath);

            // After rotating, fill transparent areas with dominant grayscale color
            FillTransparentBackground(tempFilePath);

            return tempFilePath;
        }
    }

    private void FillTransparentBackground(string imagePath)
    {
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"processed_{Guid.NewGuid()}.png");

        using (Bitmap original = new Bitmap(imagePath))
        {
            using (Bitmap bitmap = new Bitmap(original))
            {
                System.Drawing.Color dominantColor = GetDominantGrayscale(bitmap);
                FillTransparentPixels(bitmap, dominantColor);
                bitmap.Save(tempFilePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // Replace original file with processed image
        File.Delete(imagePath);
        File.Move(tempFilePath, imagePath);
    }


    private System.Drawing.Color GetDominantGrayscale(Bitmap bitmap)
    {
        var grayscaleCounts = new int[256];

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                System.Drawing.Color pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0)
                {
                    int grayscale = (pixel.R + pixel.G + pixel.B) / 3;
                    grayscaleCounts[grayscale]++;
                }
            }
        }

        int dominantGray = Array.IndexOf(grayscaleCounts, grayscaleCounts.Max());
        return System.Drawing.Color.FromArgb(255, dominantGray, dominantGray, dominantGray);
    }

    private void FillTransparentPixels(Bitmap bitmap, System.Drawing.Color fillColor)
    {
        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        int bytes = Math.Abs(data.Stride) * bitmap.Height;
        byte[] pixelBuffer = new byte[bytes];

        // Copy pixel data to array
        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixelBuffer, 0, bytes);

        for (int i = 0; i < pixelBuffer.Length; i += 4) // 4 bytes pr pixel (BGRA format)
        {
            if (pixelBuffer[i + 3] == 0) // Check transparent
            {
                pixelBuffer[i] = fillColor.B;     // Blue
                pixelBuffer[i + 1] = fillColor.G; // Green
                pixelBuffer[i + 2] = fillColor.R; // Red
                pixelBuffer[i + 3] = 255;         // Fully opaque
            }
        }

        // Copy modified data back to bitmap
        System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, 0, data.Scan0, bytes);

        bitmap.UnlockBits(data);
    }

}
