using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace AdvancedScreenPen
{
    public enum DrawingTool
    {
        Pen, Brush, Eraser, Highlighter, Laser, Ruler, Protractor,
        Circle, Rectangle, Arrow, Text, Stamp, Magnifier, Spotlight,
        SmartShape, HandwritingRecognition, VoiceAnnotation,
        GeometryCalculator, MathFormula, ColorPicker, GradientFill,
        PatternBrush, Calligraphy, Spray, Clone, Blur, Sharpen,
        MagicWand, Lightning, FlowField, Particle, Neon, Fire,
        Water, Smoke, Glass, Metal, Wood, Fabric, Stone
    }

    public enum SpecialEffect
    {
        None, Glow, Shadow, Neon, Fire, Water, Electric, Rainbow,
        Particle, Smoke, Glass, Metal, Wood, Fabric, Stone,
        Blur, Sharpen, Emboss, Edge, Sepia, Vintage, Cartoon
    }

    public enum BlendMode
    {
        Normal, Multiply, Screen, Overlay, SoftLight, HardLight,
        ColorDodge, ColorBurn, Darken, Lighten, Difference, Exclusion,
        Hue, Saturation, Color, Luminosity
    }

    public class DrawingEngine
    {
        private Graphics targetGraphics;
        private Bitmap renderTarget;
        private Dictionary<string, Brush> brushCache;
        private Dictionary<string, Pen> penCache;
        private Random randomGenerator;

        // Performance settings
        public bool AntiAliasing { get; set; } = true;
        public bool HighQualityRendering { get; set; } = true;
        public int MaxCacheSize { get; set; } = 100;

        // Advanced rendering options
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.HighQualityBicubic;
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.AntiAlias;
        public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.HighQuality;

        public DrawingEngine(Graphics graphics, Size renderSize)
        {
            targetGraphics = graphics;
            renderTarget = new Bitmap(renderSize.Width, renderSize.Height, PixelFormat.Format32bppArgb);
            brushCache = new Dictionary<string, Brush>();
            penCache = new Dictionary<string, Pen>();
            randomGenerator = new Random();

            InitializeGraphicsSettings();
        }

        private void InitializeGraphicsSettings()
        {
            if (targetGraphics != null)
            {
                targetGraphics.SmoothingMode = SmoothingMode;
                targetGraphics.InterpolationMode = InterpolationMode;
                targetGraphics.CompositingQuality = CompositingQuality;
                targetGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                if (HighQualityRendering)
                {
                    targetGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    targetGraphics.CompositingMode = CompositingMode.SourceOver;
                }
            }
        }

        public void DrawStroke(AdvancedStroke stroke, float layerOpacity = 1.0f)
        {
            if (stroke?.Points == null || stroke.Points.Count == 0)
                return;

            var effectiveOpacity = stroke.Opacity * layerOpacity;

            switch (stroke.Tool)
            {
                case DrawingTool.Pen:
                    DrawPenStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.Brush:
                    DrawBrushStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.Highlighter:
                    DrawHighlighterStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.Calligraphy:
                    DrawCalligraphyStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.Spray:
                    DrawSprayStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.MagicWand:
                    DrawMagicWandStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.Lightning:
                    DrawLightningStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.FlowField:
                    DrawFlowFieldStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.Neon:
                    DrawNeonStroke(stroke, effectiveOpacity);
                    break;
                case DrawingTool.Fire:
                    DrawFireStroke(stroke, effectiveOpacity);
                    break;
                default:
                    DrawPenStroke(stroke, effectiveOpacity);
                    break;
            }

            // Apply special effects
            if (stroke.Effect != SpecialEffect.None)
            {
                ApplySpecialEffect(stroke, effectiveOpacity);
            }
        }

        private void DrawPenStroke(AdvancedStroke stroke, float opacity)
        {
            if (stroke.Points.Count < 2) return;

            var pen = GetCachedPen(stroke, opacity);

            if (stroke.Points.Count == 2)
            {
                targetGraphics.DrawLine(pen, stroke.Points[0], stroke.Points[1]);
            }
            else
            {
                var smoothPoints = ApplySmoothing(stroke.Points, 5);
                if (smoothPoints.Count > 2)
                {
                    targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                }
            }
        }

        private void DrawBrushStroke(AdvancedStroke stroke, float opacity)
        {
            var brush = GetCachedBrush(stroke, opacity);

            foreach (var point in stroke.Points)
            {
                float size = stroke.Width * stroke.Pressure;
                var rect = new RectangleF(point.X - size / 2, point.Y - size / 2, size, size);
                targetGraphics.FillEllipse(brush, rect);
            }
        }

        private void DrawHighlighterStroke(AdvancedStroke stroke, float opacity)
        {
            var color = Color.FromArgb((int)(opacity * 100), stroke.Color);
            using (var pen = new Pen(color, stroke.Width * 2))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                if (stroke.Points.Count > 2)
                {
                    var smoothPoints = ApplySmoothing(stroke.Points, 3);
                    targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                }
            }
        }

        private void DrawCalligraphyStroke(AdvancedStroke stroke, float opacity)
        {
            if (stroke.Points.Count < 2) return;

            for (int i = 0; i < stroke.Points.Count - 1; i++)
            {
                var speed = CalculateSpeed(stroke.Points, i);
                var width = stroke.Width * (0.5f + 0.5f * (1.0f - speed)) * stroke.Pressure;

                var color = Color.FromArgb((int)(opacity * 255), stroke.Color);
                using (var pen = new Pen(color, width))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    targetGraphics.DrawLine(pen, stroke.Points[i], stroke.Points[i + 1]);
                }
            }
        }

        private void DrawSprayStroke(AdvancedStroke stroke, float opacity)
        {
            var color = Color.FromArgb((int)(opacity * 255), stroke.Color);
            using (var brush = new SolidBrush(color))
            {
                foreach (var point in stroke.Points)
                {
                    int particleCount = (int)(stroke.Width * 3);
                    for (int i = 0; i < particleCount; i++)
                    {
                        float offsetX = (float)(randomGenerator.NextDouble() - 0.5) * stroke.Width * 2;
                        float offsetY = (float)(randomGenerator.NextDouble() - 0.5) * stroke.Width * 2;
                        float particleSize = (float)randomGenerator.NextDouble() * 2 + 1;

                        var particleRect = new RectangleF(
                            point.X + offsetX - particleSize / 2,
                            point.Y + offsetY - particleSize / 2,
                            particleSize, particleSize);

                        targetGraphics.FillEllipse(brush, particleRect);
                    }
                }
            }
        }

        private void DrawMagicWandStroke(AdvancedStroke stroke, float opacity)
        {
            // Draw main stroke
            var pen = GetCachedPen(stroke, opacity);
            if (stroke.Points.Count > 2)
            {
                var smoothPoints = ApplySmoothing(stroke.Points, 5);
                targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
            }

            // Add sparkles
            var sparkleColors = new Color[] { Color.White, Color.Yellow, Color.Cyan, Color.Magenta };
            foreach (var point in stroke.Points.Where((p, i) => i % 3 == 0))
            {
                if (randomGenerator.NextDouble() < 0.3) // 30% chance for sparkle
                {
                    var sparkleColor = sparkleColors[randomGenerator.Next(sparkleColors.Length)];
                    DrawSparkle(point, sparkleColor, opacity);
                }
            }
        }

        private void DrawLightningStroke(AdvancedStroke stroke, float opacity)
        {
            if (stroke.Points.Count < 2) return;

            var lightningPoints = GenerateLightningPath(stroke.Points);

            // Draw main lightning bolt
            var color = Color.FromArgb((int)(opacity * 255), Color.White);
            using (var pen = new Pen(color, stroke.Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                targetGraphics.DrawCurve(pen, lightningPoints.ToArray());
            }

            // Draw glow effect
            var glowColor = Color.FromArgb((int)(opacity * 100), stroke.Color);
            using (var glowPen = new Pen(glowColor, stroke.Width + 10))
            {
                targetGraphics.DrawCurve(glowPen, lightningPoints.ToArray());
            }

            // Draw branches
            DrawLightningBranches(lightningPoints, stroke.Color, opacity);
        }

        private void DrawFlowFieldStroke(AdvancedStroke stroke, float opacity)
        {
            var particles = GenerateFlowFieldParticles(stroke.Points, stroke.Width);

            foreach (var particle in particles)
            {
                var alpha = (int)(particle.Life * opacity * 255);
                var color = Color.FromArgb(alpha, stroke.Color);
                using (var brush = new SolidBrush(color))
                {
                    var rect = new RectangleF(
                        particle.Position.X - particle.Size / 2,
                        particle.Position.Y - particle.Size / 2,
                        particle.Size, particle.Size);

                    targetGraphics.FillEllipse(brush, rect);
                }
            }
        }

        private void DrawNeonStroke(AdvancedStroke stroke, float opacity)
        {
            // Multiple glow layers for neon effect
            for (int i = 0; i < 5; i++)
            {
                var alpha = (int)(opacity * (50 - i * 8));
                var color = Color.FromArgb(alpha, stroke.Color);
                using (var pen = new Pen(color, stroke.Width + i * 4))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    if (stroke.Points.Count > 2)
                    {
                        var smoothPoints = ApplySmoothing(stroke.Points, 5);
                        targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                    }
                }
            }
        }

        private void DrawFireStroke(AdvancedStroke stroke, float opacity)
        {
            foreach (var point in stroke.Points)
            {
                // Generate fire particles
                for (int i = 0; i < 8; i++)
                {
                    float offsetX = (float)(randomGenerator.NextDouble() - 0.5) * stroke.Width;
                    float offsetY = (float)(randomGenerator.NextDouble() - 0.5) * stroke.Width * 0.5f - stroke.Width * 0.25f;

                    var fireColors = new Color[] {
                        Color.Red, Color.Orange, Color.Yellow, Color.DarkRed
                    };
                    var fireColor = fireColors[randomGenerator.Next(fireColors.Length)];
                    var alpha = (int)(opacity * randomGenerator.Next(100, 255));
                    var color = Color.FromArgb(alpha, fireColor);

                    using (var brush = new SolidBrush(color))
                    {
                        float size = (float)randomGenerator.NextDouble() * 4 + 2;
                        var rect = new RectangleF(
                            point.X + offsetX - size / 2,
                            point.Y + offsetY - size / 2,
                            size, size);

                        targetGraphics.FillEllipse(brush, rect);
                    }
                }
            }
        }

        private void ApplySpecialEffect(AdvancedStroke stroke, float opacity)
        {
            switch (stroke.Effect)
            {
                case SpecialEffect.Glow:
                    ApplyGlowEffect(stroke, opacity);
                    break;
                case SpecialEffect.Shadow:
                    ApplyShadowEffect(stroke, opacity);
                    break;
                case SpecialEffect.Blur:
                    ApplyBlurEffect(stroke, opacity);
                    break;
                case SpecialEffect.Emboss:
                    ApplyEmbossEffect(stroke, opacity);
                    break;
                case SpecialEffect.Rainbow:
                    ApplyRainbowEffect(stroke, opacity);
                    break;
            }
        }

        private void ApplyGlowEffect(AdvancedStroke stroke, float opacity)
        {
            var glowColor = Color.FromArgb((int)(opacity * 50), stroke.Color);
            using (var pen = new Pen(glowColor, stroke.Width + 15))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                if (stroke.Points.Count > 2)
                {
                    var smoothPoints = ApplySmoothing(stroke.Points, 5);
                    targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                }
            }
        }

        private void ApplyShadowEffect(AdvancedStroke stroke, float opacity)
        {
            var shadowPoints = stroke.Points.Select(p => new PointF(p.X + 3, p.Y + 3)).ToList();
            var shadowColor = Color.FromArgb((int)(opacity * 100), Color.Black);

            using (var pen = new Pen(shadowColor, stroke.Width))
            {
                if (shadowPoints.Count > 2)
                {
                    var smoothPoints = ApplySmoothing(shadowPoints, 5);
                    targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                }
            }
        }

        private void ApplyBlurEffect(AdvancedStroke stroke, float opacity)
        {
            // Simplified blur by drawing multiple offset strokes
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    if (i == 0 && j == 0) continue;

                    var blurPoints = stroke.Points.Select(p => new PointF(p.X + i, p.Y + j)).ToList();
                    var blurColor = Color.FromArgb((int)(opacity * 20), stroke.Color);

                    using (var pen = new Pen(blurColor, stroke.Width))
                    {
                        if (blurPoints.Count > 2)
                        {
                            var smoothPoints = ApplySmoothing(blurPoints, 5);
                            targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                        }
                    }
                }
            }
        }

        private void ApplyEmbossEffect(AdvancedStroke stroke, float opacity)
        {
            // Highlight
            var highlightPoints = stroke.Points.Select(p => new PointF(p.X - 1, p.Y - 1)).ToList();
            var highlightColor = Color.FromArgb((int)(opacity * 150), Color.White);

            using (var pen = new Pen(highlightColor, stroke.Width))
            {
                if (highlightPoints.Count > 2)
                {
                    var smoothPoints = ApplySmoothing(highlightPoints, 5);
                    targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                }
            }

            // Shadow
            var shadowPoints = stroke.Points.Select(p => new PointF(p.X + 1, p.Y + 1)).ToList();
            var shadowColor = Color.FromArgb((int)(opacity * 150), Color.Black);

            using (var pen = new Pen(shadowColor, stroke.Width))
            {
                if (shadowPoints.Count > 2)
                {
                    var smoothPoints = ApplySmoothing(shadowPoints, 5);
                    targetGraphics.DrawCurve(pen, smoothPoints.ToArray());
                }
            }
        }

        private void ApplyRainbowEffect(AdvancedStroke stroke, float opacity)
        {
            var rainbowColors = new Color[] {
                Color.Red, Color.Orange, Color.Yellow, Color.Green,
                Color.Blue, Color.Indigo, Color.Violet
            };

            for (int i = 0; i < stroke.Points.Count - 1; i++)
            {
                var colorIndex = (i / 10) % rainbowColors.Length;
                var color = Color.FromArgb((int)(opacity * 255), rainbowColors[colorIndex]);

                using (var pen = new Pen(color, stroke.Width))
                {
                    targetGraphics.DrawLine(pen, stroke.Points[i], stroke.Points[i + 1]);
                }
            }
        }

        private List<PointF> ApplySmoothing(List<PointF> points, int smoothLevel)
        {
            if (points.Count < 3 || smoothLevel <= 1)
                return points;

            var smoothedPoints = new List<PointF>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = i > 0 ? points[i - 1] : points[i];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = i < points.Count - 2 ? points[i + 2] : points[i + 1];

                for (float t = 0; t <= 1; t += 1.0f / smoothLevel)
                {
                    var smoothPoint = CatmullRomSpline(p0, p1, p2, p3, t);
                    smoothedPoints.Add(smoothPoint);
                }
            }

            return smoothedPoints;
        }

        private PointF CatmullRomSpline(PointF p0, PointF p1, PointF p2, PointF p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float x = 0.5f * ((2.0f * p1.X) +
                             (-p0.X + p2.X) * t +
                             (2.0f * p0.X - 5.0f * p1.X + 4.0f * p2.X - p3.X) * t2 +
                             (-p0.X + 3.0f * p1.X - 3.0f * p2.X + p3.X) * t3);

            float y = 0.5f * ((2.0f * p1.Y) +
                             (-p0.Y + p2.Y) * t +
                             (2.0f * p0.Y - 5.0f * p1.Y + 4.0f * p2.Y - p3.Y) * t2 +
                             (-p0.Y + 3.0f * p1.Y - 3.0f * p2.Y + p3.Y) * t3);

            return new PointF(x, y);
        }

        private float CalculateSpeed(List<PointF> points, int index)
        {
            if (index <= 0 || index >= points.Count - 1)
                return 1.0f;

            var distance = Distance(points[index - 1], points[index + 1]);
            return Math.Min(1.0f, distance / 50.0f); // Normalize to 0-1
        }

        private float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private void DrawSparkle(PointF center, Color color, float opacity)
        {
            var sparkleColor = Color.FromArgb((int)(opacity * 255), color);
            using (var pen = new Pen(sparkleColor, 2))
            {
                float size = randomGenerator.Next(6, 12);

                // Draw star shape
                targetGraphics.DrawLine(pen, center.X - size / 2, center.Y, center.X + size / 2, center.Y);
                targetGraphics.DrawLine(pen, center.X, center.Y - size / 2, center.X, center.Y + size / 2);
                targetGraphics.DrawLine(pen, center.X - size / 3, center.Y - size / 3, center.X + size / 3, center.Y + size / 3);
                targetGraphics.DrawLine(pen, center.X - size / 3, center.Y + size / 3, center.X + size / 3, center.Y - size / 3);
            }
        }

        private List<PointF> GenerateLightningPath(List<PointF> originalPoints)
        {
            var lightningPoints = new List<PointF>();

            for (int i = 0; i < originalPoints.Count - 1; i++)
            {
                var start = originalPoints[i];
                var end = originalPoints[i + 1];

                // Add intermediate points with random variations
                int segments = 5;
                for (int j = 0; j <= segments; j++)
                {
                    float t = (float)j / segments;
                    float x = start.X + (end.X - start.X) * t;
                    float y = start.Y + (end.Y - start.Y) * t;

                    // Add random offset (except for start and end points)
                    if (j > 0 && j < segments)
                    {
                        x += (float)(randomGenerator.NextDouble() - 0.5) * 20;
                        y += (float)(randomGenerator.NextDouble() - 0.5) * 20;
                    }

                    lightningPoints.Add(new PointF(x, y));
                }
            }

            return lightningPoints;
        }

        private void DrawLightningBranches(List<PointF> mainPath, Color color, float opacity)
        {
            for (int i = 1; i < mainPath.Count - 1; i += 3)
            {
                if (randomGenerator.NextDouble() < 0.3) // 30% chance for branch
                {
                    var branchStart = mainPath[i];
                    var branchEnd = new PointF(
                        branchStart.X + (float)(randomGenerator.NextDouble() - 0.5) * 40,
                        branchStart.Y + (float)(randomGenerator.NextDouble() - 0.5) * 40
                    );

                    var branchColor = Color.FromArgb((int)(opacity * 150), Color.White);
                    using (var branchPen = new Pen(branchColor, 2))
                    {
                        targetGraphics.DrawLine(branchPen, branchStart, branchEnd);
                    }
                }
            }
        }

        private List<FlowParticle> GenerateFlowFieldParticles(List<PointF> points, float width)
        {
            var particles = new List<FlowParticle>();

            foreach (var point in points)
            {
                for (int i = 0; i < 5; i++)
                {
                    particles.Add(new FlowParticle
                    {
                        Position = new PointF(
                            point.X + (float)(randomGenerator.NextDouble() - 0.5) * width,
                            point.Y + (float)(randomGenerator.NextDouble() - 0.5) * width
                        ),
                        Velocity = new PointF(
                            (float)(randomGenerator.NextDouble() - 0.5) * 2,
                            (float)(randomGenerator.NextDouble() - 0.5) * 2
                        ),
                        Life = (float)randomGenerator.NextDouble(),
                        Size = (float)randomGenerator.NextDouble() * 3 + 1
                    });
                }
            }

            return particles;
        }

        private Pen GetCachedPen(AdvancedStroke stroke, float opacity)
        {
            var key = $"pen_{stroke.Color.ToArgb()}_{stroke.Width}_{opacity}_{stroke.DashStyle}";

            if (!penCache.ContainsKey(key))
            {
                var color = Color.FromArgb((int)(opacity * 255), stroke.Color);
                var pen = new Pen(color, stroke.Width)
                {
                    DashStyle = stroke.DashStyle,
                    StartCap = stroke.StartCap,
                    EndCap = stroke.EndCap,
                    LineJoin = LineJoin.Round
                };

                penCache[key] = pen;

                // Clean cache if too large
                if (penCache.Count > MaxCacheSize)
                {
                    var firstKey = penCache.Keys.First();
                    penCache[firstKey].Dispose();
                    penCache.Remove(firstKey);
                }
            }

            return penCache[key];
        }

        private Brush GetCachedBrush(AdvancedStroke stroke, float opacity)
        {
            var key = $"brush_{stroke.Color.ToArgb()}_{opacity}";

            if (!brushCache.ContainsKey(key))
            {
                var color = Color.FromArgb((int)(opacity * 255), stroke.Color);
                var brush = new SolidBrush(color);

                brushCache[key] = brush;

                // Clean cache if too large
                if (brushCache.Count > MaxCacheSize)
                {
                    var firstKey = brushCache.Keys.First();
                    brushCache[firstKey].Dispose();
                    brushCache.Remove(firstKey);
                }
            }

            return brushCache[key];
        }

        public void Dispose()
        {
            // Dispose cached resources
            foreach (var pen in penCache.Values)
            {
                pen?.Dispose();
            }
            penCache.Clear();

            foreach (var brush in brushCache.Values)
            {
                brush?.Dispose();
            }
            brushCache.Clear();

            renderTarget?.Dispose();
        }
    }

    public class FlowParticle
    {
        public PointF Position { get; set; }
        public PointF Velocity { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
    }
}