using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace AdvancedScreenPen
{
    public static class MathematicalTools
    {
        // Constants
        public const float PI = (float)Math.PI;
        public const float TWO_PI = 2 * PI;
        public const float HALF_PI = PI / 2;
        public const float GOLDEN_RATIO = 1.618033988749f;
        public const float DEG_TO_RAD = PI / 180f;
        public const float RAD_TO_DEG = 180f / PI;

        // Default settings
        public static int DefaultScale { get; set; } = 50;
        public static bool EnableGridSnapping { get; set; } = true;
        public static bool ShowCoordinates { get; set; } = true;
        public static int GridSize { get; set; } = 20;
        public static Color GridColor { get; set; } = Color.FromArgb(100, Color.Gray);
        public static Color AxisColor { get; set; } = Color.FromArgb(150, Color.Black);

        #region Basic Mathematical Functions

        public static float Distance(PointF p1, PointF p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float Angle(PointF p1, PointF p2)
        {
            return (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        }

        public static PointF Rotate(PointF point, PointF center, float angle)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;

            return new PointF(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
        }

        public static PointF Scale(PointF point, PointF center, float scaleX, float scaleY)
        {
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;

            return new PointF(
                center.X + dx * scaleX,
                center.Y + dy * scaleY
            );
        }

        public static PointF Lerp(PointF p1, PointF p2, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return new PointF(
                p1.X + (p2.X - p1.X) * t,
                p1.Y + (p2.Y - p1.Y) * t
            );
        }

        #endregion

        #region Geometric Shape Detection

        public static ShapeType DetectShape(List<PointF> points, float tolerance = 0.1f)
        {
            if (points == null || points.Count < 3)
                return ShapeType.Unknown;

            // Normalize points
            var normalizedPoints = NormalizePoints(points);

            // Calculate features
            var boundingRect = GetBoundingRectangle(normalizedPoints);
            var aspectRatio = boundingRect.Width / boundingRect.Height;
            var area = CalculateArea(normalizedPoints);
            var perimeter = CalculatePerimeter(normalizedPoints);
            var circularity = 4 * PI * area / (perimeter * perimeter);

            // Detect specific shapes
            if (IsCircle(normalizedPoints, tolerance))
                return ShapeType.Circle;

            if (IsRectangle(normalizedPoints, tolerance))
                return ShapeType.Rectangle;

            if (IsTriangle(normalizedPoints, tolerance))
                return ShapeType.Triangle;

            if (IsLine(normalizedPoints, tolerance))
                return ShapeType.Line;

            if (IsEllipse(normalizedPoints, tolerance))
                return ShapeType.Ellipse;

            if (IsPolygon(normalizedPoints, tolerance))
                return ShapeType.Polygon;

            return ShapeType.Freeform;
        }

        private static bool IsCircle(List<PointF> points, float tolerance)
        {
            if (points.Count < 8)
                return false;

            var center = GetCentroid(points);
            var distances = points.Select(p => Distance(p, center)).ToList();
            var avgDistance = distances.Average();
            var variance = distances.Sum(d => Math.Pow(d - avgDistance, 2)) / distances.Count;
            var stdDev = (float)Math.Sqrt(variance);

            return stdDev / avgDistance < tolerance;
        }

        private static bool IsRectangle(List<PointF> points, float tolerance)
        {
            if (points.Count < 4)
                return false;

            var corners = FindCorners(points, 4);
            if (corners.Count != 4)
                return false;

            // Check if corners form a rectangle
            var sides = new List<float>();
            for (int i = 0; i < 4; i++)
            {
                var next = (i + 1) % 4;
                sides.Add(Distance(corners[i], corners[next]));
            }

            // Check if opposite sides are equal
            var side1Diff = Math.Abs(sides[0] - sides[2]) / Math.Max(sides[0], sides[2]);
            var side2Diff = Math.Abs(sides[1] - sides[3]) / Math.Max(sides[1], sides[3]);

            return side1Diff < tolerance && side2Diff < tolerance;
        }

        private static bool IsTriangle(List<PointF> points, float tolerance)
        {
            if (points.Count < 3)
                return false;

            var corners = FindCorners(points, 3);
            return corners.Count == 3;
        }

        private static bool IsLine(List<PointF> points, float tolerance)
        {
            if (points.Count < 2)
                return false;

            var start = points.First();
            var end = points.Last();
            var lineLength = Distance(start, end);

            if (lineLength < 10)
                return false;

            var deviations = points.Select(p => DistanceToLine(p, start, end)).ToList();
            var maxDeviation = deviations.Max();

            return maxDeviation / lineLength < tolerance;
        }

        private static bool IsEllipse(List<PointF> points, float tolerance)
        {
            // Simplified ellipse detection
            var boundingRect = GetBoundingRectangle(points);
            var aspectRatio = boundingRect.Width / boundingRect.Height;

            // If it's too close to a circle, it's not an ellipse
            if (Math.Abs(aspectRatio - 1.0f) < 0.2f)
                return false;

            var center = new PointF(boundingRect.X + boundingRect.Width / 2, boundingRect.Y + boundingRect.Height / 2);
            var a = boundingRect.Width / 2;
            var b = boundingRect.Height / 2;

            var errors = points.Select(p =>
            {
                var dx = (p.X - center.X) / a;
                var dy = (p.Y - center.Y) / b;
                return Math.Abs(dx * dx + dy * dy - 1);
            }).ToList();

            return errors.Average() < tolerance;
        }

        private static bool IsPolygon(List<PointF> points, float tolerance)
        {
            var corners = FindCorners(points, 8); // Max 8 corners for detection
            return corners.Count >= 3 && corners.Count <= 8;
        }

        #endregion

        #region Function Plotting

        public static List<PointF> PlotFunction(Func<float, float> function, float startX, float endX,
            float step, PointF origin, float scale)
        {
            var points = new List<PointF>();

            for (float x = startX; x <= endX; x += step)
            {
                try
                {
                    var y = function(x);
                    if (!float.IsNaN(y) && !float.IsInfinity(y))
                    {
                        var screenX = origin.X + x * scale;
                        var screenY = origin.Y - y * scale; // Invert Y for screen coordinates
                        points.Add(new PointF(screenX, screenY));
                    }
                }
                catch
                {
                    // Skip invalid points
                }
            }

            return points;
        }

        public static List<PointF> PlotParametricFunction(Func<float, PointF> function, float startT, float endT,
            float step, PointF origin, float scale)
        {
            var points = new List<PointF>();

            for (float t = startT; t <= endT; t += step)
            {
                try
                {
                    var point = function(t);
                    if (!float.IsNaN(point.X) && !float.IsNaN(point.Y) &&
                        !float.IsInfinity(point.X) && !float.IsInfinity(point.Y))
                    {
                        var screenX = origin.X + point.X * scale;
                        var screenY = origin.Y - point.Y * scale;
                        points.Add(new PointF(screenX, screenY));
                    }
                }
                catch
                {
                    // Skip invalid points
                }
            }

            return points;
        }

        public static List<PointF> PlotPolarFunction(Func<float, float> function, float startAngle, float endAngle,
            float step, PointF origin, float scale)
        {
            return PlotParametricFunction(theta =>
            {
                var r = function(theta);
                return new PointF(r * (float)Math.Cos(theta), r * (float)Math.Sin(theta));
            }, startAngle, endAngle, step, origin, scale);
        }

        #endregion

        #region Fractal Generation

        public static List<PointF> GenerateMandelbrotSet(RectangleF bounds, int width, int height, int maxIterations)
        {
            var points = new List<PointF>();
            var scaleX = bounds.Width / width;
            var scaleY = bounds.Height / height;

            for (int px = 0; px < width; px++)
            {
                for (int py = 0; py < height; py++)
                {
                    var x = bounds.X + px * scaleX;
                    var y = bounds.Y + py * scaleY;

                    if (IsInMandelbrotSet(x, y, maxIterations))
                    {
                        points.Add(new PointF(px, py));
                    }
                }
            }

            return points;
        }

        private static bool IsInMandelbrotSet(float cx, float cy, int maxIterations)
        {
            float x = 0, y = 0;

            for (int i = 0; i < maxIterations; i++)
            {
                var tempX = x * x - y * y + cx;
                y = 2 * x * y + cy;
                x = tempX;

                if (x * x + y * y > 4)
                    return false;
            }

            return true;
        }

        public static List<PointF> GenerateDragonCurve(int iterations, float length, PointF start, float angle)
        {
            var points = new List<PointF> { start };
            var directions = new List<int> { 1 }; // 1 for right turn, -1 for left turn

            for (int i = 0; i < iterations; i++)
            {
                var newDirections = new List<int>(directions);
                newDirections.Add(1); // Add a right turn

                // Add the reversed and negated sequence
                for (int j = directions.Count - 1; j >= 0; j--)
                {
                    newDirections.Add(-directions[j]);
                }

                directions = newDirections;
            }

            var currentPos = start;
            var currentAngle = angle;

            foreach (var direction in directions)
            {
                currentAngle += direction * PI / 2; // 90-degree turns
                var nextPos = new PointF(
                    currentPos.X + length * (float)Math.Cos(currentAngle),
                    currentPos.Y + length * (float)Math.Sin(currentAngle)
                );
                points.Add(nextPos);
                currentPos = nextPos;
            }

            return points;
        }

        public static List<PointF> GenerateKochSnowflake(int iterations, float sideLength, PointF center)
        {
            // Start with an equilateral triangle
            var height = sideLength * (float)Math.Sqrt(3) / 2;
            var triangle = new List<PointF>
            {
                new PointF(center.X - sideLength / 2, center.Y + height / 3),
                new PointF(center.X + sideLength / 2, center.Y + height / 3),
                new PointF(center.X, center.Y - 2 * height / 3)
            };

            var edges = new List<(PointF start, PointF end)>
            {
                (triangle[0], triangle[1]),
                (triangle[1], triangle[2]),
                (triangle[2], triangle[0])
            };

            for (int i = 0; i < iterations; i++)
            {
                var newEdges = new List<(PointF start, PointF end)>();

                foreach (var edge in edges)
                {
                    var subEdges = SubdivideEdgeKoch(edge.start, edge.end);
                    newEdges.AddRange(subEdges);
                }

                edges = newEdges;
            }

            var points = new List<PointF>();
            foreach (var edge in edges)
            {
                points.Add(edge.start);
            }

            return points;
        }

        private static List<(PointF start, PointF end)> SubdivideEdgeKoch(PointF start, PointF end)
        {
            var oneThird = Lerp(start, end, 1f / 3f);
            var twoThird = Lerp(start, end, 2f / 3f);

            // Create the peak of the equilateral triangle
            var dx = twoThird.X - oneThird.X;
            var dy = twoThird.Y - oneThird.Y;
            var peak = new PointF(
                oneThird.X + dx * 0.5f - dy * (float)Math.Sqrt(3) / 2,
                oneThird.Y + dy * 0.5f + dx * (float)Math.Sqrt(3) / 2
            );

            return new List<(PointF start, PointF end)>
            {
                (start, oneThird),
                (oneThird, peak),
                (peak, twoThird),
                (twoThird, end)
            };
        }

        #endregion

        #region Geometry Analysis

        public static PointF GetCentroid(List<PointF> points)
        {
            if (points == null || points.Count == 0)
                return PointF.Empty;

            var sumX = points.Sum(p => p.X);
            var sumY = points.Sum(p => p.Y);

            return new PointF(sumX / points.Count, sumY / points.Count);
        }

        public static RectangleF GetBoundingRectangle(List<PointF> points)
        {
            if (points == null || points.Count == 0)
                return RectangleF.Empty;

            var minX = points.Min(p => p.X);
            var maxX = points.Max(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxY = points.Max(p => p.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        public static float CalculateArea(List<PointF> points)
        {
            if (points == null || points.Count < 3)
                return 0;

            float area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                var j = (i + 1) % points.Count;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }

            return Math.Abs(area) / 2;
        }

        public static float CalculatePerimeter(List<PointF> points)
        {
            if (points == null || points.Count < 2)
                return 0;

            float perimeter = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                perimeter += Distance(points[i], points[i + 1]);
            }

            // Add distance from last to first point to close the shape
            if (points.Count > 2)
            {
                perimeter += Distance(points[points.Count - 1], points[0]);
            }

            return perimeter;
        }

        public static float DistanceToLine(PointF point, PointF lineStart, PointF lineEnd)
        {
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;

            if (dx == 0 && dy == 0)
                return Distance(point, lineStart);

            var t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            var projection = new PointF(lineStart.X + t * dx, lineStart.Y + t * dy);
            return Distance(point, projection);
        }

        #endregion

        #region Coordinate Systems

        public static PointF CartesianToPolar(PointF cartesian, PointF origin)
        {
            var dx = cartesian.X - origin.X;
            var dy = cartesian.Y - origin.Y;
            var r = (float)Math.Sqrt(dx * dx + dy * dy);
            var theta = (float)Math.Atan2(dy, dx);

            return new PointF(r, theta);
        }

        public static PointF PolarToCartesian(PointF polar, PointF origin)
        {
            var x = origin.X + polar.X * (float)Math.Cos(polar.Y);
            var y = origin.Y + polar.X * (float)Math.Sin(polar.Y);

            return new PointF(x, y);
        }

        public static void DrawCoordinateSystem(Graphics g, Rectangle bounds, PointF origin, float scale)
        {
            using (var axisPen = new Pen(AxisColor, 2))
            using (var gridPen = new Pen(GridColor, 1))
            {
                // Draw grid
                if (EnableGridSnapping)
                {
                    var gridStep = GridSize;

                    // Vertical grid lines
                    for (float x = origin.X % gridStep; x < bounds.Width; x += gridStep)
                    {
                        g.DrawLine(gridPen, x, 0, x, bounds.Height);
                    }

                    // Horizontal grid lines
                    for (float y = origin.Y % gridStep; y < bounds.Height; y += gridStep)
                    {
                        g.DrawLine(gridPen, 0, y, bounds.Width, y);
                    }
                }

                // Draw axes
                g.DrawLine(axisPen, 0, origin.Y, bounds.Width, origin.Y); // X-axis
                g.DrawLine(axisPen, origin.X, 0, origin.X, bounds.Height); // Y-axis

                // Draw axis labels
                if (ShowCoordinates)
                {
                    using (var font = new Font("Arial", 8))
                    using (var brush = new SolidBrush(AxisColor))
                    {
                        // X-axis labels
                        for (int x = -(int)(origin.X / scale); x <= (bounds.Width - origin.X) / scale; x += 1)
                        {
                            if (x == 0) continue;
                            var screenX = origin.X + x * scale;
                            if (screenX >= 0 && screenX <= bounds.Width)
                            {
                                g.DrawString(x.ToString(), font, brush, screenX - 10, origin.Y + 5);
                            }
                        }

                        // Y-axis labels
                        for (int y = -(int)((bounds.Height - origin.Y) / scale); y <= origin.Y / scale; y += 1)
                        {
                            if (y == 0) continue;
                            var screenY = origin.Y - y * scale;
                            if (screenY >= 0 && screenY <= bounds.Height)
                            {
                                g.DrawString(y.ToString(), font, brush, origin.X + 5, screenY - 8);
                            }
                        }

                        // Origin label
                        g.DrawString("0", font, brush, origin.X + 5, origin.Y + 5);
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private static List<PointF> NormalizePoints(List<PointF> points)
        {
            if (points == null || points.Count == 0)
                return new List<PointF>();

            var bounds = GetBoundingRectangle(points);
            var scale = Math.Max(bounds.Width, bounds.Height);

            if (scale == 0)
                return points.ToList();

            return points.Select(p => new PointF(
                (p.X - bounds.X) / scale,
                (p.Y - bounds.Y) / scale
            )).ToList();
        }

        private static List<PointF> FindCorners(List<PointF> points, int maxCorners)
        {
            if (points.Count < 3)
                return points.ToList();

            var corners = new List<PointF>();
            var threshold = 0.3f; // Angle threshold for corner detection

            for (int i = 1; i < points.Count - 1; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var next = points[i + 1];

                var angle1 = Angle(curr, prev);
                var angle2 = Angle(curr, next);
                var angleDiff = Math.Abs(angle1 - angle2);

                // Normalize angle difference
                if (angleDiff > PI)
                    angleDiff = TWO_PI - angleDiff;

                if (angleDiff > threshold)
                {
                    corners.Add(curr);
                    if (corners.Count >= maxCorners)
                        break;
                }
            }

            return corners;
        }

        private static float CalculateDirectionalFeature(List<PointF> points, double direction)
        {
            if (points.Count < 2) return 0;

            var count = 0;
            var cos = (float)Math.Cos(direction);
            var sin = (float)Math.Sin(direction);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var dx = points[i + 1].X - points[i].X;
                var dy = points[i + 1].Y - points[i].Y;

                var dot = dx * cos + dy * sin;
                if (dot > 0) count++;
            }

            return (float)count / (points.Count - 1);
        }

        #endregion
    }

    // Supporting enums and classes
    public enum ShapeType
    {
        Unknown,
        Line,
        Circle,
        Rectangle,
        Triangle,
        Ellipse,
        Polygon,
        Freeform
    }

    public class GeometryAnalysisResult
    {
        public ShapeType DetectedShape { get; set; }
        public float Confidence { get; set; }
        public PointF Centroid { get; set; }
        public RectangleF BoundingRectangle { get; set; }
        public float Area { get; set; }
        public float Perimeter { get; set; }
        public List<PointF> Corners { get; set; } = new List<PointF>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    public class MathFunction
    {
        public string Name { get; set; }
        public string Expression { get; set; }
        public Func<float, float> Function { get; set; }
        public Color Color { get; set; } = Color.Blue;
        public float LineWidth { get; set; } = 2f;
        public bool IsVisible { get; set; } = true;
        public float MinX { get; set; } = -10f;
        public float MaxX { get; set; } = 10f;
        public float Step { get; set; } = 0.1f;
    }
}