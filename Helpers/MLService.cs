using Microsoft.ML;
using Microsoft.ML.Data;
using System.Drawing;
using XRayDiagnosticSystem.Models;

#pragma warning disable CA1416

namespace XRayDiagnosticSystem.Helpers
{
    public class MLService
    {
        private readonly MLContext _mlContext;

        public MLService()
        {
            _mlContext = new MLContext(seed: 1);
        }

        public async Task<List<MLPrediction>> PredictAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                var predictions = new List<MLPrediction>();
                
                try 
                {
                    using (var bitmap = new Bitmap(imagePath))
                    {
                        // 1. Anatomy Detection Heuristic
                        string anatomy = DetectAnatomy(bitmap);
                        
                        // 2. Focused Analysis Based on Anatomy
                        if (anatomy == "Chest")
                        {
                            float cloudiness = CalculateDensity(bitmap, 0.2f, 0.2f, 0.4f, 0.7f); 
                            if (cloudiness > 0.75f) predictions.Add(new MLPrediction { Label = "Pulmonary Opacity", Probability = cloudiness, Severity = "Critical", Anatomy = "Chest" });
                            else if (cloudiness > 0.60f) predictions.Add(new MLPrediction { Label = "Mild Haziness", Probability = cloudiness, Severity = "Moderate", Anatomy = "Chest" });
                        }
                        else // Extremity (Hand, Leg, Foot, etc.)
                        {
                            float boneEdges = CalculateEdgeComplexity(bitmap, 0.1f, 0.1f, 0.8f, 0.8f);
                            if (boneEdges > 0.35f) predictions.Add(new MLPrediction { Label = "Structural Bone Displacement", Probability = boneEdges, Severity = "Critical", Anatomy = anatomy });
                            else if (boneEdges > 0.15f) predictions.Add(new MLPrediction { Label = "Potential Fracture Line", Probability = boneEdges, Severity = "Moderate", Anatomy = anatomy });
                        }
                        
                        if (!predictions.Any())
                        {
                            predictions.Add(new MLPrediction { Label = "No Pathological Findings", Probability = 0.99f, Severity = "Normal", Anatomy = anatomy });
                        }
                    }
                }
                catch 
                {
                    predictions.Add(new MLPrediction { Label = "Image Interpretation Error", Probability = 0.5f, Severity = "Moderate", Anatomy = "Unknown" });
                }

                return predictions;
            });
        }

        private string DetectAnatomy(Bitmap bmp)
        {
            // Heuristic A: Corner "Blackness" (Extremities have high-contrast background)
            float upperLeft = CalculateDensity(bmp, 0.05f, 0.05f, 0.15f, 0.15f);
            float upperRight = CalculateDensity(bmp, 0.80f, 0.05f, 0.15f, 0.15f);
            
            // Chest X-rays usually have tissue in at least one upper corner
            // Hand/Ankle X-rays usually have pure black (void) background in corners
            float cornerVoid = (upperLeft + upperRight) / 2.0f;

            // Heuristic B: Aspect Ratio
            float ratio = (float)bmp.Width / bmp.Height;

            if (cornerVoid < 0.25f || ratio < 0.75f) 
            {
                return "Hand/Extremity";
            }
            
            return "Chest";
        }

        private float CalculateDensity(Bitmap bmp, float xP, float yP, float wP, float hP)
        {
            int startX = (int)(bmp.Width * xP);
            int startY = (int)(bmp.Height * yP);
            int width = (int)(bmp.Width * wP);
            int height = (int)(bmp.Height * hP);

            long totalBrightness = 0;
            int count = 0;

            for (int x = startX; x < startX + width && x < bmp.Width; x += 20)
            {
                for (int y = startY; y < startY + height && y < bmp.Height; y += 20)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    totalBrightness += (pixel.R + pixel.G + pixel.B) / 3;
                    count++;
                }
            }
            return count > 0 ? (totalBrightness / (float)count) / 255.0f : 0;
        }

        private float CalculateEdgeComplexity(Bitmap bmp, float xP, float yP, float wP, float hP)
        {
            int startX = (int)(bmp.Width * xP);
            int startY = (int)(bmp.Height * yP);
            int width = (int)(bmp.Width * wP);
            int height = (int)(bmp.Height * hP);

            float variance = 0;
            int count = 0;
            for (int x = startX; x + 5 < startX + width && x + 5 < bmp.Width; x += 30)
            {
                for (int y = startY + 5; y + 10 < startY + height && y + 10 < bmp.Height; y += 30)
                {
                    Color p1 = bmp.GetPixel(x, y);
                    Color p2 = bmp.GetPixel(x + 5, y);
                    int diff = Math.Abs((p1.R + p1.G + p1.B) / 3 - (p2.R + p2.G + p2.B) / 3);
                    if (diff > 40) variance += diff;
                    count++;
                }
            }
            return count > 0 ? (variance / (float)count) / 100.0f : 0;
        }
    }
}
