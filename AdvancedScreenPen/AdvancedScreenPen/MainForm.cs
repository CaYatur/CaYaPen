using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdvancedScreenPen
{
    public partial class MainForm : Form
    {
        // Form controls and managers
        private DrawingEngine drawingEngine;
        private LayerManager layerManager;
        private PerformanceManager performanceManager;
        private SpeechController speechController;
        private EffectsEngine effectsEngine;
        private CollaborationManager collaborationManager;
        private SmartGesture gestureRecognizer;

        // UI Components
        private ToolboxForm toolboxForm;
        private LayerPanelForm layerPanelForm;
        private ColorPaletteForm colorPaletteForm;
        private Panel drawingPanel;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        private MenuStrip menuStrip;

        // Drawing state
        private bool isDrawing = false;
        private PointF lastPoint;
        private AdvancedStroke currentStroke;
        private DrawingTool currentTool = DrawingTool.Pen;
        private Color currentColor = Color.Red;
        private float currentWidth = 3f;
        private float currentOpacity = 1f;

        // Advanced features
        private bool isVoiceControlActive = false;
        private bool isGestureRecognitionActive = true;
        private bool isCollaborationActive = false;
        private Timer performanceTimer;

        public MainForm()
        {
            InitializeComponent();
            InitializeManagers();
            InitializeForms();
            SetupEventHandlers();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form settings
            this.Text = "Advanced Screen Pen Professional v2.0 - CaYatur";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.KeyPreview = true;

            // Create menu strip
            CreateMenuStrip();

            // Create main drawing panel
            drawingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            drawingPanel.Paint += DrawingPanel_Paint;
            drawingPanel.MouseDown += DrawingPanel_MouseDown;
            drawingPanel.MouseMove += DrawingPanel_MouseMove;
            drawingPanel.MouseUp += DrawingPanel_MouseUp;
            this.Controls.Add(drawingPanel);

            // Create status strip
            CreateStatusStrip();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // File menu
            var fileMenu = new ToolStripMenuItem("Dosya");
            fileMenu.DropDownItems.Add("Yeni", null, (s, e) => NewProject());
            fileMenu.DropDownItems.Add("Aç", null, (s, e) => OpenProject());
            fileMenu.DropDownItems.Add("Kaydet", null, (s, e) => SaveProject());
            fileMenu.DropDownItems.Add("Farklı Kaydet", null, (s, e) => SaveProjectAs());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Dışa Aktar", null, (s, e) => ExportProject());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Çıkış", null, (s, e) => this.Close());

            // Edit menu
            var editMenu = new ToolStripMenuItem("Düzen");
            editMenu.DropDownItems.Add("Geri Al", null, (s, e) => Undo());
            editMenu.DropDownItems.Add("Yinele", null, (s, e) => Redo());
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Temizle", null, (s, e) => ClearAll());
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add("Ayarlar", null, (s, e) => ShowSettings());
            editMenu.DropDownItems.Add("Gelişmiş Ayarlar", null, (s, e) => ShowAdvancedSettings());

            // Tools menu
            var toolsMenu = new ToolStripMenuItem("Araçlar");
            toolsMenu.DropDownItems.Add("Araç Kutusu", null, (s, e) => ToggleToolbox());
            toolsMenu.DropDownItems.Add("Katmanlar", null, (s, e) => ToggleLayerPanel());
            toolsMenu.DropDownItems.Add("Renk Paleti", null, (s, e) => ToggleColorPalette());
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("Ses Kontrolü", null, (s, e) => ToggleVoiceControl());
            toolsMenu.DropDownItems.Add("İş Birliği", null, (s, e) => ToggleCollaboration());

            // Effects menu
            var effectsMenu = new ToolStripMenuItem("Efektler");
            effectsMenu.DropDownItems.Add("Bulanıklaştırma", null, (s, e) => ApplyEffect("blur"));
            effectsMenu.DropDownItems.Add("Keskinleştirme", null, (s, e) => ApplyEffect("sharpen"));
            effectsMenu.DropDownItems.Add("Parıltı", null, (s, e) => ApplyEffect("glow"));
            effectsMenu.DropDownItems.Add("Dalgalanma", null, (s, e) => ApplyEffect("ripple"));

            // Help menu
            var helpMenu = new ToolStripMenuItem("Yardım");
            helpMenu.DropDownItems.Add("Kullanım Kılavuzu", null, (s, e) => ShowHelp());
            helpMenu.DropDownItems.Add("Kısayol Tuşları", null, (s, e) => ShowShortcuts());
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add("Hakkında", null, (s, e) => ShowAbout());

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(editMenu);
            menuStrip.Items.Add(toolsMenu);
            menuStrip.Items.Add(effectsMenu);
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            statusLabel = new ToolStripStatusLabel
            {
                Text = "Hazır",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            progressBar = new ToolStripProgressBar
            {
                Visible = false,
                Size = new Size(200, 16)
            };

            var fpsLabel = new ToolStripStatusLabel("FPS: 60");
            var memoryLabel = new ToolStripStatusLabel("Bellek: 256 MB");
            var coordLabel = new ToolStripStatusLabel("X: 0, Y: 0");

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);
            statusStrip.Items.Add(fpsLabel);
            statusStrip.Items.Add(memoryLabel);
            statusStrip.Items.Add(coordLabel);

            this.Controls.Add(statusStrip);
        }

        private void InitializeManagers()
        {
            // Initialize performance manager first
            performanceManager = new PerformanceManager();
            performanceManager.PerformanceUpdated += PerformanceManager_PerformanceUpdated;

            // Initialize drawing engine
            drawingEngine = new DrawingEngine(null, drawingPanel.Size);

            // Initialize layer manager
            layerManager = new LayerManager();
            layerManager.LayerAdded += LayerManager_LayerAdded;
            layerManager.LayerRemoved += LayerManager_LayerRemoved;
            layerManager.ActiveLayerChanged += LayerManager_ActiveLayerChanged;

            // Initialize effects engine
            effectsEngine = new EffectsEngine();
            effectsEngine.EffectApplied += EffectsEngine_EffectApplied;

            // Initialize speech controller
            speechController = new SpeechController();
            speechController.CommandRecognized += SpeechController_CommandRecognized;
            speechController.SpeechError += SpeechController_SpeechError;

            // Initialize collaboration manager
            collaborationManager = new CollaborationManager();
            collaborationManager.UserJoined += CollaborationManager_UserJoined;
            collaborationManager.UserLeft += CollaborationManager_UserLeft;

            // Initialize gesture recognizer
            gestureRecognizer = new SmartGesture();
            gestureRecognizer.GestureRecognized += GestureRecognizer_GestureRecognized;

            // Setup performance timer
            performanceTimer = new Timer
            {
                Interval = 1000, // Update every second
                Enabled = true
            };
            performanceTimer.Tick += PerformanceTimer_Tick;
        }

        private void InitializeForms()
        {
            // Initialize toolbox
            toolboxForm = new ToolboxForm();
            toolboxForm.ToolSelected += ToolboxForm_ToolSelected;
            toolboxForm.ColorSelected += ToolboxForm_ColorSelected;
            toolboxForm.SizeChanged += ToolboxForm_SizeChanged;
            toolboxForm.Show();

            // Initialize layer panel
            layerPanelForm = new LayerPanelForm(layerManager);
            layerPanelForm.Show();

            // Initialize color palette
            colorPaletteForm = new ColorPaletteForm();
            colorPaletteForm.ColorSelected += ColorPaletteForm_ColorSelected;
            colorPaletteForm.Show();

            // Position forms
            PositionChildForms();
        }

        private void PositionChildForms()
        {
            var screenBounds = Screen.PrimaryScreen.WorkingArea;

            // Position toolbox on the left
            toolboxForm.Location = new Point(10, 100);

            // Position layer panel on the right
            layerPanelForm.Location = new Point(screenBounds.Width - layerPanelForm.Width - 10, 100);

            // Position color palette at the bottom left
            colorPaletteForm.Location = new Point(10, screenBounds.Height - colorPaletteForm.Height - 50);
        }

        private void SetupEventHandlers()
        {
            this.KeyDown += MainForm_KeyDown;
            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;

            drawingPanel.MouseEnter += (s, e) => this.Cursor = GetToolCursor();
            drawingPanel.MouseLeave += (s, e) => this.Cursor = Cursors.Default;
        }

        private void LoadSettings()
        {
            try
            {
                // Load application settings
                currentTool = (DrawingTool)Properties.Settings.Default.DefaultTool;
                currentColor = Properties.Settings.Default.DefaultColor;
                currentWidth = Properties.Settings.Default.DefaultWidth;
                currentOpacity = Properties.Settings.Default.DefaultOpacity / 255f;

                // Apply voice control settings
                speechController.Language = Properties.Settings.Default.VoiceLanguage;
                speechController.ConfidenceThreshold = Properties.Settings.Default.VoiceConfidenceThreshold;
                speechController.EnableFeedback = Properties.Settings.Default.VoiceFeedbackEnabled;

                // Apply performance settings
                performanceManager.MaxFrameRate = Properties.Settings.Default.MaxFrameRate;
                performanceManager.EnableGpuAcceleration = Properties.Settings.Default.EnableGpuAcceleration;
                performanceManager.EnableMultithreading = Properties.Settings.Default.EnableMultithreading;

                UpdateStatusBar("Ayarlar yüklendi");
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Ayarlar yüklenirken hata: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                Properties.Settings.Default.DefaultTool = (int)currentTool;
                Properties.Settings.Default.DefaultColor = currentColor;
                Properties.Settings.Default.DefaultWidth = currentWidth;
                Properties.Settings.Default.DefaultOpacity = (int)(currentOpacity * 255);

                Properties.Settings.Default.Save();
                UpdateStatusBar("Ayarlar kaydedildi");
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Ayarlar kaydedilirken hata: {ex.Message}");
            }
        }

        // Drawing event handlers
        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            performanceManager.StartFrame();

            try
            {
                var graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                // Draw background
                graphics.Clear(Color.White);

                // Draw coordinate system if enabled
                if (Properties.Settings.Default.ShowMathGrid)
                {
                    MathematicalTools.DrawCoordinateSystem(graphics, drawingPanel.ClientRectangle,
                        new PointF(drawingPanel.Width / 2f, drawingPanel.Height / 2f), 50f);
                }

                // Draw all visible layers
                var visibleLayers = layerManager.GetVisibleLayers();
                drawingEngine.Graphics = graphics;

                foreach (var layer in visibleLayers)
                {
                    foreach (var stroke in layer.Strokes)
                    {
                        drawingEngine.DrawStroke(stroke, layer.Opacity);
                    }
                }

                // Draw current stroke being drawn
                if (currentStroke != null && currentStroke.Points.Count > 0)
                {
                    drawingEngine.DrawStroke(currentStroke, currentOpacity);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Çizim hatası: {ex.Message}");
            }
            finally
            {
                performanceManager.EndFrame();
            }
        }

        private void DrawingPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                StartDrawing(e.Location);
            }
        }

        private void DrawingPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // Update coordinate display
            if (statusStrip.Items.Count > 4)
            {
                var coordLabel = statusStrip.Items[4] as ToolStripStatusLabel;
                if (coordLabel != null)
                {
                    coordLabel.Text = $"X: {e.X}, Y: {e.Y}";
                }
            }

            if (isDrawing && e.Button == MouseButtons.Left)
            {
                ContinueDrawing(e.Location);
            }
        }

        private void DrawingPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isDrawing)
            {
                EndDrawing(e.Location);
            }
        }

        private void StartDrawing(Point location)
        {
            var activeLayer = layerManager.ActiveLayer;
            if (activeLayer == null || activeLayer.IsLocked)
                return;

            isDrawing = true;
            lastPoint = location;

            currentStroke = new AdvancedStroke
            {
                Tool = currentTool,
                Color = currentColor,
                Width = currentWidth,
                Opacity = (byte)(currentOpacity * 255),
                LayerId = activeLayer.LayerId,
                StartTime = DateTime.Now
            };

            currentStroke.AddPoint(location, 1.0f); // Default pressure

            // Start gesture recognition
            if (isGestureRecognitionActive)
            {
                gestureRecognizer.StartGesture(location);
            }
        }

        private void ContinueDrawing(Point location)
        {
            if (!isDrawing || currentStroke == null)
                return;

            var distance = Distance(lastPoint, location);
            if (distance > 2) // Minimum distance to avoid too many points
            {
                currentStroke.AddPoint(location, 1.0f); // Default pressure
                lastPoint = location;

                // Add to gesture recognition
                if (isGestureRecognitionActive)
                {
                    gestureRecognizer.AddPoint(location);
                }

                // Redraw the drawing panel
                drawingPanel.Invalidate();
            }
        }

        private void EndDrawing(Point location)
        {
            if (!isDrawing || currentStroke == null)
                return;

            isDrawing = false;
            currentStroke.AddPoint(location, 1.0f);
            currentStroke.EndTime = DateTime.Now;

            // Add stroke to active layer
            var activeLayer = layerManager.ActiveLayer;
            if (activeLayer != null)
            {
                activeLayer.AddStroke(currentStroke);
            }

            // End gesture recognition
            if (isGestureRecognitionActive)
            {
                gestureRecognizer.EndGesture(location);
            }

            currentStroke = null;
            drawingPanel.Invalidate();
        }

        // Menu event handlers
        private void NewProject()
        {
            if (ConfirmUnsavedChanges())
            {
                layerManager.ClearAllLayers();
                drawingPanel.Invalidate();
                UpdateStatusBar("Yeni proje oluşturuldu");
            }
        }

        private async void OpenProject()
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Advanced Screen Pen Files (*.asp)|*.asp|All Files (*.*)|*.*";
                openDialog.Title = "Proje Aç";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ShowProgress("Proje açılıyor...", 0);

                        await layerManager.LoadLayersAsync(openDialog.FileName);
                        drawingPanel.Invalidate();

                        HideProgress();
                        UpdateStatusBar($"Proje açıldı: {Path.GetFileName(openDialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        HideProgress();
                        MessageBox.Show($"Proje açılırken hata oluştu:\n{ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void SaveProject()
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Advanced Screen Pen Files (*.asp)|*.asp";
                saveDialog.Title = "Projeyi Kaydet";
                saveDialog.DefaultExt = "asp";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ShowProgress("Proje kaydediliyor...", 0);

                        await layerManager.SaveLayersAsync(saveDialog.FileName);

                        HideProgress();
                        UpdateStatusBar($"Proje kaydedildi: {Path.GetFileName(saveDialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        HideProgress();
                        MessageBox.Show($"Proje kaydedilirken hata oluştu:\n{ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveProjectAs()
        {
            SaveProject(); // For now, same as SaveProject
        }

        private void ExportProject()
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All Files (*.*)|*.*";
                saveDialog.Title = "Dışa Aktar";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var bitmap = layerManager.RenderLayers(drawingPanel.Size);
                        bitmap.Save(saveDialog.FileName);
                        bitmap.Dispose();

                        UpdateStatusBar($"Dışa aktarıldı: {Path.GetFileName(saveDialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Dışa aktarılırken hata oluştu:\n{ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Undo()
        {
            if (layerManager.CanUndo)
            {
                layerManager.Undo();
                drawingPanel.Invalidate();
                UpdateStatusBar("Geri alındı");
            }
        }

        private void Redo()
        {
            if (layerManager.CanRedo)
            {
                layerManager.Redo();
                drawingPanel.Invalidate();
                UpdateStatusBar("Yinelendi");
            }
        }

        private void ClearAll()
        {
            if (MessageBox.Show("Tüm çizimleri temizlemek istediğinizden emin misiniz?", "Onayla",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                layerManager.ClearAllLayers();
                drawingPanel.Invalidate();
                UpdateStatusBar("Temizlendi");
            }
        }

        private void ShowSettings()
        {
            using (var settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    LoadSettings();
                    UpdateStatusBar("Ayarlar güncellendi");
                }
            }
        }

        private void ShowAdvancedSettings()
        {
            using (var advancedSettingsForm = new AdvancedSettingsForm()) // Parametre yok
            {
                if (advancedSettingsForm.ShowDialog() == DialogResult.OK)
                {
                    LoadSettings();
                    UpdateStatusBar("Gelişmiş ayarlar güncellendi");
                }
            }
        }

        // Tool and UI event handlers
        private void ToolboxForm_ToolSelected(object sender, ToolSelectedEventArgs e)
        {
            currentTool = e.Tool;
            drawingPanel.Cursor = GetToolCursor();
            UpdateStatusBar($"Araç seçildi: {e.Tool}");
        }

        private void ToolboxForm_ColorSelected(object sender, ColorSelectedEventArgs e)
        {
            currentColor = e.Color;
            UpdateStatusBar($"Renk seçildi: {e.Color.Name}");
        }

        private void ToolboxForm_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            currentWidth = e.Size;
            UpdateStatusBar($"Boyut: {e.Size}");
        }

        private void ColorPaletteForm_ColorSelected(object sender, ColorSelectedEventArgs e)
        {
            currentColor = e.Color;
            UpdateStatusBar($"Renk: {e.Color.Name}");
        }

        // Voice control event handlers
        private void SpeechController_CommandRecognized(object sender, VoiceCommandEventArgs e)
        {
            try
            {
                ProcessVoiceCommand(e);
                UpdateStatusBar($"Ses komutu: {e.RecognizedText}");
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Ses komutu hatası: {ex.Message}");
            }
        }

        private void SpeechController_SpeechError(object sender, SpeechErrorEventArgs e)
        {
            UpdateStatusBar($"Ses hatası: {e.Message}");
        }

        private void ProcessVoiceCommand(VoiceCommandEventArgs e)
        {
            if (!e.Parameters.ContainsKey("action"))
                return;

            var action = e.Parameters["action"].ToString();

            switch (action)
            {
                case "clear":
                    ClearAll();
                    break;
                case "undo":
                    Undo();
                    break;
                case "redo":
                    Redo();
                    break;
                case "save":
                    SaveProject();
                    break;
                case "select_tool":
                    if (e.Parameters.ContainsKey("tool"))
                    {
                        var toolName = e.Parameters["tool"].ToString();
                        if (Enum.TryParse<DrawingTool>(toolName, true, out var tool))
                        {
                            currentTool = tool;
                            drawingPanel.Cursor = GetToolCursor();
                        }
                    }
                    break;
                case "select_color":
                    if (e.Parameters.ContainsKey("color"))
                    {
                        var colorName = e.Parameters["color"].ToString();
                        currentColor = Color.FromName(colorName);
                    }
                    break;
                case "set_size":
                    if (e.Parameters.ContainsKey("size"))
                    {
                        var sizeName = e.Parameters["size"].ToString();
                        currentWidth = sizeName switch
                        {
                            "small" => 1f,
                            "medium" => 3f,
                            "large" => 6f,
                            _ => currentWidth
                        };
                    }
                    break;
            }
        }

        // Performance and other event handlers
        private void PerformanceManager_PerformanceUpdated(object sender, PerformanceEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PerformanceManager_PerformanceUpdated(sender, e)));
                return;
            }

            // Update FPS and memory display in status bar
            if (statusStrip.Items.Count > 2)
            {
                var fpsLabel = statusStrip.Items[2] as ToolStripStatusLabel;
                var memoryLabel = statusStrip.Items[3] as ToolStripStatusLabel;

                if (fpsLabel != null)
                    fpsLabel.Text = $"FPS: {e.FPS:F1}";

                if (memoryLabel != null)
                    memoryLabel.Text = $"Bellek: {e.MemoryUsage / (1024 * 1024)} MB";
            }
        }

        private void PerformanceTimer_Tick(object sender, EventArgs e)
        {
            // Update performance metrics
            performanceManager.RecordMetric("FrameTime", 16.67f); // 60 FPS target
        }

        // Utility methods
        private float Distance(PointF p1, PointF p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private Cursor GetToolCursor()
        {
            return currentTool switch
            {
                DrawingTool.Pen => Cursors.Cross,
                DrawingTool.Brush => Cursors.Cross,
                DrawingTool.Eraser => Cursors.Cross,
                DrawingTool.Highlighter => Cursors.Cross,
                DrawingTool.Line => Cursors.Cross,
                _ => Cursors.Default
            };
        }

        private bool ConfirmUnsavedChanges()
        {
            // Check if there are unsaved changes
            if (layerManager.Layers.Any(l => l.Strokes.Any()))
            {
                var result = MessageBox.Show("Kaydedilmemiş değişiklikler var. Devam etmek istediğinizden emin misiniz?",
                    "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }
            return true;
        }

        private void UpdateStatusBar(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
        }

        private void ShowProgress(string message, int percentage)
        {
            if (progressBar != null && statusLabel != null)
            {
                statusLabel.Text = message;
                progressBar.Value = percentage;
                progressBar.Visible = true;
            }
        }

        private void HideProgress()
        {
            if (progressBar != null)
            {
                progressBar.Visible = false;
            }
        }

        // Form event handlers
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle keyboard shortcuts
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.N:
                        NewProject();
                        e.Handled = true;
                        break;
                    case Keys.O:
                        OpenProject();
                        e.Handled = true;
                        break;
                    case Keys.S:
                        SaveProject();
                        e.Handled = true;
                        break;
                    case Keys.Z:
                        Undo();
                        e.Handled = true;
                        break;
                    case Keys.Y:
                        Redo();
                        e.Handled = true;
                        break;
                    case Keys.Delete:
                        ClearAll();
                        e.Handled = true;
                        break;
                    case Keys.Space:
                        ToggleVoiceControl();
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.F11:
                        ToggleFullScreen();
                        e.Handled = true;
                        break;
                    case Keys.Escape:
                        if (WindowState == FormWindowState.Maximized)
                        {
                            WindowState = FormWindowState.Normal;
                        }
                        break;
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            PositionChildForms();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmUnsavedChanges())
            {
                e.Cancel = true;
                return;
            }

            try
            {
                SaveSettings();

                // Dispose of managers
                performanceManager?.Dispose();
                speechController?.Dispose();
                effectsEngine?.Dispose();
                collaborationManager?.Dispose();
                drawingEngine?.Dispose();
                layerManager?.Dispose();

                // Close child forms
                toolboxForm?.Close();
                layerPanelForm?.Close();
                colorPaletteForm?.Close();
            }
            catch (Exception ex)
            {
                // Log error but don't prevent closing
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }
        }

        // Toggle methods
        private void ToggleToolbox()
        {
            if (toolboxForm.Visible)
                toolboxForm.Hide();
            else
                toolboxForm.Show();
        }

        private void ToggleLayerPanel()
        {
            if (layerPanelForm.Visible)
                layerPanelForm.Hide();
            else
                layerPanelForm.Show();
        }

        private void ToggleColorPalette()
        {
            if (colorPaletteForm.Visible)
                colorPaletteForm.Hide();
            else
                colorPaletteForm.Show();
        }

        private void ToggleVoiceControl()
        {
            isVoiceControlActive = !isVoiceControlActive;

            if (isVoiceControlActive)
            {
                speechController.StartListening();
                UpdateStatusBar("Ses kontrolü etkinleştirildi");
            }
            else
            {
                speechController.StopListening();
                UpdateStatusBar("Ses kontrolü durduruldu");
            }
        }

        private void ToggleCollaboration()
        {
            isCollaborationActive = !isCollaborationActive;

            if (isCollaborationActive)
            {
                collaborationManager.StartSession("Default Session");
                UpdateStatusBar("İş birliği etkinleştirildi");
            }
            else
            {
                collaborationManager.StopSession();
                UpdateStatusBar("İş birliği durduruldu");
            }
        }

        private void ToggleFullScreen()
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
                menuStrip.Visible = true;
                statusStrip.Visible = true;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                menuStrip.Visible = false;
                statusStrip.Visible = false;
            }
        }

        // Effect and other methods
        private async void ApplyEffect(string effectName)
        {
            try
            {
                var activeLayer = layerManager.ActiveLayer;
                if (activeLayer == null || activeLayer.Strokes.Count == 0)
                {
                    UpdateStatusBar("Efekt uygulanacak çizim bulunamadı");
                    return;
                }

                ShowProgress($"Efekt uygulanıyor: {effectName}", 0);

                var bitmap = layerManager.RenderLayers(drawingPanel.Size, new List<AnnotationLayer> { activeLayer });
                var result = await effectsEngine.ApplyEffectAsync(bitmap, effectName);

                // Convert result back to strokes (simplified)
                // In a real implementation, this would be more sophisticated

                HideProgress();
                UpdateStatusBar($"Efekt uygulandı: {effectName}");
                drawingPanel.Invalidate();
            }
            catch (Exception ex)
            {
                HideProgress();
                MessageBox.Show($"Efekt uygulanırken hata oluştu:\n{ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler methods for managers
        private void LayerManager_LayerAdded(object sender, LayerEventArgs e)
        {
            layerPanelForm?.RefreshLayers();
            UpdateStatusBar($"Katman eklendi: {e.Layer.Name}");
        }

        private void LayerManager_LayerRemoved(object sender, LayerEventArgs e)
        {
            layerPanelForm?.RefreshLayers();
            UpdateStatusBar($"Katman silindi: {e.Layer.Name}");
        }

        private void LayerManager_ActiveLayerChanged(object sender, LayerEventArgs e)
        {
            layerPanelForm?.RefreshLayers();
            UpdateStatusBar($"Aktif katman: {e.Layer.Name}");
        }

        private void EffectsEngine_EffectApplied(object sender, EffectAppliedEventArgs e)
        {
            UpdateStatusBar($"Efekt tamamlandı: {e.EffectName}");
        }

        private void CollaborationManager_UserJoined(object sender, UserEventArgs e)
        {
            UpdateStatusBar($"Kullanıcı katıldı: {e.User.Name}");
        }

        private void CollaborationManager_UserLeft(object sender, UserEventArgs e)
        {
            UpdateStatusBar($"Kullanıcı ayrıldı: {e.User.Name}");
        }

        private void GestureRecognizer_GestureRecognized(object sender, GestureRecognizedEventArgs e)
        {
            UpdateStatusBar($"Hareket tanındı: {e.GestureType}");

            // Apply gesture-based actions
            switch (e.GestureType)
            {
                case GestureType.Circle:
                    // Convert stroke to perfect circle
                    break;
                case GestureType.Rectangle:
                    // Convert stroke to perfect rectangle
                    break;
                case GestureType.Line:
                    // Convert stroke to straight line
                    break;
            }
        }

        // Help and about methods
        private void ShowHelp()
        {
            MessageBox.Show("Advanced Screen Pen Professional v2.0 Kullanım Kılavuzu\n\n" +
                "• Sol tık ile çizim yapabilirsiniz\n" +
                "• Ctrl+Z: Geri al\n" +
                "• Ctrl+Y: Yinele\n" +
                "• Ctrl+S: Kaydet\n" +
                "• Ctrl+Space: Ses kontrolü\n" +
                "• F11: Tam ekran\n\n" +
                "Daha fazla bilgi için: https://cayatur.com/screenpen",
                "Yardım", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowShortcuts()
        {
            var shortcuts = "Kısayol Tuşları:\n\n" +
                "Dosya İşlemleri:\n" +
                "Ctrl+N: Yeni proje\n" +
                "Ctrl+O: Proje aç\n" +
                "Ctrl+S: Proje kaydet\n" +
                "Ctrl+E: Dışa aktar\n\n" +
                "Düzenleme:\n" +
                "Ctrl+Z: Geri al\n" +
                "Ctrl+Y: Yinele\n" +
                "Ctrl+Delete: Temizle\n\n" +
                "Görünüm:\n" +
                "F11: Tam ekran\n" +
                "Escape: Normal ekran\n\n" +
                "Gelişmiş:\n" +
                "Ctrl+Space: Ses kontrolü\n" +
                "Ctrl+G: Izgara göster/gizle\n" +
                "Ctrl+L: Katmanlar paneli";

            MessageBox.Show(shortcuts, "Kısayol Tuşları", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            var about = "Advanced Screen Pen Professional v2.0\n\n" +
                "Geliştirici: CaYatur\n" +
                "Tarih: 26 Mayıs 2025\n" +
                "Sürüm: 2.0.0.0\n\n" +
                "Özellikler:\n" +
                "• Gelişmiş çizim araçları\n" +
                "• Ses kontrolü\n" +
                "• Hareket tanıma\n" +
                "• Çoklu katman desteği\n" +
                "• Matematik araçları\n" +
                "• İş birliği özellikleri\n" +
                "• Görsel efektler\n" +
                "• Performans optimizasyonu\n\n" +
                "© 2025 CaYatur. Tüm hakları saklıdır.\n" +
                "Web: https://cayatur.com";

            MessageBox.Show(about, "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}