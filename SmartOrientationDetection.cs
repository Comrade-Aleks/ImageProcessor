using System;
using System.IO;
using System.Linq;
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
            // Step 1: Detect major angles (0, 90, 180, 270)
            int[] baseAngles = { 0, 90, 180, 270 };
            int bestBaseAngle = FindBestAngle(imagePath, engine, baseAngles, ref highestScore, ref bestText);

            if (highestScore >= 2.5) return bestBaseAngle;

            // Step 2: Detect possible tilt angles in wider steps
            int[] roughAngles = { 30, 45, 60, 120, 135, 150 };
            int bestTiltAngle = FindBestAngle(imagePath, engine, roughAngles, ref highestScore, ref bestText);

            // Step 3: Fine-tune if a tilt was detected
            if (bestTiltAngle != bestBaseAngle && Math.Abs(bestTiltAngle - bestBaseAngle) > 20)
            {
                int[] fineAngles = Enumerable.Range(bestTiltAngle - 10, 21) // ±10° range
                    .Where(a => a >= 0 && a < 360)
                    .ToArray();

                bestAngle = FindBestAngle(imagePath, engine, fineAngles, ref highestScore, ref bestText);
            }
            else
            {
                bestAngle = bestBaseAngle;
            }
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

                // Scoring heuristic: Confidence + Text Length
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
        using (var image = Image.Load<Rgba32>(imagePath))
        {
            image.Mutate(x => x.Rotate((float)angle));

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"rotated_{Guid.NewGuid()}.png");
            image.Save(tempFilePath);
            return tempFilePath;
        }
    }
}
