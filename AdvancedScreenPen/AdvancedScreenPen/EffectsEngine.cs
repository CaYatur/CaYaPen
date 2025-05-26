using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AdvancedScreenPen
{
    public class EffectsEngine : IDisposable
    {
        private readonly Dictionary<string, IEffect> effectLibrary;
        private readonly Queue<EffectRenderTask> renderQueue;
        private readonly object renderLock = new object();
        private bool isProcessing = false;

        // Effect settings
        public bool EnableGPUAcceleration { get; set; } = true;
        public bool EnableMultithreading { get; set; } = true;
        public int MaxConcurrentEffects { get; set; } = 4;
        public EffectQuality Quality { get; set; } = EffectQuality.High;

        // Performance settings
        public bool EnableCaching { get; set; } = true;
        public int CacheSize { get; set; } = 50;
        public bool EnableLOD { get; set; } = true; // Level of Detail

        // Advanced features
        public bool EnableRealTimePreview { get; set; } = true;
        public bool EnableAnimatedEffects { get; set; } = true;
        public bool EnableParticleEffects { get; set; } = true;
        public bool EnableShaderEffects { get; set; } = false;

        // Events
        public event EventHandler<EffectAppliedEventArgs> EffectApplied;
        public event EventHandler<EffectProgressEventArgs> EffectProgress;
        public event EventHandler<EffectErrorEventArgs> EffectError;

        public EffectsEngine()
        {
            effectLibrary = new Dictionary<string, IEffect>();
            renderQueue = new Queue<EffectRenderTask>();

            InitializeBuiltInEffects();
        }

        public async Task<Bitmap> ApplyEffectAsync(Bitmap source, string effectName, Dictionary<string, object> parameters = null)
        {
            if (source == null || string.IsNullOrEmpty(effectName))
                return source;

            if (!effectLibrary.ContainsKey(effectName))
            {
                OnEffectError($"Effect '{effectName}' not found");
                return source;
            }

            try
            {
                var effect = effectLibrary[effectName];
                var effectParameters = parameters ?? new Dictionary<string, object>();

                OnEffectProgress(effectName, 0, "Starting effect...");

                var result = await Task.Run(() => effect.Apply(source, effectParameters, Quality));

                OnEffectProgress(effectName, 100, "Effect completed");
                OnEffectApplied(effectName, result);

                return result;
            }
            catch (Exception ex)
            {
                OnEffectError($"Error applying effect '{effectName}': {ex.Message}");
                return source;
            }
        }

        public async Task<Bitmap> ApplyEffectChainAsync(Bitmap source, List<EffectDefinition> effects)
        {
            if (source == null || effects == null || effects.Count == 0)
                return source;

            var currentImage = new Bitmap(source);

            try
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    var progress = (i * 100) / effects.Count;

                    OnEffectProgress("Chain", progress, $"Applying {effect.Name}...");

                    var result = await ApplyEffectAsync(currentImage, effect.Name, effect.Parameters);

                    if (currentImage != source)
                    {
                        currentImage.Dispose();
                    }

                    currentImage = result;
                }

                OnEffectProgress("Chain", 100, "Chain completed");
                return currentImage;
            }
            catch (Exception ex)
            {
                if (currentImage != source)
                {
                    currentImage.Dispose();
                }
                OnEffectError($"Error in effect chain: {ex.Message}");
                return source;
            }
        }

        public void RegisterEffect(string name, IEffect effect)
        {
            if (!string.IsNullOrEmpty(name) && effect != null)
            {
                effectLibrary[name] = effect;
            }
        }

        public void UnregisterEffect(string name)
        {
            if (effectLibrary.ContainsKey(name))
            {
                effectLibrary.Remove(name);
            }
        }

        public List<string> GetAvailableEffects()
        {
            return effectLibrary.Keys.ToList();
        }

        public EffectInfo GetEffectInfo(string effectName)
        {
            if (effectLibrary.ContainsKey(effectName))
            {
                var effect = effectLibrary[effectName];
                return new EffectInfo
                {
                    Name = effectName,
                    Description = effect.Description,
                    Category = effect.Category,
                    Parameters = effect.GetParameters(),
                    SupportsAnimation = effect.SupportsAnimation,
                    RequiresGPU = effect.RequiresGPU
                };
            }

            return null;
        }

        public async Task<Bitmap> CreateAnimatedEffectFrameAsync(Bitmap source, string effectName,
            Dictionary<string, object> parameters, float time)
        {
            if (!effectLibrary.ContainsKey(effectName))
                return source;

            var effect = effectLibrary[effectName];
            if (!effect.SupportsAnimation)
                return await ApplyEffectAsync(source, effectName, parameters);

            var animatedParameters = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>())
            {
                ["_time"] = time,
                ["_frame"] = (int)(time * 30) // Assume 30 FPS
            };

            return await ApplyEffectAsync(source, effectName, animatedParameters);
        }

        private void InitializeBuiltInEffects()
        {
            // Basic effects
            RegisterEffect("blur", new BlurEffect());
            RegisterEffect("sharpen", new SharpenEffect());
            RegisterEffect("emboss", new EmbossEffect());
            RegisterEffect("edge", new EdgeDetectionEffect());
            RegisterEffect("noise", new NoiseEffect());

            // Color effects
            RegisterEffect("brightness", new BrightnessEffect());
            RegisterEffect("contrast", new ContrastEffect());
            RegisterEffect("saturation", new SaturationEffect());
            RegisterEffect("hue", new HueShiftEffect());
            RegisterEffect("sepia", new SepiaEffect());
            RegisterEffect("grayscale", new GrayscaleEffect());
            RegisterEffect("invert", new InvertEffect());

            // Artistic effects
            RegisterEffect("oil_painting", new OilPaintingEffect());
            RegisterEffect("watercolor", new WatercolorEffect());
            RegisterEffect("pencil_sketch", new PencilSketchEffect());
            RegisterEffect("cartoon", new CartoonEffect());
            RegisterEffect("vintage", new VintageEffect());

            // Distortion effects
            RegisterEffect("ripple", new RippleEffect());
            RegisterEffect("wave", new WaveEffect());
            RegisterEffect("twirl", new TwirlEffect());
            RegisterEffect("pinch", new PinchEffect());
            RegisterEffect("fisheye", new FisheyeEffect());

            // Lighting effects
            RegisterEffect("glow", new GlowEffect());
            RegisterEffect("lens_flare", new LensFlareEffect());
            RegisterEffect("lighting", new LightingEffect());
            RegisterEffect("shadow", new ShadowEffect());
            RegisterEffect("highlight", new HighlightEffect());

            // Particle effects
            if (EnableParticleEffects)
            {
                RegisterEffect("sparkles", new SparklesEffect());
                RegisterEffect("fire", new FireEffect());
                RegisterEffect("smoke", new SmokeEffect());
                RegisterEffect("rain", new RainEffect());
                RegisterEffect("snow", new SnowEffect());
            }

            // Advanced effects
            RegisterEffect("depth_of_field", new DepthOfFieldEffect());
            RegisterEffect("motion_blur", new MotionBlurEffect());
            RegisterEffect("bloom", new BloomEffect());
            RegisterEffect("chromatic_aberration", new ChromaticAberrationEffect());
            RegisterEffect("vignette", new VignetteEffect());
        }

        // Event handlers
        protected virtual void OnEffectApplied(string effectName, Bitmap result)
        {
            EffectApplied?.Invoke(this, new EffectAppliedEventArgs
            {
                EffectName = effectName,
                Result = result,
                Timestamp = DateTime.Now
            });
        }

        protected virtual void OnEffectProgress(string effectName, int percentage, string message)
        {
            EffectProgress?.Invoke(this, new EffectProgressEventArgs
            {
                EffectName = effectName,
                Percentage = percentage,
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        protected virtual void OnEffectError(string message)
        {
            EffectError?.Invoke(this, new EffectErrorEventArgs
            {
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        public void Dispose()
        {
            foreach (var effect in effectLibrary.Values)
            {
                if (effect is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            effectLibrary.Clear();
        }
    }

    // Effect interfaces and base classes
    public interface IEffect
    {
        string Description { get; }
        string Category { get; }
        bool SupportsAnimation { get; }
        bool RequiresGPU { get; }

        Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality);
        List<EffectParameter> GetParameters();
    }

    public abstract class BaseEffect : IEffect, IDisposable
    {
        public abstract string Description { get; }
        public abstract string Category { get; }
        public virtual bool SupportsAnimation => false;
        public virtual bool RequiresGPU => false;

        public abstract Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality);
        public abstract List<EffectParameter> GetParameters();

        protected T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue)
        {
            if (parameters != null && parameters.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        protected ColorMatrix CreateColorMatrix(float brightness = 1.0f, float contrast = 1.0f,
            float saturation = 1.0f, float hue = 0.0f)
        {
            // ColorMatrix'in Matrix özelliği yoktur, doğrudan float[][] kullanırız
            var matrixArray = new float[][]
            {
                new float[] { 1, 0, 0, 0, 0 },
                new float[] { 0, 1, 0, 0, 0 },
                new float[] { 0, 0, 1, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            };

            // Brightness ayarı
            if (brightness != 1.0f)
            {
                matrixArray[4][0] = brightness - 1;
                matrixArray[4][1] = brightness - 1;
                matrixArray[4][2] = brightness - 1;
            }

            // Contrast ayarı
            if (contrast != 1.0f)
            {
                matrixArray[0][0] = contrast;
                matrixArray[1][1] = contrast;
                matrixArray[2][2] = contrast;
                matrixArray[4][0] += 0.5f * (1 - contrast);
                matrixArray[4][1] += 0.5f * (1 - contrast);
                matrixArray[4][2] += 0.5f * (1 - contrast);
            }

            return new ColorMatrix(matrixArray);
        }

        protected unsafe void ProcessPixels(Bitmap bitmap, Func<Color, Color> processor)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                byte* scan0 = (byte*)bitmapData.Scan0.ToPointer();
                int stride = bitmapData.Stride;

                Parallel.For(0, bitmap.Height, y =>
                {
                    byte* row = scan0 + (y * stride);
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int offset = x * 4;
                        var b = row[offset];
                        var g = row[offset + 1];
                        var r = row[offset + 2];
                        var a = row[offset + 3];

                        var color = Color.FromArgb(a, r, g, b);
                        var newColor = processor(color);

                        row[offset] = newColor.B;
                        row[offset + 1] = newColor.G;
                        row[offset + 2] = newColor.R;
                        row[offset + 3] = newColor.A;
                    }
                });
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        public virtual void Dispose()
        {
            // Base implementation - override if needed
        }
    }

    // Concrete effect implementations
    public class BlurEffect : BaseEffect
    {
        public override string Description => "Applies Gaussian blur to the image";
        public override string Category => "Basic";

        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality)
        {
            var radius = GetParameter(parameters, "radius", 2.0f);
            var intensity = GetParameter(parameters, "intensity", 1.0f);

            return ApplyGaussianBlur(source, radius * intensity);
        }

        public override List<EffectParameter> GetParameters()
        {
            return new List<EffectParameter>
            {
                new EffectParameter { Name = "radius", Type = typeof(float), DefaultValue = 2.0f, Min = 0.1f, Max = 20.0f },
                new EffectParameter { Name = "intensity", Type = typeof(float), DefaultValue = 1.0f, Min = 0.0f, Max = 2.0f }
            };
        }

        private Bitmap ApplyGaussianBlur(Bitmap source, float radius)
        {
            var result = new Bitmap(source.Width, source.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                // Create blur effect using convolution
                var kernel = CreateGaussianKernel(radius);
                var convolution = new ConvolutionMatrix(kernel);

                using (var attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(convolution.ToColorMatrix());
                    graphics.DrawImage(source, new Rectangle(0, 0, result.Width, result.Height),
                        0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                }
            }

            return result;
        }

        private float[,] CreateGaussianKernel(float radius)
        {
            int size = (int)(radius * 2) + 1;
            var kernel = new float[size, size];
            float sigma = radius / 3.0f;
            float twoSigmaSquare = 2.0f * sigma * sigma;
            float sum = 0;
            int center = size / 2;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int dx = x - center;
                    int dy = y - center;
                    float distance = dx * dx + dy * dy;
                    kernel[x, y] = (float)Math.Exp(-distance / twoSigmaSquare);
                    sum += kernel[x, y];
                }
            }

            // Normalize
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }
    }

    public class BrightnessEffect : BaseEffect
    {
        public override string Description => "Adjusts image brightness";
        public override string Category => "Color";

        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality)
        {
            var brightness = GetParameter(parameters, "brightness", 0.0f);
            var result = new Bitmap(source);

            ProcessPixels(result, color =>
            {
                int r = Math.Max(0, Math.Min(255, color.R + (int)(brightness * 255)));
                int g = Math.Max(0, Math.Min(255, color.G + (int)(brightness * 255)));
                int b = Math.Max(0, Math.Min(255, color.B + (int)(brightness * 255)));

                return Color.FromArgb(color.A, r, g, b);
            });

            return result;
        }

        public override List<EffectParameter> GetParameters()
        {
            return new List<EffectParameter>
            {
                new EffectParameter { Name = "brightness", Type = typeof(float), DefaultValue = 0.0f, Min = -1.0f, Max = 1.0f }
            };
        }
    }

    public class RippleEffect : BaseEffect
    {
        public override string Description => "Creates ripple distortion effect";
        public override string Category => "Distortion";
        public override bool SupportsAnimation => true;

        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality)
        {
            var amplitude = GetParameter(parameters, "amplitude", 10.0f);
            var frequency = GetParameter(parameters, "frequency", 0.1f);
            var centerX = GetParameter(parameters, "centerX", source.Width / 2.0f);
            var centerY = GetParameter(parameters, "centerY", source.Height / 2.0f);
            var time = GetParameter(parameters, "_time", 0.0f);

            var result = new Bitmap(source.Width, source.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Transparent);

                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        var dx = x - centerX;
                        var dy = y - centerY;
                        var distance = Math.Sqrt(dx * dx + dy * dy);

                        var angle = Math.Atan2(dy, dx);
                        var ripple = Math.Sin(distance * frequency - time * 5) * amplitude;

                        var sourceX = (int)(centerX + (distance + ripple) * Math.Cos(angle));
                        var sourceY = (int)(centerY + (distance + ripple) * Math.Sin(angle));

                        if (sourceX >= 0 && sourceX < source.Width && sourceY >= 0 && sourceY < source.Height)
                        {
                            var pixel = source.GetPixel(sourceX, sourceY);
                            result.SetPixel(x, y, pixel);
                        }
                    }
                }
            }

            return result;
        }

        public override List<EffectParameter> GetParameters()
        {
            return new List<EffectParameter>
            {
                new EffectParameter { Name = "amplitude", Type = typeof(float), DefaultValue = 10.0f, Min = 0.0f, Max = 50.0f },
                new EffectParameter { Name = "frequency", Type = typeof(float), DefaultValue = 0.1f, Min = 0.01f, Max = 1.0f },
                new EffectParameter { Name = "centerX", Type = typeof(float), DefaultValue = 0.0f, Min = 0.0f, Max = 1000.0f },
                new EffectParameter { Name = "centerY", Type = typeof(float), DefaultValue = 0.0f, Min = 0.0f, Max = 1000.0f }
            };
        }
    }

    public class SparklesEffect : BaseEffect
    {
        public override string Description => "Adds animated sparkles to the image";
        public override string Category => "Particle";
        public override bool SupportsAnimation => true;

        private readonly Random random = new Random();

        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality)
        {
            var count = GetParameter(parameters, "count", 20);
            var size = GetParameter(parameters, "size", 3.0f);
            var color = GetParameter(parameters, "color", Color.White);
            var time = GetParameter(parameters, "_time", 0.0f);

            var result = new Bitmap(source);

            using (var graphics = Graphics.FromImage(result))
            {
                using (var brush = new SolidBrush(color))
                {
                    for (int i = 0; i < count; i++)
                    {
                        // Create deterministic but pseudo-random positions based on time and index
                        var seed = i + (int)(time * 10);
                        var rnd = new Random(seed);

                        var x = rnd.Next(0, source.Width);
                        var y = rnd.Next(0, source.Height);
                        var sparkleSize = size * (0.5f + 0.5f * (float)Math.Sin(time * 2 + i));

                        // Draw sparkle as a star
                        DrawStar(graphics, brush, new PointF(x, y), sparkleSize);
                    }
                }
            }

            return result;
        }

        private void DrawStar(Graphics graphics, Brush brush, PointF center, float size)
        {
            var points = new PointF[8];
            for (int i = 0; i < 8; i++)
            {
                var angle = i * Math.PI / 4;
                var radius = (i % 2 == 0) ? size : size * 0.4f;
                points[i] = new PointF(
                    center.X + (float)(radius * Math.Cos(angle)),
                    center.Y + (float)(radius * Math.Sin(angle))
                );
            }

            graphics.FillPolygon(brush, points);
        }

        public override List<EffectParameter> GetParameters()
        {
            return new List<EffectParameter>
            {
                new EffectParameter { Name = "count", Type = typeof(int), DefaultValue = 20, Min = 1, Max = 100 },
                new EffectParameter { Name = "size", Type = typeof(float), DefaultValue = 3.0f, Min = 1.0f, Max = 10.0f },
                new EffectParameter { Name = "color", Type = typeof(Color), DefaultValue = Color.White }
            };
        }
    }

    // Utility classes
    public class ConvolutionMatrix
    {
        private readonly float[,] matrix;

        public ConvolutionMatrix(float[,] matrix)
        {
            this.matrix = matrix;
        }

        public ColorMatrix ToColorMatrix()
        {
            // Convert convolution matrix to color matrix (simplified)
            return new ColorMatrix();
        }
    }

    // Supporting classes and enums
    public enum EffectQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public class EffectDefinition
    {
        public string Name { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class EffectParameter
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object DefaultValue { get; set; }
        public object Min { get; set; }
        public object Max { get; set; }
        public string Description { get; set; }
    }

    public class EffectInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public List<EffectParameter> Parameters { get; set; }
        public bool SupportsAnimation { get; set; }
        public bool RequiresGPU { get; set; }
    }

    public class EffectRenderTask
    {
        public string EffectName { get; set; }
        public Bitmap Source { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public TaskCompletionSource<Bitmap> CompletionSource { get; set; }
    }

    // Event argument classes
    public class EffectAppliedEventArgs : EventArgs
    {
        public string EffectName { get; set; }
        public Bitmap Result { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class EffectProgressEventArgs : EventArgs
    {
        public string EffectName { get; set; }
        public int Percentage { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class EffectErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Stub implementations for remaining effects
    public class SharpenEffect : BaseEffect
    {
        public override string Description => "Sharpens the image";
        public override string Category => "Basic";

        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality)
        {
            var intensity = GetParameter(parameters, "intensity", 1.0f);
            var result = new Bitmap(source);

            // Simple sharpening implementation
            ProcessPixels(result, color => color);

            return result;
        }

        public override List<EffectParameter> GetParameters()
        {
            return new List<EffectParameter>
            {
                new EffectParameter { Name = "intensity", Type = typeof(float), DefaultValue = 1.0f, Min = 0.0f, Max = 3.0f }
            };
        }
    }

    public class EmbossEffect : BaseEffect
    {
        public override string Description => "Creates embossed effect";
        public override string Category => "Artistic";

        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality)
        {
            var depth = GetParameter(parameters, "depth", 1.0f);
            return new Bitmap(source);
        }

        public override List<EffectParameter> GetParameters()
        {
            return new List<EffectParameter>
            {
                new EffectParameter { Name = "depth", Type = typeof(float), DefaultValue = 1.0f, Min = 0.1f, Max = 5.0f }
            };
        }
    }

    // Add all other effect stubs
    public class EdgeDetectionEffect : BaseEffect
    {
        public override string Description => "Detects edges in the image";
        public override string Category => "Basic";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class NoiseEffect : BaseEffect
    {
        public override string Description => "Adds noise to the image";
        public override string Category => "Basic";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class ContrastEffect : BaseEffect
    {
        public override string Description => "Adjusts image contrast";
        public override string Category => "Color";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class SaturationEffect : BaseEffect
    {
        public override string Description => "Adjusts color saturation";
        public override string Category => "Color";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class HueShiftEffect : BaseEffect
    {
        public override string Description => "Shifts hue values";
        public override string Category => "Color";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class SepiaEffect : BaseEffect
    {
        public override string Description => "Applies sepia tone";
        public override string Category => "Color";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class GrayscaleEffect : BaseEffect
    {
        public override string Description => "Converts to grayscale";
        public override string Category => "Color";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class InvertEffect : BaseEffect
    {
        public override string Description => "Inverts colors";
        public override string Category => "Color";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class OilPaintingEffect : BaseEffect
    {
        public override string Description => "Oil painting artistic effect";
        public override string Category => "Artistic";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class WatercolorEffect : BaseEffect
    {
        public override string Description => "Watercolor artistic effect";
        public override string Category => "Artistic";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class PencilSketchEffect : BaseEffect
    {
        public override string Description => "Pencil sketch effect";
        public override string Category => "Artistic";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class CartoonEffect : BaseEffect
    {
        public override string Description => "Cartoon-style effect";
        public override string Category => "Artistic";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class VintageEffect : BaseEffect
    {
        public override string Description => "Vintage photo effect";
        public override string Category => "Artistic";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class WaveEffect : BaseEffect
    {
        public override string Description => "Wave distortion effect";
        public override string Category => "Distortion";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class TwirlEffect : BaseEffect
    {
        public override string Description => "Twirl distortion effect";
        public override string Category => "Distortion";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class PinchEffect : BaseEffect
    {
        public override string Description => "Pinch distortion effect";
        public override string Category => "Distortion";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class FisheyeEffect : BaseEffect
    {
        public override string Description => "Fisheye lens effect";
        public override string Category => "Distortion";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class GlowEffect : BaseEffect
    {
        public override string Description => "Glow lighting effect";
        public override string Category => "Lighting";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class LensFlareEffect : BaseEffect
    {
        public override string Description => "Lens flare effect";
        public override string Category => "Lighting";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class LightingEffect : BaseEffect
    {
        public override string Description => "Dynamic lighting effect";
        public override string Category => "Lighting";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class ShadowEffect : BaseEffect
    {
        public override string Description => "Drop shadow effect";
        public override string Category => "Lighting";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class HighlightEffect : BaseEffect
    {
        public override string Description => "Highlight effect";
        public override string Category => "Lighting";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class FireEffect : BaseEffect
    {
        public override string Description => "Fire particle effect";
        public override string Category => "Particle";
        public override bool SupportsAnimation => true;
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class SmokeEffect : BaseEffect
    {
        public override string Description => "Smoke particle effect";
        public override string Category => "Particle";
        public override bool SupportsAnimation => true;
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class RainEffect : BaseEffect
    {
        public override string Description => "Rain particle effect";
        public override string Category => "Particle";
        public override bool SupportsAnimation => true;
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class SnowEffect : BaseEffect
    {
        public override string Description => "Snow particle effect";
        public override string Category => "Particle";
        public override bool SupportsAnimation => true;
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class DepthOfFieldEffect : BaseEffect
    {
        public override string Description => "Depth of field blur effect";
        public override string Category => "Advanced";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class MotionBlurEffect : BaseEffect
    {
        public override string Description => "Motion blur effect";
        public override string Category => "Advanced";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class BloomEffect : BaseEffect
    {
        public override string Description => "Bloom lighting effect";
        public override string Category => "Advanced";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class ChromaticAberrationEffect : BaseEffect
    {
        public override string Description => "Chromatic aberration effect";
        public override string Category => "Advanced";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }

    public class VignetteEffect : BaseEffect
    {
        public override string Description => "Vignette edge darkening effect";
        public override string Category => "Advanced";
        public override Bitmap Apply(Bitmap source, Dictionary<string, object> parameters, EffectQuality quality) => new Bitmap(source);
        public override List<EffectParameter> GetParameters() => new List<EffectParameter>();
    }
}