using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AdvancedScreenPen
{
    public class SmartGesture
    {
        public List<PointF> Points { get; set; } = new List<PointF>();
        public string RecognizedShape { get; set; } = "";
        public float Confidence { get; set; } = 0.0f;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now;
        public GestureType Type { get; set; } = GestureType.Unknown;
        public Dictionary<string, float> ShapeProbabilities { get; set; } = new Dictionary<string, float>();

        // Gesture characteristics
        public float Speed { get; set; } = 0.0f;
        public float TotalLength { get; set; } = 0.0f;
        public RectangleF BoundingBox { get; set; } = RectangleF.Empty;
        public PointF Centroid { get; set; } = PointF.Empty;
        public float AspectRatio { get; set; } = 1.0f;
        public float Curvature { get; set; } = 0.0f;
        public int DirectionChanges { get; set; } = 0;

        // Advanced properties
        public bool IsClockwise { get; set; } = false;
        public float Smoothness { get; set; } = 0.0f;
        public float Symmetry { get; set; } = 0.0f;
        public List<float> Angles { get; set; } = new List<float>();
        public List<float> Distances { get; set; } = new List<float>();

        public SmartGesture()
        {
            StartTime = DateTime.Now;
        }

        public void AddPoint(PointF point)
        {
            Points.Add(point);
            UpdateCharacteristics();
        }

        public void FinishGesture()
        {
            EndTime = DateTime.Now;
            AnalyzeGesture();
        }

        private void UpdateCharacteristics()
        {
            if (Points.Count < 2) return;

            // Calculate speed
            var lastPoint = Points[Points.Count - 1];
            var secondLastPoint = Points[Points.Count - 2];
            var distance = Distance(lastPoint, secondLastPoint);
            Speed = distance / Math.Max(1, (float)(DateTime.Now - StartTime).TotalMilliseconds);

            // Update total length
            TotalLength += distance;

            // Update bounding box
            UpdateBoundingBox();

            // Update centroid
            UpdateCentroid();
        }

        private void UpdateBoundingBox()
        {
            if (Points.Count == 0) return;

            float minX = Points.Min(p => p.X);
            float maxX = Points.Max(p => p.X);
            float minY = Points.Min(p => p.Y);
            float maxY = Points.Max(p => p.Y);

            BoundingBox = new RectangleF(minX, minY, maxX - minX, maxY - minY);
            AspectRatio = BoundingBox.Width / Math.Max(BoundingBox.Height, 0.1f);
        }

        private void UpdateCentroid()
        {
            if (Points.Count == 0) return;

            float x = Points.Average(p => p.X);
            float y = Points.Average(p => p.Y);
            Centroid = new PointF(x, y);
        }

        private void AnalyzeGesture()
        {
            if (Points.Count < 3)
            {
                Type = GestureType.Point;
                RecognizedShape = "point";
                return;
            }

            CalculateAdvancedCharacteristics();
            RecognizeShape();
        }

        private void CalculateAdvancedCharacteristics()
        {
            CalculateAngles();
            CalculateDistances();
            CalculateCurvature();
            CalculateDirectionChanges();
            CalculateClockwise();
            CalculateSmoothness();
            CalculateSymmetry();
        }

        private void CalculateAngles()
        {
            Angles.Clear();

            for (int i = 1; i < Points.Count - 1; i++)
            {
                var p1 = Points[i - 1];
                var p2 = Points[i];
                var p3 = Points[i + 1];

                var angle1 = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                var angle2 = Math.Atan2(p3.Y - p2.Y, p3.X - p2.X);

                var angleDiff = angle2 - angle1;

                // Normalize angle to [-π, π]
                while (angleDiff > Math.PI) angleDiff -= 2 * Math.PI;
                while (angleDiff < -Math.PI) angleDiff += 2 * Math.PI;

                Angles.Add((float)angleDiff);
            }
        }

        private void CalculateDistances()
        {
            Distances.Clear();

            for (int i = 0; i < Points.Count - 1; i++)
            {
                var distance = Distance(Points[i], Points[i + 1]);
                Distances.Add(distance);
            }
        }

        private void CalculateCurvature()
        {
            if (Angles.Count == 0) return;

            Curvature = Angles.Sum(Math.Abs) / Angles.Count;
        }

        private void CalculateDirectionChanges()
        {
            DirectionChanges = 0;

            for (int i = 1; i < Angles.Count; i++)
            {
                if (Math.Sign(Angles[i]) != Math.Sign(Angles[i - 1]))
                {
                    DirectionChanges++;
                }
            }
        }

        private void CalculateClockwise()
        {
            if (Points.Count < 3) return;

            float sum = 0;
            for (int i = 0; i < Points.Count; i++)
            {
                var p1 = Points[i];
                var p2 = Points[(i + 1) % Points.Count];
                sum += (p2.X - p1.X) * (p2.Y + p1.Y);
            }

            IsClockwise = sum > 0;
        }

        private void CalculateSmoothness()
        {
            if (Distances.Count == 0) return;

            var avgDistance = Distances.Average();
            var variance = Distances.Sum(d => Math.Pow(d - avgDistance, 2)) / Distances.Count;
            Smoothness = 1.0f / (1.0f + (float)Math.Sqrt(variance));
        }

        private void CalculateSymmetry()
        {
            // Simplified symmetry calculation
            if (Points.Count < 4) return;

            var center = Centroid;
            float symmetryScore = 0;
            int comparisons = 0;

            for (int i = 0; i < Points.Count / 2; i++)
            {
                var p1 = Points[i];
                var p2 = Points[Points.Count - 1 - i];

                // Calculate distances from center
                var d1 = Distance(p1, center);
                var d2 = Distance(p2, center);

                var diff = Math.Abs(d1 - d2);
                symmetryScore += 1.0f / (1.0f + diff);
                comparisons++;
            }

            Symmetry = comparisons > 0 ? symmetryScore / comparisons : 0;
        }

        private void RecognizeShape()
        {
            ShapeProbabilities.Clear();

            // Test for different shapes
            ShapeProbabilities["line"] = TestLine();
            ShapeProbabilities["circle"] = TestCircle();
            ShapeProbabilities["rectangle"] = TestRectangle();
            ShapeProbabilities["triangle"] = TestTriangle();
            ShapeProbabilities["arrow"] = TestArrow();
            ShapeProbabilities["star"] = TestStar();
            ShapeProbabilities["heart"] = TestHeart();
            ShapeProbabilities["spiral"] = TestSpiral();
            ShapeProbabilities["wave"] = TestWave();
            ShapeProbabilities["zigzag"] = TestZigzag();

            // Find the shape with highest probability
            var bestMatch = ShapeProbabilities.OrderByDescending(kvp => kvp.Value).First();

            if (bestMatch.Value > 0.5f) // Confidence threshold
            {
                RecognizedShape = bestMatch.Key;
                Confidence = bestMatch.Value;
                Type = GetGestureType(bestMatch.Key);
            }
            else
            {
                RecognizedShape = "freeform";
                Confidence = 0.3f;
                Type = GestureType.Freeform;
            }
        }

        private float TestLine()
        {
            if (Points.Count < 2) return 0;

            var start = Points[0];
            var end = Points[Points.Count - 1];

            float totalDeviation = 0;
            foreach (var point in Points)
            {
                var deviation = DistanceToLine(point, start, end);
                totalDeviation += deviation;
            }

            var avgDeviation = totalDeviation / Points.Count;
            var lineLength = Distance(start, end);

            if (lineLength < 20) return 0; // Too short to be a line

            var score = 1.0f - Math.Min(1.0f, avgDeviation / 20.0f);

            // Bonus for straightness
            if (DirectionChanges < 3)
                score += 0.2f;

            return Math.Min(1.0f, score);
        }

        private float TestCircle()
        {
            if (Points.Count < 10) return 0;

            var center = Centroid;
            var avgRadius = Points.Average(p => Distance(p, center));

            float radiusVariance = 0;
            foreach (var point in Points)
            {
                var radius = Distance(point, center);
                radiusVariance += Math.Abs(radius - avgRadius);
            }
            radiusVariance /= Points.Count;

            var score = 1.0f - Math.Min(1.0f, radiusVariance / avgRadius);

            // Check if it's a closed shape
            var closure = Distance(Points[0], Points[Points.Count - 1]);
            if (closure > avgRadius * 0.3f)
                score *= 0.5f;

            // Bonus for consistent curvature
            if (Math.Abs(Curvature - Math.PI) < 0.5f)
                score += 0.2f;

            return Math.Min(1.0f, score);
        }

        private float TestRectangle()
        {
            if (Points.Count < 8) return 0;

            // Find corners using direction changes
            var corners = FindCorners();

            if (corners.Count < 3 || corners.Count > 5) return 0;

            float score = 0;

            if (corners.Count == 4)
            {
                // Test if angles are approximately 90 degrees
                var angles = CalculateCornerAngles(corners);
                var rightAngleScore = angles.Average(a => 1.0f - Math.Min(1.0f, Math.Abs(a - 90) / 45.0f));

                // Test if opposite sides are equal
                var sides = CalculateSideLengths(corners);
                var sideEqualityScore = 0f;
                if (sides.Count == 4)
                {
                    var side1 = Math.Abs(sides[0] - sides[2]) / Math.Max(sides[0], sides[2]);
                    var side2 = Math.Abs(sides[1] - sides[3]) / Math.Max(sides[1], sides[3]);
                    sideEqualityScore = 1.0f - (side1 + side2) / 2.0f;
                }

                score = (rightAngleScore + sideEqualityScore) / 2.0f;
            }

            // Check aspect ratio for square bonus
            if (Math.Abs(AspectRatio - 1.0f) < 0.2f)
                score += 0.1f;

            return Math.Min(1.0f, score);
        }

        private float TestTriangle()
        {
            if (Points.Count < 6) return 0;

            var corners = FindCorners();

            if (corners.Count != 3) return 0;

            // Test if angles sum to approximately 180 degrees
            var angles = CalculateCornerAngles(corners);
            var angleSum = angles.Sum();
            var angleSumScore = 1.0f - Math.Min(1.0f, Math.Abs(angleSum - 180) / 90.0f);

            // Test closure
            var closure = Distance(Points[0], Points[Points.Count - 1]);
            var avgSideLength = CalculateSideLengths(corners).Average();
            var closureScore = 1.0f - Math.Min(1.0f, closure / (avgSideLength * 0.3f));

            return (angleSumScore + closureScore) / 2.0f;
        }

        private float TestArrow()
        {
            if (Points.Count < 6) return 0;

            // Look for arrow pattern: long line with short lines at the end
            var start = Points[0];
            var end = Points[Points.Count - 1];
            var mainLineLength = Distance(start, end);

            if (mainLineLength < 30) return 0;

            // Check if most points are near the main line
            var onMainLine = 0;
            foreach (var point in Points.Take(Points.Count * 2 / 3))
            {
                if (DistanceToLine(point, start, end) < 10)
                    onMainLine++;
            }

            var mainLineScore = (float)onMainLine / (Points.Count * 2 / 3);

            // Check for arrowhead (direction changes at the end)
            var endPoints = Points.Skip(Points.Count * 2 / 3).ToList();
            var endDirectionChanges = 0;

            for (int i = 1; i < endPoints.Count - 1; i++)
            {
                var angle1 = Math.Atan2(endPoints[i].Y - endPoints[i - 1].Y, endPoints[i].X - endPoints[i - 1].X);
                var angle2 = Math.Atan2(endPoints[i + 1].Y - endPoints[i].Y, endPoints[i + 1].X - endPoints[i].X);
                var angleDiff = Math.Abs(angle2 - angle1);
                if (angleDiff > Math.PI / 4) endDirectionChanges++;
            }

            var arrowheadScore = endDirectionChanges > 0 ? 0.8f : 0.2f;

            return (mainLineScore + arrowheadScore) / 2.0f;
        }

        private float TestStar()
        {
            if (Points.Count < 10) return 0;

            var corners = FindCorners();

            // Stars typically have 5 or 6 points (10 or 12 corners including inner points)
            if (corners.Count < 8 || corners.Count > 14) return 0;

            // Check for alternating inner and outer points
            var center = Centroid;
            var radii = corners.Select(c => Distance(c, center)).ToList();

            var innerRadii = new List<float>();
            var outerRadii = new List<float>();

            for (int i = 0; i < radii.Count; i++)
            {
                if (i % 2 == 0)
                    outerRadii.Add(radii[i]);
                else
                    innerRadii.Add(radii[i]);
            }

            if (innerRadii.Count == 0 || outerRadii.Count == 0) return 0;

            var avgInner = innerRadii.Average();
            var avgOuter = outerRadii.Average();

            // Outer points should be significantly farther from center
            var radiusRatio = avgInner / avgOuter;
            var ratioScore = radiusRatio > 0.3f && radiusRatio < 0.8f ? 1.0f : 0.0f;

            // Check for consistent spacing
            var spacingScore = 1.0f - Math.Min(1.0f, innerRadii.Concat(outerRadii).StandardDeviation() / avgOuter);

            return (ratioScore + spacingScore) / 2.0f;
        }

        private float TestHeart()
        {
            if (Points.Count < 12) return 0;

            // Hearts have a distinctive shape with two bumps at the top and a point at the bottom
            var topQuarter = Points.Take(Points.Count / 4).ToList();
            var bottomQuarter = Points.Skip(Points.Count * 3 / 4).ToList();

            // Check if bottom ends in a point (low Y values)
            var bottomY = bottomQuarter.Average(p => p.Y);
            var topY = topQuarter.Average(p => p.Y);

            if (bottomY <= topY) return 0; // Heart should point downward

            // Check for two peaks in the top half
            var topHalf = Points.Take(Points.Count / 2).ToList();
            var peaks = FindPeaks(topHalf);

            var peakScore = peaks.Count == 2 ? 1.0f : 0.0f;

            // Check overall shape symmetry
            var symmetryScore = Symmetry;

            return (peakScore + symmetryScore) / 2.0f;
        }

        private float TestSpiral()
        {
            if (Points.Count < 15) return 0;

            var center = Centroid;
            var radii = Points.Select(p => Distance(p, center)).ToList();

            // Check if radius increases or decreases consistently
            var increasing = 0;
            var decreasing = 0;

            for (int i = 1; i < radii.Count; i++)
            {
                if (radii[i] > radii[i - 1]) increasing++;
                else if (radii[i] < radii[i - 1]) decreasing++;
            }

            var consistencyScore = Math.Max(increasing, decreasing) / (float)radii.Count;

            // Check for multiple revolutions around center
            var totalAngle = 0f;
            for (int i = 1; i < Points.Count; i++)
            {
                var angle1 = Math.Atan2(Points[i - 1].Y - center.Y, Points[i - 1].X - center.X);
                var angle2 = Math.Atan2(Points[i].Y - center.Y, Points[i].X - center.X);
                var angleDiff = angle2 - angle1;

                // Normalize angle difference
                while (angleDiff > Math.PI) angleDiff -= 2 * Math.PI;
                while (angleDiff < -Math.PI) angleDiff += 2 * Math.PI;

                totalAngle += Math.Abs(angleDiff);
            }

            var revolutionScore = Math.Min(1.0f, totalAngle / (2 * Math.PI));

            return (consistencyScore + revolutionScore) / 2.0f;
        }

        private float TestWave()
        {
            if (Points.Count < 8) return 0;

            // Check for oscillating Y values with relatively consistent X progression
            var xProgression = Points.Zip(Points.Skip(1), (p1, p2) => p2.X - p1.X).Average();

            if (xProgression <= 0) return 0; // Should progress in X direction

            // Count peaks and valleys in Y direction
            var yValues = Points.Select(p => p.Y).ToList();
            var peaks = 0;
            var valleys = 0;

            for (int i = 1; i < yValues.Count - 1; i++)
            {
                if (yValues[i] > yValues[i - 1] && yValues[i] > yValues[i + 1])
                    peaks++;
                else if (yValues[i] < yValues[i - 1] && yValues[i] < yValues[i + 1])
                    valleys++;
            }

            var oscillationScore = Math.Min(1.0f, (peaks + valleys) / 4.0f);

            // Check for regularity
            var regularityScore = 1.0f - Math.Min(1.0f, Math.Abs(peaks - valleys) / Math.Max(peaks, valleys));

            return (oscillationScore + regularityScore) / 2.0f;
        }

        private float TestZigzag()
        {
            if (Points.Count < 6) return 0;

            // Zigzag should have many sharp direction changes
            var sharpTurns = Angles.Count(a => Math.Abs(a) > Math.PI / 3); // > 60 degrees
            var sharpTurnScore = Math.Min(1.0f, sharpTurns / (float)Angles.Count);

            // Check for alternating directions
            var alternations = 0;
            for (int i = 1; i < Angles.Count; i++)
            {
                if (Math.Sign(Angles[i]) != Math.Sign(Angles[i - 1]))
                    alternations++;
            }

            var alternationScore = alternations / (float)Math.Max(1, Angles.Count - 1);

            return (sharpTurnScore + alternationScore) / 2.0f;
        }

        private List<PointF> FindCorners()
        {
            var corners = new List<PointF>();

            for (int i = 1; i < Angles.Count - 1; i++)
            {
                if (Math.Abs(Angles[i]) > Math.PI / 4) // > 45 degrees
                {
                    corners.Add(Points[i + 1]); // Angle[i] corresponds to Points[i+1]
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

                var angle1 = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                var angle2 = Math.Atan2(p3.Y - p2.Y, p3.X - p2.X);

                var angleDiff = Math.Abs(angle2 - angle1) * 180 / Math.PI;
                if (angleDiff > 180) angleDiff = 360 - angleDiff;

                angles.Add((float)angleDiff);
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

        private List<PointF> FindPeaks(List<PointF> points)
        {
            var peaks = new List<PointF>();

            for (int i = 1; i < points.Count - 1; i++)
            {
                if (points[i].Y < points[i - 1].Y && points[i].Y < points[i + 1].Y) // Lower Y = higher peak
                {
                    peaks.Add(points[i]);
                }
            }

            return peaks;
        }

        private GestureType GetGestureType(string shapeName)
        {
            switch (shapeName.ToLower())
            {
                case "line": return GestureType.Line;
                case "circle": return GestureType.Circle;
                case "rectangle": return GestureType.Rectangle;
                case "triangle": return GestureType.Triangle;
                case "arrow": return GestureType.Arrow;
                case "star": return GestureType.Star;
                case "heart": return GestureType.Heart;
                case "spiral": return GestureType.Spiral;
                case "wave": return GestureType.Wave;
                case "zigzag": return GestureType.Zigzag;
                default: return GestureType.Freeform;
            }
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

        public override string ToString()
        {
            return $"{RecognizedShape} ({Confidence:P1}) - {Points.Count} points - {EndTime - StartTime:mm\\:ss\\.fff}";
        }
    }

    public enum GestureType
    {
        Unknown,
        Point,
        Line,
        Circle,
        Rectangle,
        Triangle,
        Arrow,
        Star,
        Heart,
        Spiral,
        Wave,
        Zigzag,
        Freeform
    }
}

// Extension methods for statistical calculations
public static class StatisticsExtensions
{
    public static float StandardDeviation(this IEnumerable<float> values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
        return (float)Math.Sqrt(sumOfSquares / values.Count());
    }
}