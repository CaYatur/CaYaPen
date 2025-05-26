using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AdvancedScreenPen
{
    [Serializable]
    public class AdvancedStroke
    {
        public List<PointF> Points { get; set; } = new List<PointF>();
        public Color Color { get; set; } = Color.Black;
        public float Width { get; set; } = 3.0f;
        public DrawingTool Tool { get; set; } = DrawingTool.Pen;
        public SpecialEffect Effect { get; set; } = SpecialEffect.None;
        public float Opacity { get; set; } = 1.0f;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string VoiceNote { get; set; } = "";
        public DashStyle DashStyle { get; set; } = DashStyle.Solid;
        public LineCap StartCap { get; set; } = LineCap.Round;
        public LineCap EndCap { get; set; } = LineCap.Round;
        public float Pressure { get; set; } = 1.0f;
        public float Tilt { get; set; } = 0.0f;
        public float Rotation { get; set; } = 0.0f;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;

        // Advanced properties
        public bool IsSmoothed { get; set; } = true;
        public int SmoothingLevel { get; set; } = 5;
        public float Speed { get; set; } = 1.0f;
        public string TextContent { get; set; } = "";
        public Font TextFont { get; set; }
        public string CustomPattern { get; set; } = "";
        public Bitmap CustomTexture { get; set; }

        // Metadata
        public string UserId { get; set; } = "Local";
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public int LayerId { get; set; } = 0;
        public int StrokeId { get; set; } = 0;
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

        // Animation properties
        public bool IsAnimated { get; set; } = false;
        public float AnimationDuration { get; set; } = 1.0f;
        public AnimationType AnimationType { get; set; } = AnimationType.None;
        public float AnimationProgress { get; set; } = 0.0f;

        // Physics properties for advanced effects
        public PointF Velocity { get; set; } = new PointF(0, 0);
        public PointF Acceleration { get; set; } = new PointF(0, 0);
        public float Mass { get; set; } = 1.0f;
        public float Friction { get; set; } = 0.95f;
        public float Gravity { get; set; } = 0.0f;

        public AdvancedStroke()
        {
            Timestamp = DateTime.Now;
            StrokeId = GetHashCode();
        }

        public AdvancedStroke(DrawingTool tool, Color color, float width) : this()
        {
            Tool = tool;
            Color = color;
            Width = width;
        }

        public void AddPoint(PointF point)
        {
            Points.Add(point);
            UpdateSpeed();
        }

        public void AddPoint(float x, float y)
        {
            AddPoint(new PointF(x, y));
        }

        public void AddPoint(PointF point, float pressure, float tilt = 0.0f)
        {
            Points.Add(point);
            Pressure = pressure;
            Tilt = tilt;
            UpdateSpeed();
        }

        private void UpdateSpeed()
        {
            if (Points.Count < 2)
            {
                Speed = 0.0f;
                return;
            }

            var lastPoint = Points[Points.Count - 1];
            var secondLastPoint = Points[Points.Count - 2];

            var distance = Math.Sqrt(
                Math.Pow(lastPoint.X - secondLastPoint.X, 2) +
                Math.Pow(lastPoint.Y - secondLastPoint.Y, 2)
            );

            Speed = (float)distance;
        }

        public RectangleF GetBounds()
        {
            if (Points.Count == 0)
                return RectangleF.Empty;

            float minX = Points[0].X, maxX = Points[0].X;
            float minY = Points[0].Y, maxY = Points[0].Y;

            foreach (var point in Points)
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }

            // Add padding based on width
            float padding = Width / 2 + 5;
            return new RectangleF(
                minX - padding,
                minY - padding,
                maxX - minX + padding * 2,
                maxY - minY + padding * 2
            );
        }

        public float GetLength()
        {
            if (Points.Count < 2)
                return 0.0f;

            float totalLength = 0.0f;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                var distance = Math.Sqrt(
                    Math.Pow(Points[i + 1].X - Points[i].X, 2) +
                    Math.Pow(Points[i + 1].Y - Points[i].Y, 2)
                );
                totalLength += (float)distance;
            }

            return totalLength;
        }

        public PointF GetCenterPoint()
        {
            if (Points.Count == 0)
                return PointF.Empty;

            float totalX = 0, totalY = 0;
            foreach (var point in Points)
            {
                totalX += point.X;
                totalY += point.Y;
            }

            return new PointF(totalX / Points.Count, totalY / Points.Count);
        }

        public void Transform(Matrix transformMatrix)
        {
            if (transformMatrix == null || Points.Count == 0)
                return;

            var pointsArray = Points.ToArray();
            transformMatrix.TransformPoints(pointsArray);
            Points = new List<PointF>(pointsArray);
        }

        public void Translate(float dx, float dy)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = new PointF(Points[i].X + dx, Points[i].Y + dy);
            }
        }

        public void Scale(float scaleX, float scaleY, PointF center)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                var point = Points[i];
                var newX = center.X + (point.X - center.X) * scaleX;
                var newY = center.Y + (point.Y - center.Y) * scaleY;
                Points[i] = new PointF(newX, newY);
            }

            Width *= Math.Max(scaleX, scaleY);
        }

        public void Rotate(float angle, PointF center)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            for (int i = 0; i < Points.Count; i++)
            {
                var point = Points[i];
                var x = point.X - center.X;
                var y = point.Y - center.Y;

                var newX = x * cos - y * sin + center.X;
                var newY = x * sin + y * cos + center.Y;

                Points[i] = new PointF(newX, newY);
            }

            Rotation += angle;
        }

        public bool HitTest(PointF testPoint, float tolerance = 5.0f)
        {
            if (Points.Count == 0)
                return false;

            tolerance += Width / 2;

            for (int i = 0; i < Points.Count - 1; i++)
            {
                var distance = DistanceToLineSegment(testPoint, Points[i], Points[i + 1]);
                if (distance <= tolerance)
                    return true;
            }

            return false;
        }

        private float DistanceToLineSegment(PointF point, PointF lineStart, PointF lineEnd)
        {
            var A = point.X - lineStart.X;
            var B = point.Y - lineStart.Y;
            var C = lineEnd.X - lineStart.X;
            var D = lineEnd.Y - lineStart.Y;

            var dot = A * C + B * D;
            var lenSq = C * C + D * D;

            if (lenSq == 0)
                return (float)Math.Sqrt(A * A + B * B);

            var param = dot / lenSq;

            float xx, yy;

            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + param * C;
                yy = lineStart.Y + param * D;
            }

            var dx = point.X - xx;
            var dy = point.Y - yy;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public AdvancedStroke Clone()
        {
            var clone = new AdvancedStroke
            {
                Points = new List<PointF>(this.Points),
                Color = this.Color,
                Width = this.Width,
                Tool = this.Tool,
                Effect = this.Effect,
                Opacity = this.Opacity,
                Timestamp = this.Timestamp,
                VoiceNote = this.VoiceNote,
                DashStyle = this.DashStyle,
                StartCap = this.StartCap,
                EndCap = this.EndCap,
                Pressure = this.Pressure,
                Tilt = this.Tilt,
                Rotation = this.Rotation,
                BlendMode = this.BlendMode,
                IsSmoothed = this.IsSmoothed,
                SmoothingLevel = this.SmoothingLevel,
                Speed = this.Speed,
                TextContent = this.TextContent,
                TextFont = this.TextFont,
                CustomPattern = this.CustomPattern,
                UserId = this.UserId,
                SessionId = this.SessionId,
                LayerId = this.LayerId,
                StrokeId = this.StrokeId,
                CustomProperties = new Dictionary<string, object>(this.CustomProperties),
                IsAnimated = this.IsAnimated,
                AnimationDuration = this.AnimationDuration,
                AnimationType = this.AnimationType,
                AnimationProgress = this.AnimationProgress,
                Velocity = this.Velocity,
                Acceleration = this.Acceleration,
                Mass = this.Mass,
                Friction = this.Friction,
                Gravity = this.Gravity
            };

            return clone;
        }

        public void UpdateAnimation(float deltaTime)
        {
            if (!IsAnimated || AnimationType == AnimationType.None)
                return;

            AnimationProgress += deltaTime / AnimationDuration;

            if (AnimationProgress >= 1.0f)
            {
                AnimationProgress = 1.0f;
                IsAnimated = false;
            }

            ApplyAnimationEffect();
        }

        private void ApplyAnimationEffect()
        {
            switch (AnimationType)
            {
                case AnimationType.FadeIn:
                    Opacity = AnimationProgress;
                    break;
                case AnimationType.FadeOut:
                    Opacity = 1.0f - AnimationProgress;
                    break;
                case AnimationType.GrowIn:
                    Width = Width * AnimationProgress;
                    break;
                case AnimationType.DrawOn:
                    // Gradually reveal points
                    var visibleCount = (int)(Points.Count * AnimationProgress);
                    // This would be handled in the drawing engine
                    break;
            }
        }

        public void SetCustomProperty(string key, object value)
        {
            CustomProperties[key] = value;
        }

        public T GetCustomProperty<T>(string key, T defaultValue = default(T))
        {
            if (CustomProperties.TryGetValue(key, out object value) && value is T)
                return (T)value;
            return defaultValue;
        }

        public override string ToString()
        {
            return $"{Tool} - {Points.Count} points - {Color.Name} - {Width}px - {Timestamp:HH:mm:ss}";
        }
    }

    public enum AnimationType
    {
        None,
        FadeIn,
        FadeOut,
        GrowIn,
        ShrinkOut,
        DrawOn,
        EraseOff,
        Bounce,
        Shake,
        Pulse,
        Rotate,
        Spiral
    }

    [Serializable]
    public class AnnotationLayer
    {
        public string Name { get; set; } = "Layer";
        public List<AdvancedStroke> Strokes { get; set; } = new List<AdvancedStroke>();
        public bool IsVisible { get; set; } = true;
        public bool IsLocked { get; set; } = false;
        public float Opacity { get; set; } = 1.0f;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
        public Color BackgroundColor { get; set; } = Color.Transparent;
        public bool HasBackground { get; set; } = false;

        // Layer metadata
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime ModifiedTime { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "Local";
        public int LayerId { get; set; } = 0;
        public Dictionary<string, object> LayerProperties { get; set; } = new Dictionary<string, object>();

        public AnnotationLayer()
        {
            LayerId = GetHashCode();
        }

        public AnnotationLayer(string name) : this()
        {
            Name = name;
        }

        public void AddStroke(AdvancedStroke stroke)
        {
            if (IsLocked)
                return;

            stroke.LayerId = LayerId;
            Strokes.Add(stroke);
            ModifiedTime = DateTime.Now;
        }

        public bool RemoveStroke(AdvancedStroke stroke)
        {
            if (IsLocked)
                return false;

            var removed = Strokes.Remove(stroke);
            if (removed)
                ModifiedTime = DateTime.Now;

            return removed;
        }

        public void ClearStrokes()
        {
            if (IsLocked)
                return;

            Strokes.Clear();
            ModifiedTime = DateTime.Now;
        }

        public RectangleF GetBounds()
        {
            if (Strokes.Count == 0)
                return RectangleF.Empty;

            var bounds = Strokes[0].GetBounds();

            for (int i = 1; i < Strokes.Count; i++)
            {
                bounds = RectangleF.Union(bounds, Strokes[i].GetBounds());
            }

            return bounds;
        }

        public List<AdvancedStroke> GetStrokesInArea(RectangleF area)
        {
            var result = new List<AdvancedStroke>();

            foreach (var stroke in Strokes)
            {
                if (area.IntersectsWith(stroke.GetBounds()))
                {
                    result.Add(stroke);
                }
            }

            return result;
        }

        public AdvancedStroke GetStrokeAt(PointF point, float tolerance = 5.0f)
        {
            // Search in reverse order (top to bottom)
            for (int i = Strokes.Count - 1; i >= 0; i--)
            {
                if (Strokes[i].HitTest(point, tolerance))
                    return Strokes[i];
            }

            return null;
        }

        public AnnotationLayer Clone()
        {
            var clone = new AnnotationLayer
            {
                Name = this.Name + " Copy",
                IsVisible = this.IsVisible,
                IsLocked = this.IsLocked,
                Opacity = this.Opacity,
                BlendMode = this.BlendMode,
                BackgroundColor = this.BackgroundColor,
                HasBackground = this.HasBackground,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
                CreatedBy = this.CreatedBy,
                LayerProperties = new Dictionary<string, object>(this.LayerProperties)
            };

            foreach (var stroke in this.Strokes)
            {
                clone.Strokes.Add(stroke.Clone());
            }

            return clone;
        }

        public override string ToString()
        {
            return $"{Name} ({Strokes.Count} strokes) - {(IsVisible ? "Visible" : "Hidden")} - {(IsLocked ? "Locked" : "Unlocked")}";
        }
    }
}