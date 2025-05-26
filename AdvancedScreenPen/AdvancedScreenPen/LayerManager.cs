using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JsonFormatting = Newtonsoft.Json.Formatting;
using Newtonsoft.Json;

namespace AdvancedScreenPen
{
    public class LayerManager : IDisposable
    {
        private readonly List<AnnotationLayer> layers;
        private AnnotationLayer activeLayer;
        private readonly Stack<LayerAction> undoStack;
        private readonly Stack<LayerAction> redoStack;
        private int nextLayerId = 1;

        // Layer management settings
        public int MaxLayers { get; set; } = 50;
        public int MaxUndoActions { get; set; } = 100;
        public bool AutoMergeEmpty { get; set; } = true;
        public bool PreserveLayerHistory { get; set; } = true;

        // Advanced features
        public bool EnableBlending { get; set; } = true;
        public bool EnableMasks { get; set; } = true;
        public bool EnableEffects { get; set; } = true;
        public bool EnableGrouping { get; set; } = true;

        // Events
        public event EventHandler<LayerEventArgs> LayerAdded;
        public event EventHandler<LayerEventArgs> LayerRemoved;
        public event EventHandler<LayerEventArgs> LayerChanged;
        public event EventHandler<LayerEventArgs> ActiveLayerChanged;
        public event EventHandler<LayerActionEventArgs> ActionPerformed;

        // Properties
        public IReadOnlyList<AnnotationLayer> Layers => layers.AsReadOnly();
        public AnnotationLayer ActiveLayer => activeLayer;
        public int LayerCount => layers.Count;
        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public LayerManager()
        {
            layers = new List<AnnotationLayer>();
            undoStack = new Stack<LayerAction>();
            redoStack = new Stack<LayerAction>();

            // Create default layer
            CreateDefaultLayer();
        }

        public AnnotationLayer CreateLayer(string name = null, bool setAsActive = true)
        {
            if (layers.Count >= MaxLayers)
            {
                throw new InvalidOperationException($"Maximum number of layers ({MaxLayers}) reached");
            }

            var layer = new AnnotationLayer
            {
                Name = name ?? $"Layer {nextLayerId}",
                LayerId = nextLayerId++,
                IsVisible = true,
                IsLocked = false,
                Opacity = 1.0f,
                BlendMode = BlendMode.Normal,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
                CreatedBy = Environment.UserName
            };

            layers.Add(layer);

            if (setAsActive)
            {
                SetActiveLayer(layer);
            }

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Create,
                Layer = layer,
                Description = $"Created layer '{layer.Name}'"
            });

            OnLayerAdded(layer);
            return layer;
        }

        public bool RemoveLayer(AnnotationLayer layer)
        {
            if (layer == null || !layers.Contains(layer))
                return false;

            if (layers.Count == 1)
            {
                // Don't allow removing the last layer
                return false;
            }

            var index = layers.IndexOf(layer);
            layers.Remove(layer);

            // If this was the active layer, set a new active layer
            if (activeLayer == layer)
            {
                var newActiveIndex = Math.Min(index, layers.Count - 1);
                SetActiveLayer(layers[newActiveIndex]);
            }

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Delete,
                Layer = layer,
                Index = index,
                Description = $"Deleted layer '{layer.Name}'"
            });

            OnLayerRemoved(layer);
            return true;
        }

        public bool RemoveLayer(int index)
        {
            if (index < 0 || index >= layers.Count)
                return false;

            return RemoveLayer(layers[index]);
        }

        public void SetActiveLayer(AnnotationLayer layer)
        {
            if (layer == null || !layers.Contains(layer))
                return;

            var previousLayer = activeLayer;
            activeLayer = layer;

            OnActiveLayerChanged(layer);

            if (previousLayer != layer)
            {
                RecordAction(new LayerAction
                {
                    Type = LayerActionType.Activate,
                    Layer = layer,
                    PreviousLayer = previousLayer,
                    Description = $"Activated layer '{layer.Name}'"
                });
            }
        }

        public void SetActiveLayer(int index)
        {
            if (index >= 0 && index < layers.Count)
            {
                SetActiveLayer(layers[index]);
            }
        }

        public bool MoveLayer(AnnotationLayer layer, int newIndex)
        {
            if (layer == null || !layers.Contains(layer))
                return false;

            if (newIndex < 0 || newIndex >= layers.Count)
                return false;

            var oldIndex = layers.IndexOf(layer);
            if (oldIndex == newIndex)
                return true;

            layers.RemoveAt(oldIndex);
            layers.Insert(newIndex, layer);

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Move,
                Layer = layer,
                Index = oldIndex,
                NewIndex = newIndex,
                Description = $"Moved layer '{layer.Name}' from position {oldIndex} to {newIndex}"
            });

            OnLayerChanged(layer);
            return true;
        }

        public bool MoveLayerUp(AnnotationLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index > 0)
            {
                return MoveLayer(layer, index - 1);
            }
            return false;
        }

        public bool MoveLayerDown(AnnotationLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index < layers.Count - 1)
            {
                return MoveLayer(layer, index + 1);
            }
            return false;
        }

        public AnnotationLayer DuplicateLayer(AnnotationLayer layer)
        {
            if (layer == null || !layers.Contains(layer))
                return null;

            var duplicated = layer.Clone();
            duplicated.Name = $"{layer.Name} Copy";
            duplicated.LayerId = nextLayerId++;
            duplicated.CreatedTime = DateTime.Now;
            duplicated.ModifiedTime = DateTime.Now;

            var index = layers.IndexOf(layer);
            layers.Insert(index + 1, duplicated);

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Duplicate,
                Layer = duplicated,
                SourceLayer = layer,
                Index = index + 1,
                Description = $"Duplicated layer '{layer.Name}'"
            });

            OnLayerAdded(duplicated);
            return duplicated;
        }

        public AnnotationLayer MergeLayers(AnnotationLayer layer1, AnnotationLayer layer2, string newName = null)
        {
            if (layer1 == null || layer2 == null || !layers.Contains(layer1) || !layers.Contains(layer2))
                return null;

            var mergedLayer = new AnnotationLayer
            {
                Name = newName ?? $"{layer1.Name} + {layer2.Name}",
                LayerId = nextLayerId++,
                IsVisible = true,
                IsLocked = false,
                Opacity = 1.0f,
                BlendMode = BlendMode.Normal,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
                CreatedBy = Environment.UserName
            };

            // Merge strokes from both layers
            mergedLayer.Strokes.AddRange(layer1.Strokes);
            mergedLayer.Strokes.AddRange(layer2.Strokes);

            // Update stroke layer IDs
            foreach (var stroke in mergedLayer.Strokes)
            {
                stroke.LayerId = mergedLayer.LayerId;
            }

            // Insert merged layer at the position of the first layer
            var index = Math.Min(layers.IndexOf(layer1), layers.IndexOf(layer2));
            layers.Insert(index, mergedLayer);

            // Remove original layers
            layers.Remove(layer1);
            layers.Remove(layer2);

            // Set as active if one of the merged layers was active
            if (activeLayer == layer1 || activeLayer == layer2)
            {
                SetActiveLayer(mergedLayer);
            }

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Merge,
                Layer = mergedLayer,
                SourceLayer = layer1,
                SecondaryLayer = layer2,
                Index = index,
                Description = $"Merged layers '{layer1.Name}' and '{layer2.Name}'"
            });

            OnLayerAdded(mergedLayer);
            OnLayerRemoved(layer1);
            OnLayerRemoved(layer2);

            return mergedLayer;
        }

        public void SetLayerVisibility(AnnotationLayer layer, bool visible)
        {
            if (layer == null || !layers.Contains(layer))
                return;

            var oldVisibility = layer.IsVisible;
            layer.IsVisible = visible;
            layer.ModifiedTime = DateTime.Now;

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Visibility,
                Layer = layer,
                OldValue = oldVisibility,
                NewValue = visible,
                Description = $"{(visible ? "Showed" : "Hid")} layer '{layer.Name}'"
            });

            OnLayerChanged(layer);
        }

        public void SetLayerLock(AnnotationLayer layer, bool locked)
        {
            if (layer == null || !layers.Contains(layer))
                return;

            var oldLock = layer.IsLocked;
            layer.IsLocked = locked;
            layer.ModifiedTime = DateTime.Now;

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Lock,
                Layer = layer,
                OldValue = oldLock,
                NewValue = locked,
                Description = $"{(locked ? "Locked" : "Unlocked")} layer '{layer.Name}'"
            });

            OnLayerChanged(layer);
        }

        public void SetLayerOpacity(AnnotationLayer layer, float opacity)
        {
            if (layer == null || !layers.Contains(layer))
                return;

            opacity = Math.Max(0f, Math.Min(1f, opacity));
            var oldOpacity = layer.Opacity;
            layer.Opacity = opacity;
            layer.ModifiedTime = DateTime.Now;

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Opacity,
                Layer = layer,
                OldValue = oldOpacity,
                NewValue = opacity,
                Description = $"Changed opacity of layer '{layer.Name}' to {opacity:P0}"
            });

            OnLayerChanged(layer);
        }

        public void SetLayerBlendMode(AnnotationLayer layer, BlendMode blendMode)
        {
            if (layer == null || !layers.Contains(layer))
                return;

            var oldBlendMode = layer.BlendMode;
            layer.BlendMode = blendMode;
            layer.ModifiedTime = DateTime.Now;

            RecordAction(new LayerAction
            {
                Type = LayerActionType.BlendMode,
                Layer = layer,
                OldValue = oldBlendMode,
                NewValue = blendMode,
                Description = $"Changed blend mode of layer '{layer.Name}' to {blendMode}"
            });

            OnLayerChanged(layer);
        }

        public void RenameLayer(AnnotationLayer layer, string newName)
        {
            if (layer == null || !layers.Contains(layer) || string.IsNullOrWhiteSpace(newName))
                return;

            var oldName = layer.Name;
            layer.Name = newName.Trim();
            layer.ModifiedTime = DateTime.Now;

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Rename,
                Layer = layer,
                OldValue = oldName,
                NewValue = newName,
                Description = $"Renamed layer from '{oldName}' to '{newName}'"
            });

            OnLayerChanged(layer);
        }

        public void ClearLayer(AnnotationLayer layer)
        {
            if (layer == null || !layers.Contains(layer) || layer.IsLocked)
                return;

            var clearedStrokes = new List<AdvancedStroke>(layer.Strokes);
            layer.ClearStrokes();

            RecordAction(new LayerAction
            {
                Type = LayerActionType.Clear,
                Layer = layer,
                Strokes = clearedStrokes,
                Description = $"Cleared layer '{layer.Name}'"
            });

            OnLayerChanged(layer);
        }

        public void ClearAllLayers()
        {
            var actions = new List<LayerAction>();

            foreach (var layer in layers)
            {
                if (!layer.IsLocked && layer.Strokes.Count > 0)
                {
                    actions.Add(new LayerAction
                    {
                        Type = LayerActionType.Clear,
                        Layer = layer,
                        Strokes = new List<AdvancedStroke>(layer.Strokes),
                        Description = $"Cleared layer '{layer.Name}'"
                    });

                    layer.ClearStrokes();
                    OnLayerChanged(layer);
                }
            }

            if (actions.Any())
            {
                RecordAction(new LayerAction
                {
                    Type = LayerActionType.ClearAll,
                    CompositeActions = actions,
                    Description = "Cleared all layers"
                });
            }
        }

        public List<AnnotationLayer> GetVisibleLayers()
        {
            return layers.Where(l => l.IsVisible).ToList();
        }

        public List<AnnotationLayer> GetUnlockedLayers()
        {
            return layers.Where(l => !l.IsLocked).ToList();
        }

        public AnnotationLayer FindLayerById(int id)
        {
            return layers.FirstOrDefault(l => l.LayerId == id);
        }

        public AnnotationLayer FindLayerByName(string name)
        {
            return layers.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public bool Undo()
        {
            if (undoStack.Count == 0)
                return false;

            var action = undoStack.Pop();

            try
            {
                UndoAction(action);
                redoStack.Push(action);

                OnActionPerformed(action, true);
                return true;
            }
            catch (Exception ex)
            {
                // Re-add action to undo stack if undo failed
                undoStack.Push(action);
                throw new InvalidOperationException($"Failed to undo action: {ex.Message}", ex);
            }
        }

        public bool Redo()
        {
            if (redoStack.Count == 0)
                return false;

            var action = redoStack.Pop();

            try
            {
                RedoAction(action);
                undoStack.Push(action);

                OnActionPerformed(action, false);
                return true;
            }
            catch (Exception ex)
            {
                // Re-add action to redo stack if redo failed
                redoStack.Push(action);
                throw new InvalidOperationException($"Failed to redo action: {ex.Message}", ex);
            }
        }

        public async Task<bool> SaveLayersAsync(string filePath)
        {
            try
            {
                var layerData = new LayerData
                {
                    Version = "2.0",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = Environment.UserName,
                    Layers = layers.Select(SerializeLayer).ToList(),
                    ActiveLayerId = activeLayer?.LayerId ?? 0
                };

                var json = JsonConvert.SerializeObject(layerData, JsonFormatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save layers: {ex.Message}", ex);
            }
        }

        public async Task<bool> LoadLayersAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var layerData = JsonConvert.DeserializeObject<LayerData>(json);

                // Clear existing layers
                layers.Clear();
                undoStack.Clear();
                redoStack.Clear();
                nextLayerId = 1;

                // Load layers
                foreach (var layerInfo in layerData.Layers)
                {
                    var layer = DeserializeLayer(layerInfo);
                    layers.Add(layer);
                    nextLayerId = Math.Max(nextLayerId, layer.LayerId + 1);
                }

                // Set active layer
                if (layerData.ActiveLayerId > 0)
                {
                    var activeLayerToSet = FindLayerById(layerData.ActiveLayerId);
                    if (activeLayerToSet != null)
                    {
                        SetActiveLayer(activeLayerToSet);
                    }
                }

                if (layers.Count == 0)
                {
                    CreateDefaultLayer();
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load layers: {ex.Message}", ex);
            }
        }

        public Bitmap RenderLayers(Size size, List<AnnotationLayer> layersToRender = null)
        {
            var targetLayers = layersToRender ?? GetVisibleLayers();
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                var drawingEngine = new DrawingEngine(graphics, size);

                foreach (var layer in targetLayers.OrderBy(l => layers.IndexOf(l)))
                {
                    if (!layer.IsVisible)
                        continue;

                    foreach (var stroke in layer.Strokes)
                    {
                        drawingEngine.DrawStroke(stroke, layer.Opacity);
                    }
                }

                drawingEngine.Dispose();
            }

            return bitmap;
        }

        private void CreateDefaultLayer()
        {
            var defaultLayer = new AnnotationLayer
            {
                Name = "Layer 1",
                LayerId = nextLayerId++,
                IsVisible = true,
                IsLocked = false,
                Opacity = 1.0f,
                BlendMode = BlendMode.Normal,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
                CreatedBy = Environment.UserName
            };

            layers.Add(defaultLayer);
            activeLayer = defaultLayer;
        }

        private void RecordAction(LayerAction action)
        {
            if (!PreserveLayerHistory)
                return;

            action.Timestamp = DateTime.Now;
            undoStack.Push(action);

            // Clear redo stack when new action is performed
            redoStack.Clear();

            // Limit undo stack size
            if (undoStack.Count > MaxUndoActions)
            {
                var actionsToRemove = undoStack.Count - MaxUndoActions;
                var tempStack = new Stack<LayerAction>();

                // Keep only the most recent actions
                for (int i = 0; i < MaxUndoActions; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }

                undoStack.Clear();

                while (tempStack.Count > 0)
                {
                    undoStack.Push(tempStack.Pop());
                }
            }
        }

        private void UndoAction(LayerAction action)
        {
            switch (action.Type)
            {
                case LayerActionType.Create:
                    layers.Remove(action.Layer);
                    if (activeLayer == action.Layer)
                    {
                        SetActiveLayer(layers.LastOrDefault());
                    }
                    break;

                case LayerActionType.Delete:
                    layers.Insert(action.Index ?? layers.Count, action.Layer);
                    break;

                case LayerActionType.Move:
                    MoveLayer(action.Layer, action.Index ?? 0);
                    break;

                case LayerActionType.Duplicate:
                    layers.Remove(action.Layer);
                    break;

                case LayerActionType.Merge:
                    // This would require more complex logic to restore original layers
                    break;

                case LayerActionType.Visibility:
                    action.Layer.IsVisible = (bool)action.OldValue;
                    break;

                case LayerActionType.Lock:
                    action.Layer.IsLocked = (bool)action.OldValue;
                    break;

                case LayerActionType.Opacity:
                    action.Layer.Opacity = (float)action.OldValue;
                    break;

                case LayerActionType.BlendMode:
                    action.Layer.BlendMode = (BlendMode)action.OldValue;
                    break;

                case LayerActionType.Rename:
                    action.Layer.Name = (string)action.OldValue;
                    break;

                case LayerActionType.Clear:
                    if (action.Strokes != null)
                    {
                        action.Layer.Strokes.AddRange(action.Strokes);
                    }
                    break;

                case LayerActionType.ClearAll:
                    if (action.CompositeActions != null)
                    {
                        foreach (var subAction in action.CompositeActions)
                        {
                            if (subAction.Strokes != null)
                            {
                                subAction.Layer.Strokes.AddRange(subAction.Strokes);
                            }
                        }
                    }
                    break;

                case LayerActionType.Activate:
                    if (action.PreviousLayer != null)
                    {
                        SetActiveLayer(action.PreviousLayer);
                    }
                    break;
            }
        }

        private void RedoAction(LayerAction action)
        {
            switch (action.Type)
            {
                case LayerActionType.Create:
                    layers.Add(action.Layer);
                    SetActiveLayer(action.Layer);
                    break;

                case LayerActionType.Delete:
                    layers.Remove(action.Layer);
                    if (activeLayer == action.Layer)
                    {
                        SetActiveLayer(layers.LastOrDefault());
                    }
                    break;

                case LayerActionType.Move:
                    MoveLayer(action.Layer, action.NewIndex ?? 0);
                    break;

                case LayerActionType.Duplicate:
                    layers.Insert(action.Index ?? layers.Count, action.Layer);
                    break;

                case LayerActionType.Visibility:
                    action.Layer.IsVisible = (bool)action.NewValue;
                    break;

                case LayerActionType.Lock:
                    action.Layer.IsLocked = (bool)action.NewValue;
                    break;

                case LayerActionType.Opacity:
                    action.Layer.Opacity = (float)action.NewValue;
                    break;

                case LayerActionType.BlendMode:
                    action.Layer.BlendMode = (BlendMode)action.NewValue;
                    break;

                case LayerActionType.Rename:
                    action.Layer.Name = (string)action.NewValue;
                    break;

                case LayerActionType.Clear:
                    action.Layer.ClearStrokes();
                    break;

                case LayerActionType.ClearAll:
                    if (action.CompositeActions != null)
                    {
                        foreach (var subAction in action.CompositeActions)
                        {
                            subAction.Layer.ClearStrokes();
                        }
                    }
                    break;

                case LayerActionType.Activate:
                    SetActiveLayer(action.Layer);
                    break;
            }
        }

        private LayerSerializationInfo SerializeLayer(AnnotationLayer layer)
        {
            return new LayerSerializationInfo
            {
                LayerId = layer.LayerId,
                Name = layer.Name,
                IsVisible = layer.IsVisible,
                IsLocked = layer.IsLocked,
                Opacity = layer.Opacity,
                BlendMode = layer.BlendMode,
                BackgroundColor = layer.BackgroundColor,
                HasBackground = layer.HasBackground,
                CreatedTime = layer.CreatedTime,
                ModifiedTime = layer.ModifiedTime,
                CreatedBy = layer.CreatedBy,
                Strokes = layer.Strokes.Select(SerializeStroke).ToList(),
                LayerProperties = layer.LayerProperties
            };
        }

        private AdvancedStroke SerializeStroke(AdvancedStroke stroke)
        {
            // Return a copy of the stroke with all its properties
            return stroke.Clone();
        }

        private AnnotationLayer DeserializeLayer(LayerSerializationInfo info)
        {
            var layer = new AnnotationLayer
            {
                LayerId = info.LayerId,
                Name = info.Name,
                IsVisible = info.IsVisible,
                IsLocked = info.IsLocked,
                Opacity = info.Opacity,
                BlendMode = info.BlendMode,
                BackgroundColor = info.BackgroundColor,
                HasBackground = info.HasBackground,
                CreatedTime = info.CreatedTime,
                ModifiedTime = info.ModifiedTime,
                CreatedBy = info.CreatedBy,
                LayerProperties = info.LayerProperties ?? new Dictionary<string, object>()
            };

            if (info.Strokes != null)
            {
                layer.Strokes.AddRange(info.Strokes);
            }

            return layer;
        }

        // Event handlers
        protected virtual void OnLayerAdded(AnnotationLayer layer)
        {
            LayerAdded?.Invoke(this, new LayerEventArgs { Layer = layer });
        }

        protected virtual void OnLayerRemoved(AnnotationLayer layer)
        {
            LayerRemoved?.Invoke(this, new LayerEventArgs { Layer = layer });
        }

        protected virtual void OnLayerChanged(AnnotationLayer layer)
        {
            LayerChanged?.Invoke(this, new LayerEventArgs { Layer = layer });
        }

        protected virtual void OnActiveLayerChanged(AnnotationLayer layer)
        {
            ActiveLayerChanged?.Invoke(this, new LayerEventArgs { Layer = layer });
        }

        protected virtual void OnActionPerformed(LayerAction action, bool isUndo)
        {
            ActionPerformed?.Invoke(this, new LayerActionEventArgs { Action = action, IsUndo = isUndo });
        }

        public void Dispose()
        {
            // Clean up any resources
            layers.Clear();
            undoStack.Clear();
            redoStack.Clear();
        }
    }

    // Supporting classes and enums
    public enum LayerActionType
    {
        Create,
        Delete,
        Move,
        Duplicate,
        Merge,
        Visibility,
        Lock,
        Opacity,
        BlendMode,
        Rename,
        Clear,
        ClearAll,
        Activate
    }

    public class LayerAction
    {
        public LayerActionType Type { get; set; }
        public AnnotationLayer Layer { get; set; }
        public AnnotationLayer SourceLayer { get; set; }
        public AnnotationLayer SecondaryLayer { get; set; }
        public AnnotationLayer PreviousLayer { get; set; }
        public int? Index { get; set; }
        public int? NewIndex { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public List<AdvancedStroke> Strokes { get; set; }
        public List<LayerAction> CompositeActions { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LayerData
    {
        public string Version { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public List<LayerSerializationInfo> Layers { get; set; }
        public int ActiveLayerId { get; set; }
    }

    public class LayerSerializationInfo
    {
        public int LayerId { get; set; }
        public string Name { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }
        public float Opacity { get; set; }
        public BlendMode BlendMode { get; set; }
        public Color BackgroundColor { get; set; }
        public bool HasBackground { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string CreatedBy { get; set; }
        public List<AdvancedStroke> Strokes { get; set; }
        public Dictionary<string, object> LayerProperties { get; set; }
    }

    // Event argument classes
    public class LayerEventArgs : EventArgs
    {
        public AnnotationLayer Layer { get; set; }
    }

    public class LayerActionEventArgs : EventArgs
    {
        public LayerAction Action { get; set; }
        public bool IsUndo { get; set; }
    }
}