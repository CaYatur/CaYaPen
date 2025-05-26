using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace AdvancedScreenPen
{
    public class GestureRecognitionEngine
    {
        private readonly Dictionary<string, GestureTemplate> templates;
        private readonly List<SmartGesture> recentGestures;
        private readonly Random random;

        // Recognition settings
        public float ConfidenceThreshold { get; set; } = 0.6f;
        public int MaxTemplatePoints { get; set; } = 64;
        public float ResampleDistance { get; set; } = 2.0f;
        public bool EnableLearning { get; set; } = true;
        public int MaxRecentGestures { get; set; } = 100;

        // Advanced recognition features
        public bool EnableRotationInvariance { get; set; } = true;
        public bool EnableScaleInvariance { get; set; } = true;
        public bool EnableTranslationInvariance { get; set; } = true;
        public bool EnableSpeedAnalysis { get; set; } = true;
        public bool EnablePressureAnalysis { get; set; } = true;

        // Events
        public event EventHandler<GestureRecognizedEventArgs> GestureRecognized;
        public event EventHandler<GestureLearnedEventArgs> GestureLearned;

        public GestureRecognitionEngine()
        {
            templates = new Dictionary<string, GestureTemplate>();
            recentGestures = new List<SmartGesture>();
            random = new Random();

            InitializeBuiltInTemplates();
        }

        public async Task<GestureRecognitionResult> RecognizeGestureAsync(SmartGesture gesture)
        {
            try
            {
                if (gesture?.Points == null || gesture.Points.Count < 3)
                {
                    return new GestureRecognitionResult
                    {
                        RecognizedShape = "point",
                        Confidence = 0.1f,
                        Gesture = gesture
                    };
                }

                // Preprocess the gesture
                var preprocessedPoints = await PreprocessGestureAsync(gesture.Points);

                // Try multiple recognition algorithms
                var results = new List<GestureRecognitionResult>();

                // Template matching
                var templateResult = await RecognizeUsingTemplatesAsync(preprocessedPoints, gesture);
                if (templateResult != null) results.Add(templateResult);

                // Geometric analysis
                var geometricResult = await RecognizeUsingGeometryAsync(preprocessedPoints, gesture);
                if (geometricResult != null) results.Add(geometricResult);

                // Statistical analysis
                var statisticalResult = await RecognizeUsingStatisticsAsync(preprocessedPoints, gesture);
                if (statisticalResult != null) results.Add(statisticalResult);

                // Neural network recognition (simplified)
                var neuralResult = await RecognizeUsingNeuralNetworkAsync(preprocessedPoints, gesture);
                if (neuralResult != null) results.Add(neuralResult);

                // Combine results
                var bestResult = CombineResults(results);

                // Learn from the gesture if enabled
                if (EnableLearning && bestResult.Confidence > 0.3f)
                {
                    await LearnGestureAsync(gesture, bestResult.RecognizedShape);
                }

                // Store in recent gestures
                AddToRecentGestures(gesture);

                // Fire event
                OnGestureRecognized(bestResult);

                return bestResult;
            }
            catch (Exception ex)
            {
                return new GestureRecognitionResult
                {
                    RecognizedShape = "error",
                    Confidence = 0.0f,
                    Gesture = gesture,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<List<PointF>> PreprocessGestureAsync(List<PointF> points)
        {
            var processed = new List<PointF>(points);

            // Remove duplicate points
            processed = RemoveDuplicatePoints(processed);

            // Resample to uniform distance
            processed = ResamplePoints(processed, ResampleDistance);

            // Apply invariance transformations
            if (EnableTranslationInvariance)
                processed = TranslateToOrigin(processed);

            if (EnableScaleInvariance)
                processed = ScaleToUnitSquare(processed);

            if (EnableRotationInvariance)
                processed = RotateToZero(processed);

            return processed;
        }

        private async Task<GestureRecognitionResult> RecognizeUsingTemplatesAsync(List<PointF> points, SmartGesture gesture)
        {
            var bestMatch = "";
            var bestScore = 0.0f;

            foreach (var template in templates.Values)
            {
                var score = CalculateTemplateScore(points, template.Points);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = template.Name;
                }
            }

            if (bestScore > ConfidenceThreshold)
            {
                return new GestureRecognitionResult
                {
                    RecognizedShape = bestMatch,
                    Confidence = bestScore,
                    Gesture = gesture,
                    Method = "Template Matching"
                };
            }

            return null;
        }

        private async Task<GestureRecognitionResult> RecognizeUsingGeometryAsync(List<PointF> points, SmartGesture gesture)
        {
            var analyzer = new GeometricAnalyzer();
            var features = analyzer.ExtractFeatures(points);

            var recognizedShape = "";
            var confidence = 0.0f;

            // Circle detection
            if (features.Circularity > 0.8f && features.AspectRatio > 0.7f && features.AspectRatio < 1.3f)
            {
                recognizedShape = "circle";
                confidence = features.Circularity;
            }
            // Rectangle detection
            else if (features.Rectangularity > 0.7f && features.CornerCount >= 3 && features.CornerCount <= 5)
            {
                recognizedShape = features.AspectRatio > 0.8f && features.AspectRatio < 1.2f ? "square" : "rectangle";
                confidence = features.Rectangularity;
            }
            // Triangle detection
            else if (features.CornerCount == 3 && features.Triangularity > 0.7f)
            {
                recognizedShape = "triangle";
                confidence = features.Triangularity;
            }
            // Line detection
            else if (features.Linearity > 0.8f)
            {
                recognizedShape = "line";
                confidence = features.Linearity;
            }
            // Star detection
            else if (features.StarLikeness > 0.7f && features.CornerCount >= 8)
            {
                recognizedShape = "star";
                confidence = features.StarLikeness;
            }

            if (confidence > ConfidenceThreshold)
            {
                return new GestureRecognitionResult
                {
                    RecognizedShape = recognizedShape,
                    Confidence = confidence,
                    Gesture = gesture,
                    Method = "Geometric Analysis",
                    Features = features
                };
            }

            return null;
        }

        private async Task<GestureRecognitionResult> RecognizeUsingStatisticsAsync(List<PointF> points, SmartGesture gesture)
        {
            var analyzer = new StatisticalAnalyzer();
            var stats = analyzer.AnalyzeGesture(points, gesture);

            var recognizedShape = "";
            var confidence = 0.0f;

            // Speed-based recognition
            if (EnableSpeedAnalysis && stats.SpeedVariation < 0.3f)
            {
                if (stats.AverageSpeed < 5.0f)
                {
                    recognizedShape = "careful_drawing";
                    confidence = 0.6f;
                }
                else if (stats.AverageSpeed > 20.0f)
                {
                    recognizedShape = "quick_sketch";
                    confidence = 0.6f;
                }
            }

            // Pressure-based recognition
            if (EnablePressureAnalysis && stats.PressureVariation > 0.5f)
            {
                recognizedShape = "expressive_stroke";
                confidence = 0.5f;
            }

            // Pattern recognition based on statistical features
            if (stats.Periodicity > 0.7f)
            {
                recognizedShape = "wave";
                confidence = stats.Periodicity;
            }
            else if (stats.Randomness > 0.8f)
            {
                recognizedShape = "scribble";
                confidence = stats.Randomness;
            }

            if (confidence > ConfidenceThreshold)
            {
                return new GestureRecognitionResult
                {
                    RecognizedShape = recognizedShape,
                    Confidence = confidence,
                    Gesture = gesture,
                    Method = "Statistical Analysis",
                    Statistics = stats
                };
            }

            return null;
        }

        private async Task<GestureRecognitionResult> RecognizeUsingNeuralNetworkAsync(List<PointF> points, SmartGesture gesture)
        {
            // Simplified neural network recognition
            // In a real implementation, this would use a trained neural network

            var features = ExtractNeuralFeatures(points);
            var networkOutput = await SimulateNeuralNetworkAsync(features);

            var bestClass = 0;
            var bestConfidence = 0.0f;

            for (int i = 0; i < networkOutput.Length; i++)
            {
                if (networkOutput[i] > bestConfidence)
                {
                    bestConfidence = networkOutput[i];
                    bestClass = i;
                }
            }

            var shapeNames = new[] { "circle", "rectangle", "triangle", "line", "star", "heart", "arrow", "spiral" };

            if (bestConfidence > ConfidenceThreshold && bestClass < shapeNames.Length)
            {
                return new GestureRecognitionResult
                {
                    RecognizedShape = shapeNames[bestClass],
                    Confidence = bestConfidence,
                    Gesture = gesture,
                    Method = "Neural Network"
                };
            }

            return null;
        }

        private GestureRecognitionResult CombineResults(List<GestureRecognitionResult> results)
        {
            if (results.Count == 0)
            {
                return new GestureRecognitionResult
                {
                    RecognizedShape = "unknown",
                    Confidence = 0.0f,
                    Method = "No Recognition"
                };
            }

            if (results.Count == 1)
                return results[0];

            // Weight results by confidence and method reliability
            var weights = new Dictionary<string, float>
            {
                ["Template Matching"] = 1.0f,
                ["Geometric Analysis"] = 0.9f,
                ["Statistical Analysis"] = 0.7f,
                ["Neural Network"] = 0.8f
            };

            var weightedResults = results
                .Select(r => new
                {
                    Result = r,
                    WeightedConfidence = r.Confidence * weights.GetValueOrDefault(r.Method, 0.5f)
                })
                .OrderByDescending(r => r.WeightedConfidence)
                .ToList();

            var bestResult = weightedResults.First().Result;

            // If multiple results agree, boost confidence
            var agreementCount = results.Count(r => r.RecognizedShape == bestResult.RecognizedShape);
            if (agreementCount > 1)
            {
                bestResult.Confidence = Math.Min(1.0f, bestResult.Confidence * (1 + 0.2f * (agreementCount - 1)));
                bestResult.Method += $" (+{agreementCount - 1} agreement)";
            }

            return bestResult;
        }

        private async Task LearnGestureAsync(SmartGesture gesture, string recognizedShape)
        {
            try
            {
                // Update or create template
                if (templates.ContainsKey(recognizedShape))
                {
                    var template = templates[recognizedShape];
                    template.UpdateTemplate(gesture.Points);
                    template.UsageCount++;
                }
                else
                {
                    var newTemplate = new GestureTemplate
                    {
                        Name = recognizedShape,
                        Points = new List<PointF>(gesture.Points),
                        CreatedDate = DateTime.Now,
                        UsageCount = 1
                    };

                    templates[recognizedShape] = newTemplate;
                }

                OnGestureLearned(recognizedShape, gesture);
            }
            catch (Exception ex)
            {
                // Log learning error
            }
        }

        private void AddToRecentGestures(SmartGesture gesture)
        {
            recentGestures.Add(gesture);

            if (recentGestures.Count > MaxRecentGestures)
            {
                recentGestures.RemoveAt(0);
            }
        }

        private void InitializeBuiltInTemplates()
        {
            // Initialize common shape templates
            templates["circle"] = CreateCircleTemplate();
            templates["rectangle"] = CreateRectangleTemplate();
            templates["triangle"] = CreateTriangleTemplate();
            templates["line"] = CreateLineTemplate();
            templates["star"] = CreateStarTemplate();
            templates["heart"] = CreateHeartTemplate();
            templates["arrow"] = CreateArrowTemplate();
            templates["spiral"] = CreateSpiralTemplate();
        }

        private GestureTemplate CreateCircleTemplate()
        {
            var points = new List<PointF>();
            var center = new PointF(0, 0);
            var radius = 50;

            for (int i = 0; i < 32; i++)
            {
                var angle = 2 * Math.PI * i / 32;
                points.Add(new PointF(
                    center.X + (float)(radius * Math.Cos(angle)),
                    center.Y + (float)(radius * Math.Sin(angle))
                ));
            }

            return new GestureTemplate
            {
                Name = "circle",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        private GestureTemplate CreateRectangleTemplate()
        {
            var points = new List<PointF>
            {
                new PointF(-50, -30), new PointF(50, -30),
                new PointF(50, 30), new PointF(-50, 30),
                new PointF(-50, -30)
            };

            return new GestureTemplate
            {
                Name = "rectangle",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        private GestureTemplate CreateTriangleTemplate()
        {
            var points = new List<PointF>
            {
                new PointF(0, -50),
                new PointF(-43, 25),
                new PointF(43, 25),
                new PointF(0, -50)
            };

            return new GestureTemplate
            {
                Name = "triangle",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        private GestureTemplate CreateLineTemplate()
        {
            var points = new List<PointF>
            {
                new PointF(-50, 0),
                new PointF(50, 0)
            };

            return new GestureTemplate
            {
                Name = "line",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        private GestureTemplate CreateStarTemplate()
        {
            var points = new List<PointF>();
            var outerRadius = 50;
            var innerRadius = 20;

            for (int i = 0; i < 10; i++)
            {
                var angle = Math.PI * i / 5;
                var radius = (i % 2 == 0) ? outerRadius : innerRadius;
                points.Add(new PointF(
                    (float)(radius * Math.Cos(angle)),
                    (float)(radius * Math.Sin(angle))
                ));
            }
            points.Add(points[0]); // Close the shape

            return new GestureTemplate
            {
                Name = "star",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        private GestureTemplate CreateHeartTemplate()
        {
            var points = new List<PointF>();

            // Generate heart shape using parametric equations
            for (float t = 0; t < 2 * Math.PI; t += 0.1f)
            {
                var x = 16 * Math.Pow(Math.Sin(t), 3);
                var y = -(13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t));
                points.Add(new PointF((float)x, (float)y));
            }

            return new GestureTemplate
            {
                Name = "heart",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        private GestureTemplate CreateArrowTemplate()
        {
            var points = new List<PointF>
            {
                new PointF(-40, 0),
                new PointF(30, 0),
                new PointF(20, -15),
                new PointF(30, 0),
                new PointF(20, 15)
            };

            return new GestureTemplate
            {
                Name = "arrow",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        private GestureTemplate CreateSpiralTemplate()
        {
            var points = new List<PointF>();

            for (float t = 0; t < 4 * Math.PI; t += 0.2f)
            {
                var radius = t * 3;
                points.Add(new PointF(
                    (float)(radius * Math.Cos(t)),
                    (float)(radius * Math.Sin(t))
                ));
            }

            return new GestureTemplate
            {
                Name = "spiral",
                Points = points,
                CreatedDate = DateTime.Now,
                IsBuiltIn = true
            };
        }

        // Utility methods
        private List<PointF> RemoveDuplicatePoints(List<PointF> points)
        {
            var result = new List<PointF>();
            var threshold = 1.0f;

            if (points.Count > 0)
                result.Add(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                var distance = Distance(points[i], points[i - 1]);
                if (distance > threshold)
                {
                    result.Add(points[i]);
                }
            }

            return result;
        }

        private List<PointF> ResamplePoints(List<PointF> points, float distance)
        {
            if (points.Count < 2) return points;

            var result = new List<PointF> { points[0] };
            var totalDistance = 0.0f;

            for (int i = 1; i < points.Count; i++)
            {
                var segmentDistance = Distance(points[i], points[i - 1]);
                totalDistance += segmentDistance;

                while (totalDistance >= distance)
                {
                    var ratio = (totalDistance - distance) / segmentDistance;
                    var newPoint = new PointF(
                        points[i].X - ratio * (points[i].X - points[i - 1].X),
                        points[i].Y - ratio * (points[i].Y - points[i - 1].Y)
                    );
                    result.Add(newPoint);
                    totalDistance -= distance;
                }
            }

            return result;
        }

        private List<PointF> TranslateToOrigin(List<PointF> points)
        {
            if (points.Count == 0) return points;

            var centroid = CalculateCentroid(points);
            return points.Select(p => new PointF(p.X - centroid.X, p.Y - centroid.Y)).ToList();
        }

        private List<PointF> ScaleToUnitSquare(List<PointF> points)
        {
            if (points.Count == 0) return points;

            var bounds = CalculateBounds(points);
            var scale = Math.Max(bounds.Width, bounds.Height);

            if (scale == 0) return points;

            return points.Select(p => new PointF(p.X / scale, p.Y / scale)).ToList();
        }

        private List<PointF> RotateToZero(List<PointF> points)
        {
            if (points.Count < 2) return points;

            var centroid = CalculateCentroid(points);
            var angle = Math.Atan2(points[0].Y - centroid.Y, points[0].X - centroid.X);

            var cos = (float)Math.Cos(-angle);
            var sin = (float)Math.Sin(-angle);

            return points.Select(p => new PointF(
                p.X * cos - p.Y * sin,
                p.X * sin + p.Y * cos
            )).ToList();
        }

        private float CalculateTemplateScore(List<PointF> points1, List<PointF> points2)
        {
            if (points1.Count == 0 || points2.Count == 0) return 0;

            // Use Dynamic Time Warping for better matching
            return CalculateDTWScore(points1, points2);
        }

        private float CalculateDTWScore(List<PointF> seq1, List<PointF> seq2)
        {
            var n = seq1.Count;
            var m = seq2.Count;
            var dtw = new float[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                dtw[i, 0] = float.MaxValue;
            for (int j = 0; j <= m; j++)
                dtw[0, j] = float.MaxValue;
            dtw[0, 0] = 0;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    var cost = Distance(seq1[i - 1], seq2[j - 1]);
                    dtw[i, j] = cost + Math.Min(Math.Min(dtw[i - 1, j], dtw[i, j - 1]), dtw[i - 1, j - 1]);
                }
            }

            var maxDistance = Math.Max(n, m) * 100; // Normalize
            return Math.Max(0, 1 - dtw[n, m] / maxDistance);
        }

        private float[] ExtractNeuralFeatures(List<PointF> points)
        {
            // Extract features for neural network
            var features = new List<float>();

            if (points.Count == 0) return new float[64];

            var bounds = CalculateBounds(points);
            var centroid = CalculateCentroid(points);

            // Basic geometric features
            features.Add(bounds.Width / bounds.Height); // Aspect ratio
            features.Add(CalculatePerimeter(points));
            features.Add(CalculateArea(points));
            features.Add(points.Count);

            // Directional features (8 directions)
            for (int i = 0; i < 8; i++)
            {
                features.Add(CalculateDirectionalFeature(points, i * Math.PI / 4));
            }

            // Distance features from centroid (normalized)
            var distances = points.Select(p => Distance(p, centroid)).ToList();
            features.Add(distances.Average());
            features.Add(distances.Max() - distances.Min());

            // Angle features
            var angles = CalculateAngles(points);
            if (angles.Count > 0)
            {
                features.Add(angles.Average());
                features.Add(angles.Max() - angles.Min());
            }
            else
            {
                features.Add(0);
                features.Add(0);
            }

            // Pad or truncate to 64 features
            while (features.Count < 64)
                features.Add(0);

            return features.Take(64).ToArray();
        }

        private async Task<float[]> SimulateNeuralNetworkAsync(float[] features)
        {
            // Simplified neural network simulation
            // In a real implementation, this would be a trained neural network

            var output = new float[8]; // 8 shape classes

            // Simple weighted sum simulation
            for (int i = 0; i < output.Length; i++)
            {
                var sum = 0.0f;
                for (int j = 0; j < features.Length; j++)
                {
                    // Use pseudo-random weights based on feature and class index
                    var weight = (float)Math.Sin(i * 13 + j * 17) * 0.1f;
                    sum += features[j] * weight;
                }
                output[i] = 1.0f / (1.0f + (float)Math.Exp(-sum)); // Sigmoid activation
            }

            return output;
        }

        // Utility methods
        private float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private PointF CalculateCentroid(List<PointF> points)
        {
            if (points.Count == 0) return PointF.Empty;

            var x = points.Average(p => p.X);
            var y = points.Average(p => p.Y);
            return new PointF(x, y);
        }

        private RectangleF CalculateBounds(List<PointF> points)
        {
            if (points.Count == 0) return RectangleF.Empty;

            var minX = points.Min(p => p.X);
            var maxX = points.Max(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxY = points.Max(p => p.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        private float CalculatePerimeter(List<PointF> points)
        {
            if (points.Count < 2) return 0;

            var perimeter = 0.0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                perimeter += Distance(points[i], points[i + 1]);
            }

            return perimeter;
        }

        private float CalculateArea(List<PointF> points)
        {
            if (points.Count < 3) return 0;

            var area = 0.0f;
            for (int i = 0; i < points.Count; i++)
            {
                int j = (i + 1) % points.Count;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }

            return Math.Abs(area) / 2;
        }

        private List<float> CalculateAngles(List<PointF> points)
        {
            var angles = new List<float>();

            for (int i = 1; i < points.Count - 1; i++)
            {
                var v1 = new PointF(points[i].X - points[i - 1].X, points[i].Y - points[i - 1].Y);
                var v2 = new PointF(points[i + 1].X - points[i].X, points[i + 1].Y - points[i].Y);

                var angle = Math.Atan2(v2.Y, v2.X) - Math.Atan2(v1.Y, v1.X);
                angles.Add((float)angle);
            }

            return angles;
        }

        private float CalculateDirectionalFeature(List<PointF> points, double direction)
        {
            if (points.Count < 2) return 0;

            var count = 0;
            var cos = Math.Cos(direction);
            var sin = Math.Sin(direction);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var dx = points[i + 1].X - points[i].X;
                var dy = points[i + 1].Y - points[i].Y;

                var dot = dx * cos + dy * sin;
                if (dot > 0) count++;
            }

            return (float)count / (points.Count - 1);
        }

        // Event methods
        protected virtual void OnGestureRecognized(GestureRecognitionResult result)
        {
            GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs { Result = result });
        }

        protected virtual void OnGestureLearned(string shapeName, SmartGesture gesture)
        {
            GestureLearned?.Invoke(this, new GestureLearnedEventArgs { ShapeName = shapeName, Gesture = gesture });
        }
    }

    // Supporting classes
    public class GestureTemplate
    {
        public string Name { get; set; }
        public List<PointF> Points { get; set; } = new List<PointF>();
        public DateTime CreatedDate { get; set; }
        public DateTime LastUsed { get; set; }
        public int UsageCount { get; set; }
        public bool IsBuiltIn { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public void UpdateTemplate(List<PointF> newPoints)
        {
            // Simple template update - average with existing points
            if (Points.Count == 0)
            {
                Points = new List<PointF>(newPoints);
            }
            else
            {
                // Weighted average based on usage count
                var weight = 1.0f / (UsageCount + 1);

                for (int i = 0; i < Math.Min(Points.Count, newPoints.Count); i++)
                {
                    Points[i] = new PointF(
                        Points[i].X * (1 - weight) + newPoints[i].X * weight,
                        Points[i].Y * (1 - weight) + newPoints[i].Y * weight
                    );
                }
            }

            LastUsed = DateTime.Now;
        }
    }

    public class GestureRecognitionResult
    {
        public string RecognizedShape { get; set; }
        public float Confidence { get; set; }
        public SmartGesture Gesture { get; set; }
        public string Method { get; set; }
        public GeometricFeatures Features { get; set; }
        public StatisticalFeatures Statistics { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    public class GeometricFeatures
    {
        public float Circularity { get; set; }
        public float Rectangularity { get; set; }
        public float Triangularity { get; set; }
        public float Linearity { get; set; }
        public float StarLikeness { get; set; }
        public float AspectRatio { get; set; }
        public int CornerCount { get; set; }
        public float Symmetry { get; set; }
        public float Convexity { get; set; }
        public float Compactness { get; set; }
    }

    public class StatisticalFeatures
    {
        public float AverageSpeed { get; set; }
        public float SpeedVariation { get; set; }
        public float AveragePressure { get; set; }
        public float PressureVariation { get; set; }
        public float Periodicity { get; set; }
        public float Randomness { get; set; }
        public float Smoothness { get; set; }
        public List<float> DirectionChanges { get; set; } = new List<float>();
    }

    public class GeometricAnalyzer
    {
        public GeometricFeatures ExtractFeatures(List<PointF> points)
        {
            if (points == null || points.Count < 3)
                return new GeometricFeatures();

            var features = new GeometricFeatures();

            features.Circularity = CalculateCircularity(points);
            features.Rectangularity = CalculateRectangularity(points);
            features.Triangularity = CalculateTriangularity(points);
            features.Linearity = CalculateLinearity(points);
            features.StarLikeness = CalculateStarLikeness(points);
            features.AspectRatio = CalculateAspectRatio(points);
            features.CornerCount = CountCorners(points);
            features.Symmetry = CalculateSymmetry(points);
            features.Convexity = CalculateConvexity(points);
            features.Compactness = CalculateCompactness(points);

            return features;
        }

        private float CalculateCircularity(List<PointF> points)
        {
            var centroid = CalculateCentroid(points);
            var distances = points.Select(p => Distance(p, centroid)).ToList();
            var avgRadius = distances.Average();

            if (avgRadius == 0) return 0;

            var variance = distances.Sum(d => Math.Pow(d - avgRadius, 2)) / distances.Count;
            var stdDev = Math.Sqrt(variance);

            return Math.Max(0, 1 - (float)(stdDev / avgRadius));
        }

        private float CalculateRectangularity(List<PointF> points)
        {
            var corners = FindCorners(points);
            if (corners.Count < 3 || corners.Count > 6) return 0;

            if (corners.Count == 4)
            {
                // Check if angles are close to 90 degrees
                var angles = CalculateCornerAngles(corners);
                var rightAngleScore = angles.Average(a => 1.0f - Math.Min(1.0f, Math.Abs(a - 90) / 45.0f));

                // Check if opposite sides are similar
                var sides = CalculateSideLengths(corners);
                var sideRatio1 = Math.Min(sides[0], sides[2]) / Math.Max(sides[0], sides[2]);
                var sideRatio2 = Math.Min(sides[1], sides[3]) / Math.Max(sides[1], sides[3]);
                var sideScore = (sideRatio1 + sideRatio2) / 2;

                return (rightAngleScore + sideScore) / 2;
            }

            return 0;
        }

        private float CalculateTriangularity(List<PointF> points)
        {
            var corners = FindCorners(points);
            if (corners.Count != 3) return 0;

            var angles = CalculateCornerAngles(corners);
            var angleSum = angles.Sum();

            // Triangle angles should sum to 180 degrees
            return Math.Max(0, 1 - Math.Abs(angleSum - 180) / 180);
        }

        private float CalculateLinearity(List<PointF> points)
        {
            if (points.Count < 2) return 0;

            var start = points[0];
            var end = points[points.Count - 1];

            var totalDeviation = 0.0f;
            foreach (var point in points)
            {
                totalDeviation += DistanceToLine(point, start, end);
            }

            var avgDeviation = totalDeviation / points.Count;
            var lineLength = Distance(start, end);

            if (lineLength == 0) return 0;

            return Math.Max(0, 1 - avgDeviation / (lineLength * 0.1f));
        }

        private float CalculateStarLikeness(List<PointF> points)
        {
            var centroid = CalculateCentroid(points);
            var corners = FindCorners(points);

            if (corners.Count < 8) return 0;

            // Check for alternating inner and outer points
            var radii = corners.Select(c => Distance(c, centroid)).ToList();

            var innerCount = 0;
            var outerCount = 0;
            var avgRadius = radii.Average();

            foreach (var radius in radii)
            {
                if (radius < avgRadius * 0.7f)
                    innerCount++;
                else if (radius > avgRadius * 1.3f)
                    outerCount++;
            }

            var ratio = Math.Min(innerCount, outerCount) / (float)Math.Max(innerCount, outerCount);
            return ratio > 0.5f ? ratio : 0;
        }

        private float CalculateAspectRatio(List<PointF> points)
        {
            var bounds = CalculateBounds(points);
            if (bounds.Height == 0) return float.MaxValue;
            return bounds.Width / bounds.Height;
        }

        private int CountCorners(List<PointF> points)
        {
            return FindCorners(points).Count;
        }

        private float CalculateSymmetry(List<PointF> points)
        {
            var centroid = CalculateCentroid(points);
            var symmetryScore = 0.0f;
            var comparisons = 0;

            // Test horizontal symmetry
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var mirrorPoint = new PointF(2 * centroid.X - point.X, point.Y);
                var minDistance = points.Min(p => Distance(p, mirrorPoint));

                symmetryScore += 1.0f / (1.0f + minDistance);
                comparisons++;
            }

            return comparisons > 0 ? symmetryScore / comparisons : 0;
        }

        private float CalculateConvexity(List<PointF> points)
        {
            if (points.Count < 3) return 0;

            var convexHull = ComputeConvexHull(points);
            var hullArea = CalculatePolygonArea(convexHull);
            var actualArea = CalculatePolygonArea(points);

            if (hullArea == 0) return 0;
            return actualArea / hullArea;
        }

        private float CalculateCompactness(List<PointF> points)
        {
            var area = CalculatePolygonArea(points);
            var perimeter = CalculatePerimeter(points);

            if (perimeter == 0) return 0;

            // Compactness = 4π * Area / Perimeter²
            return (float)(4 * Math.PI * area / (perimeter * perimeter));
        }

        // Helper methods
        private List<PointF> FindCorners(List<PointF> points)
        {
            var corners = new List<PointF>();
            var threshold = Math.PI / 4; // 45 degrees

            for (int i = 1; i < points.Count - 1; i++)
            {
                var angle = CalculateAngle(points[i - 1], points[i], points[i + 1]);
                if (Math.Abs(angle) > threshold)
                {
                    corners.Add(points[i]);
                }
            }

            return corners;
        }

        private List<float> CalculateCornerAngles(List<PointF> corners)
        {
            var angles = new List<float>();

            for (int i = 0; i < corners.Count; i++)
            {
                var p1 = corners[i];
                var p2 = corners[(i + 1) % corners.Count];
                var p3 = corners[(i + 2) % corners.Count];

                var angle = CalculateAngle(p1, p2, p3) * 180 / Math.PI;
                angles.Add(Math.Abs((float)angle));
            }

            return angles;
        }

        private List<float> CalculateSideLengths(List<PointF> corners)
        {
            var sides = new List<float>();

            for (int i = 0; i < corners.Count; i++)
            {
                var p1 = corners[i];
                var p2 = corners[(i + 1) % corners.Count];
                sides.Add(Distance(p1, p2));
            }

            return sides;
        }

        private List<PointF> ComputeConvexHull(List<PointF> points)
        {
            // Graham scan algorithm (simplified)
            var sortedPoints = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            var hull = new List<PointF>();

            // Build lower hull
            foreach (var point in sortedPoints)
            {
                while (hull.Count >= 2 &&
                       CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }

            // Build upper hull
            var t = hull.Count + 1;
            for (int i = sortedPoints.Count - 2; i >= 0; i--)
            {
                var point = sortedPoints[i];
                while (hull.Count >= t &&
                       CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }

            hull.RemoveAt(hull.Count - 1); // Remove duplicate point
            return hull;
        }

        private float CrossProduct(PointF o, PointF a, PointF b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }

        private float CalculateAngle(PointF p1, PointF p2, PointF p3)
        {
            var v1 = new PointF(p1.X - p2.X, p1.Y - p2.Y);
            var v2 = new PointF(p3.X - p2.X, p3.Y - p2.Y);

            var dot = v1.X * v2.X + v1.Y * v2.Y;
            var cross = v1.X * v2.Y - v1.Y * v2.X;

            return (float)Math.Atan2(cross, dot);
        }

        private float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private float DistanceToLine(PointF point, PointF lineStart, PointF lineEnd)
        {
            var A = point.X - lineStart.X;
            var B = point.Y - lineStart.Y;
            var C = lineEnd.X - lineStart.X;
            var D = lineEnd.Y - lineStart.Y;

            var dot = A * C + B * D;
            var lenSq = C * C + D * D;

            if (lenSq == 0) return Distance(point, lineStart);

            var param = dot / lenSq;

            PointF projection;
            if (param < 0)
                projection = lineStart;
            else if (param > 1)
                projection = lineEnd;
            else
                projection = new PointF(lineStart.X + param * C, lineStart.Y + param * D);

            return Distance(point, projection);
        }

        private PointF CalculateCentroid(List<PointF> points)
        {
            if (points.Count == 0) return PointF.Empty;

            var x = points.Average(p => p.X);
            var y = points.Average(p => p.Y);
            return new PointF(x, y);
        }

        private RectangleF CalculateBounds(List<PointF> points)
        {
            if (points.Count == 0) return RectangleF.Empty;

            var minX = points.Min(p => p.X);
            var maxX = points.Max(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxY = points.Max(p => p.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        private float CalculatePolygonArea(List<PointF> points)
        {
            if (points.Count < 3) return 0;

            var area = 0.0f;
            for (int i = 0; i < points.Count; i++)
            {
                int j = (i + 1) % points.Count;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }

            return Math.Abs(area) / 2;
        }

        private float CalculatePerimeter(List<PointF> points)
        {
            if (points.Count < 2) return 0;

            var perimeter = 0.0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                perimeter += Distance(points[i], points[i + 1]);
            }

            return perimeter;
        }
    }

    public class StatisticalAnalyzer
    {
        public StatisticalFeatures AnalyzeGesture(List<PointF> points, SmartGesture gesture)
        {
            var features = new StatisticalFeatures();

            if (points == null || points.Count < 2)
                return features;

            features.AverageSpeed = CalculateAverageSpeed(points, gesture);
            features.SpeedVariation = CalculateSpeedVariation(points, gesture);
            features.AveragePressure = CalculateAveragePressure(gesture);
            features.PressureVariation = CalculatePressureVariation(gesture);
            features.Periodicity = CalculatePeriodicity(points);
            features.Randomness = CalculateRandomness(points);
            features.Smoothness = CalculateSmoothness(points);
            features.DirectionChanges = CalculateDirectionChanges(points);

            return features;
        }

        private float CalculateAverageSpeed(List<PointF> points, SmartGesture gesture)
        {
            if (points.Count < 2) return 0;

            var totalDistance = 0.0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                totalDistance += Distance(points[i], points[i + 1]);
            }

            var totalTime = (gesture.EndTime - gesture.StartTime).TotalMilliseconds;
            return totalTime > 0 ? totalDistance / (float)totalTime : 0;
        }

        private float CalculateSpeedVariation(List<PointF> points, SmartGesture gesture)
        {
            if (points.Count < 3) return 0;

            var speeds = new List<float>();
            var timePerSegment = (gesture.EndTime - gesture.StartTime).TotalMilliseconds / (points.Count - 1);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var distance = Distance(points[i], points[i + 1]);
                var speed = timePerSegment > 0 ? distance / (float)timePerSegment : 0;
                speeds.Add(speed);
            }

            if (speeds.Count == 0) return 0;

            var avgSpeed = speeds.Average();
            var variance = speeds.Sum(s => Math.Pow(s - avgSpeed, 2)) / speeds.Count;

            return avgSpeed > 0 ? (float)Math.Sqrt(variance) / avgSpeed : 0;
        }

        private float CalculateAveragePressure(SmartGesture gesture)
        {
            // This would be implemented if pressure data is available
            return 1.0f; // Default pressure
        }

        private float CalculatePressureVariation(SmartGesture gesture)
        {
            // This would be implemented if pressure data is available
            return 0.0f; // No variation by default
        }

        private float CalculatePeriodicity(List<PointF> points)
        {
            if (points.Count < 10) return 0;

            // Simple autocorrelation to detect periodic patterns
            var yValues = points.Select(p => p.Y).ToArray();
            var maxCorrelation = 0.0f;

            for (int lag = 1; lag < yValues.Length / 3; lag++)
            {
                var correlation = CalculateAutocorrelation(yValues, lag);
                maxCorrelation = Math.Max(maxCorrelation, Math.Abs(correlation));
            }

            return maxCorrelation;
        }

        private float CalculateRandomness(List<PointF> points)
        {
            if (points.Count < 3) return 0;

            var directions = new List<float>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var dx = points[i + 1].X - points[i].X;
                var dy = points[i + 1].Y - points[i].Y;
                var direction = (float)Math.Atan2(dy, dx);
                directions.Add(direction);
            }

            // Calculate entropy of direction changes
            var directionBins = new int[8]; // 8 direction bins
            foreach (var dir in directions)
            {
                var bin = (int)((dir + Math.PI) / (2 * Math.PI) * 8) % 8;
                directionBins[bin]++;
            }

            var entropy = 0.0f;
            var total = directions.Count;

            foreach (var count in directionBins)
            {
                if (count > 0)
                {
                    var probability = (float)count / total;
                    entropy -= probability * (float)Math.Log(probability, 2);
                }
            }

            return entropy / 3.0f; // Normalize to 0-1 range
        }

        private float CalculateSmoothness(List<PointF> points)
        {
            if (points.Count < 3) return 1;

            var curvatures = new List<float>();

            for (int i = 1; i < points.Count - 1; i++)
            {
                var curvature = CalculateCurvature(points[i - 1], points[i], points[i + 1]);
                curvatures.Add(Math.Abs(curvature));
            }

            var avgCurvature = curvatures.Average();
            return 1.0f / (1.0f + avgCurvature);
        }

        private List<float> CalculateDirectionChanges(List<PointF> points)
        {
            var changes = new List<float>();

            for (int i = 1; i < points.Count - 1; i++)
            {
                var angle1 = Math.Atan2(points[i].Y - points[i - 1].Y, points[i].X - points[i - 1].X);
                var angle2 = Math.Atan2(points[i + 1].Y - points[i].Y, points[i + 1].X - points[i].X);

                var change = angle2 - angle1;

                // Normalize to [-π, π]
                while (change > Math.PI) change -= 2 * Math.PI;
                while (change < -Math.PI) change += 2 * Math.PI;

                changes.Add(Math.Abs((float)change));
            }

            return changes;
        }

        private float CalculateAutocorrelation(float[] data, int lag)
        {
            if (lag >= data.Length) return 0;

            var n = data.Length - lag;
            var mean = data.Average();

            var numerator = 0.0f;
            var denominator = 0.0f;

            for (int i = 0; i < n; i++)
            {
                numerator += (data[i] - mean) * (data[i + lag] - mean);
            }

            for (int i = 0; i < data.Length; i++)
            {
                denominator += (data[i] - mean) * (data[i] - mean);
            }

            return denominator != 0 ? numerator / denominator : 0;
        }

        private float CalculateCurvature(PointF p1, PointF p2, PointF p3)
        {
            var v1 = new PointF(p2.X - p1.X, p2.Y - p1.Y);
            var v2 = new PointF(p3.X - p2.X, p3.Y - p2.Y);

            var cross = v1.X * v2.Y - v1.Y * v2.X;
            var v1Length = (float)Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
            var v2Length = (float)Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);

            if (v1Length == 0 || v2Length == 0) return 0;

            return cross / (v1Length * v2Length);
        }

        private float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
    }

    // Event argument classes
    public class GestureRecognizedEventArgs : EventArgs
    {
        public GestureRecognitionResult Result { get; set; }
    }

    public class GestureLearnedEventArgs : EventArgs
    {
        public string ShapeName { get; set; }
        public SmartGesture Gesture { get; set; }
    }
}