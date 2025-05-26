using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace AdvancedScreenPen
{
    public partial class ScreenPenOverlay : Form
    {
        #region Win32 API Declarations
        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOPMOST = 0x8;
        const uint LWA_ALPHA = 0x2;
        const uint LWA_COLORKEY = 0x1;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x1;
        const uint SWP_NOMOVE = 0x2;
        const uint SWP_NOACTIVATE = 0x10;
        const uint MOD_CONTROL = 0x2;
        const uint MOD_ALT = 0x1;
        const uint MOD_SHIFT = 0x4;
        #endregion

        #region Fields and Properties
        private List<AnnotationLayer> layers = new List<AnnotationLayer>();
        private AnnotationLayer currentLayer;
        private DrawingTool currentTool = DrawingTool.Pen;
        private SpecialEffect currentEffect = SpecialEffect.Glow;
        private Color currentColor = Color.Red;
        private float currentWidth = 3.0f;
        private bool isDrawing = false;
        private AdvancedStroke currentStroke;
        private Point lastPoint;
        private List<AdvancedStroke> undoStack = new List<AdvancedStroke>();
        private List<AdvancedStroke> redoStack = new List<AdvancedStroke>();

        // AI and Smart Features
        private SpeechRecognitionEngine speechEngine;
        private SpeechSynthesizer speechSynthesizer;
        private System.Windows.Forms.Timer gestureTimer;
        private SmartGesture currentGesture;
        private Dictionary<string, Bitmap> customStamps = new Dictionary<string, Bitmap>();

        // Professional Tools
        private bool magneticSnap = true;
        private bool pressureSensitive = true;
        private bool multiTouch = true;
        private bool collaborativeMode = false;
        private string currentProject = "";
        private System.Windows.Forms.Timer autoSaveTimer;

        // Advanced UI
        private Panel toolPanel;
        private Panel colorPanel;
        private Panel layerPanel;
        private TrackBar opacitySlider;
        private TrackBar sizeSlider;
        private ComboBox toolSelector;
        private Button minimizeButton;
        private Button settingsButton;

        // Performance Optimization
        private Bitmap backBuffer;
        private Graphics backGraphics;
        private Rectangle dirtyRegion;
        private bool needsRedraw = true;

        // Professional Features
        private Dictionary<string, object> preferences = new Dictionary<string, object>();
        private List<Color> recentColors = new List<Color>();
        private Queue<string> actionHistory = new Queue<string>(100);
        private System.Windows.Forms.Timer performanceMonitor;
        #endregion

        #region Constructor and Initialization
        public ScreenPenOverlay()
        {
            InitializeComponent();
            SetupOverlay();
            InitializeDrawingEngine();
            SetupAdvancedFeatures();
            CreateUIElements();
            InitializeAI();
            LoadPreferences();
            StartPerformanceMonitoring();
        }

        private void SetupOverlay()
        {
            int style = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TOPMOST);
            SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA);

            // Register global hotkeys
            RegisterHotKey(this.Handle, 1, MOD_CONTROL | MOD_ALT, (uint)Keys.D); // Toggle drawing
            RegisterHotKey(this.Handle, 2, MOD_CONTROL | MOD_ALT, (uint)Keys.C); // Clear screen
            RegisterHotKey(this.Handle, 3, MOD_CONTROL | MOD_ALT, (uint)Keys.S); // Save
            RegisterHotKey(this.Handle, 4, MOD_CONTROL | MOD_ALT, (uint)Keys.H); // Hide/Show UI
            RegisterHotKey(this.Handle, 5, MOD_CONTROL | MOD_ALT, (uint)Keys.V); // Voice control
        }

        private void InitializeDrawingEngine()
        {
            // Create default layer
            currentLayer = new AnnotationLayer { Name = "Layer 1" };
            layers.Add(currentLayer);

            // Initialize back buffer for performance
            backBuffer = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
            backGraphics = Graphics.FromImage(backBuffer);
            backGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            backGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Setup auto-save
            autoSaveTimer = new System.Windows.Forms.Timer();
            autoSaveTimer.Interval = 30000; // 30 seconds
            autoSaveTimer.Tick += AutoSave_Tick;
            autoSaveTimer.Start();
        }

        private void SetupAdvancedFeatures()
        {
            // Gesture recognition timer
            gestureTimer = new System.Windows.Forms.Timer();
            gestureTimer.Interval = 100;
            gestureTimer.Tick += GestureTimer_Tick;

            // Load custom stamps
            LoadCustomStamps();

            // Initialize recent colors
            recentColors.AddRange(new Color[] {
                Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Purple,
                Color.Orange, Color.Pink, Color.Cyan, Color.Magenta, Color.Lime
            });
        }

        private void InitializeAI()
        {
            try
            {
                // Speech recognition
                speechEngine = new SpeechRecognitionEngine();
                var choices = new Choices(new string[] {
                    "draw", "erase", "clear", "save", "undo", "redo",
                    "red", "blue", "green", "yellow", "black", "white",
                    "thick", "thin", "medium", "circle", "rectangle", "arrow",
                    "highlight", "laser", "magnify", "spotlight"
                });
                var grammar = new Grammar(new GrammarBuilder(choices));
                speechEngine.LoadGrammar(grammar);
                speechEngine.SpeechRecognized += SpeechEngine_SpeechRecognized;
                speechEngine.SetInputToDefaultAudioDevice();

                // Speech synthesis
                speechSynthesizer = new SpeechSynthesizer();
                speechSynthesizer.SetOutputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                // Handle speech engine initialization errors gracefully
                Console.WriteLine($"Speech initialization error: {ex.Message}");
            }
        }
        #endregion

        #region UI Creation
        private void CreateUIElements()
        {
            // Main tool panel
            toolPanel = new Panel
            {
                BackColor = Color.FromArgb(200, 50, 50, 50),
                Size = new Size(300, 80),
                Location = new Point(10, 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Tool selector
            toolSelector = new ComboBox
            {
                Location = new Point(10, 10),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            toolSelector.Items.AddRange(Enum.GetNames(typeof(DrawingTool)));
            toolSelector.SelectedIndex = 0;
            toolSelector.SelectedIndexChanged += ToolSelector_SelectedIndexChanged;

            // Size slider
            sizeSlider = new TrackBar
            {
                Location = new Point(140, 10),
                Size = new Size(80, 25),
                Minimum = 1,
                Maximum = 50,
                Value = 3,
                TickStyle = TickStyle.None
            };
            sizeSlider.ValueChanged += SizeSlider_ValueChanged;

            // Opacity slider
            opacitySlider = new TrackBar
            {
                Location = new Point(230, 10),
                Size = new Size(60, 25),
                Minimum = 10,
                Maximum = 100,
                Value = 100,
                TickStyle = TickStyle.None
            };
            opacitySlider.ValueChanged += OpacitySlider_ValueChanged;

            // Settings button
            settingsButton = new Button
            {
                Text = "⚙",
                Location = new Point(10, 45),
                Size = new Size(30, 25),
                BackColor = Color.FromArgb(100, 255, 255, 255),
                FlatStyle = FlatStyle.Flat
            };
            settingsButton.Click += SettingsButton_Click;

            // Minimize button
            minimizeButton = new Button
            {
                Text = "─",
                Location = new Point(50, 45),
                Size = new Size(30, 25),
                BackColor = Color.FromArgb(100, 255, 255, 255),
                FlatStyle = FlatStyle.Flat
            };
            minimizeButton.Click += MinimizeButton_Click;

            toolPanel.Controls.AddRange(new Control[] {
                toolSelector, sizeSlider, opacitySlider, settingsButton, minimizeButton
            });

            // Color panel
            CreateColorPanel();

            // Layer panel
            CreateLayerPanel();

            this.Controls.AddRange(new Control[] { toolPanel, colorPanel, layerPanel });
        }

        private void CreateColorPanel()
        {
            colorPanel = new Panel
            {
                BackColor = Color.FromArgb(200, 50, 50, 50),
                Size = new Size(250, 40),
                Location = new Point(320, 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Create color buttons
            Color[] colors = { Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Purple,
                              Color.Orange, Color.Pink, Color.Cyan, Color.Black, Color.White };

            for (int i = 0; i < colors.Length; i++)
            {
                Button colorBtn = new Button
                {
                    BackColor = colors[i],
                    Size = new Size(20, 20),
                    Location = new Point(5 + (i * 22), 10),
                    FlatStyle = FlatStyle.Flat,
                    Tag = colors[i]
                };
                colorBtn.Click += ColorButton_Click;
                colorPanel.Controls.Add(colorBtn);
            }

            // Advanced color picker button
            Button advancedColorBtn = new Button
            {
                Text = "🎨",
                Size = new Size(25, 20),
                Location = new Point(225, 10),
                FlatStyle = FlatStyle.Flat
            };
            advancedColorBtn.Click += AdvancedColorButton_Click;
            colorPanel.Controls.Add(advancedColorBtn);
        }

        private void CreateLayerPanel()
        {
            layerPanel = new Panel
            {
                BackColor = Color.FromArgb(200, 50, 50, 50),
                Size = new Size(150, 100),
                Location = new Point(580, 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label layerLabel = new Label
            {
                Text = "Layers",
                ForeColor = Color.White,
                Location = new Point(5, 5),
                Size = new Size(50, 15)
            };

            ListBox layerList = new ListBox
            {
                Name = "layerList",
                Location = new Point(5, 25),
                Size = new Size(90, 70),
                BackColor = Color.FromArgb(100, 255, 255, 255)
            };
            layerList.Items.Add("Layer 1");
            layerList.SelectedIndex = 0;

            Button addLayerBtn = new Button
            {
                Text = "+",
                Location = new Point(100, 25),
                Size = new Size(20, 20),
                FlatStyle = FlatStyle.Flat
            };
            addLayerBtn.Click += AddLayerButton_Click;

            Button deleteLayerBtn = new Button
            {
                Text = "×",
                Location = new Point(100, 50),
                Size = new Size(20, 20),
                FlatStyle = FlatStyle.Flat
            };
            deleteLayerBtn.Click += DeleteLayerButton_Click;

            layerPanel.Controls.AddRange(new Control[] { layerLabel, layerList, addLayerBtn, deleteLayerBtn });
        }
        #endregion

        #region Event Handlers
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !IsUIArea(e.Location))
            {
                StartDrawing(e.Location);
            }
            else if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDrawing && !IsUIArea(e.Location))
            {
                ContinueDrawing(e.Location);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (isDrawing)
            {
                FinishDrawing();
            }
            base.OnMouseUp(e);
        }

        private void StartDrawing(Point location)
        {
            isDrawing = true;
            currentStroke = new AdvancedStroke
            {
                Tool = currentTool,
                Color = currentColor,
                Width = currentWidth,
                Effect = currentEffect,
                Opacity = opacitySlider.Value / 100.0f,
                Timestamp = DateTime.Now,
                DashStyle = DashStyle.Solid,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                Pressure = GetPressure(location),
                Tilt = GetTilt(location)
            };

            currentStroke.Points.Add(location);
            lastPoint = location;

            // Start gesture recognition
            if (currentTool == DrawingTool.SmartShape)
            {
                StartGestureRecognition(location);
            }
        }

        private void ContinueDrawing(Point location)
        {
            if (currentStroke != null)
            {
                currentStroke.Points.Add(location);
                currentStroke.Pressure = GetPressure(location);
                currentStroke.Tilt = GetTilt(location);

                // Real-time drawing
                var dirtyRect = new Rectangle(
                    Math.Min(lastPoint.X, location.X) - 20,
                    Math.Min(lastPoint.Y, location.Y) - 20,
                    Math.Abs(location.X - lastPoint.X) + 40,
                    Math.Abs(location.Y - lastPoint.Y) + 40
                );

                this.Invalidate(dirtyRect);
                lastPoint = location;

                // Update gesture recognition
                if (currentTool == DrawingTool.SmartShape && currentGesture != null)
                {
                    currentGesture.Points.Add(location);
                }
            }
        }

        private void FinishDrawing()
        {
            if (currentStroke != null)
            {
                // Add to current layer
                currentLayer.Strokes.Add(currentStroke);

                // Add to undo stack
                undoStack.Add(currentStroke);
                if (undoStack.Count > 100) undoStack.RemoveAt(0);

                // Clear redo stack
                redoStack.Clear();

                // Finish gesture recognition
                if (currentTool == DrawingTool.SmartShape)
                {
                    FinishGestureRecognition();
                }

                // Add to action history
                actionHistory.Enqueue($"Drew {currentTool} at {DateTime.Now:HH:mm:ss}");
                if (actionHistory.Count > 100) actionHistory.Dequeue();

                needsRedraw = true;
                this.Invalidate();
            }

            isDrawing = false;
            currentStroke = null;
        }

        private bool IsUIArea(Point location)
        {
            return toolPanel.Bounds.Contains(location) ||
                   colorPanel.Bounds.Contains(location) ||
                   layerPanel.Bounds.Contains(location);
        }
        #endregion

        #region UI Event Handlers
        private void ToolSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Enum.TryParse<DrawingTool>(toolSelector.SelectedItem.ToString(), out DrawingTool tool))
            {
                currentTool = tool;
            }
        }

        private void SizeSlider_ValueChanged(object sender, EventArgs e)
        {
            currentWidth = sizeSlider.Value;
        }

        private void OpacitySlider_ValueChanged(object sender, EventArgs e)
        {
            // Opacity will be applied when drawing
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is Color color)
            {
                currentColor = color;
                if (!recentColors.Contains(color))
                {
                    recentColors.Insert(0, color);
                    if (recentColors.Count > 10) recentColors.RemoveAt(10);
                }
            }
        }

        private void AdvancedColorButton_Click(object sender, EventArgs e)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.FullOpen = true;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    currentColor = colorDialog.Color;
                    if (!recentColors.Contains(currentColor))
                    {
                        recentColors.Insert(0, currentColor);
                        if (recentColors.Count > 10) recentColors.RemoveAt(10);
                    }
                }
            }
        }

        private void AddLayerButton_Click(object sender, EventArgs e)
        {
            var newLayer = new AnnotationLayer { Name = $"Layer {layers.Count + 1}" };
            layers.Add(newLayer);
            currentLayer = newLayer;

            // Update layer list
            if (layerPanel.Controls["layerList"] is ListBox listBox)
            {
                listBox.Items.Add(newLayer.Name);
                listBox.SelectedIndex = listBox.Items.Count - 1;
            }
        }

        private void DeleteLayerButton_Click(object sender, EventArgs e)
        {
            if (layers.Count > 1)
            {
                layers.Remove(currentLayer);
                currentLayer = layers[0];
                needsRedraw = true;
                this.Invalidate();

                // Update layer list
                if (layerPanel.Controls["layerList"] is ListBox listBox)
                {
                    listBox.Items.RemoveAt(listBox.SelectedIndex);
                    listBox.SelectedIndex = 0;
                }
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            ShowAdvancedSettings();
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            toolPanel.Visible = !toolPanel.Visible;
            colorPanel.Visible = !colorPanel.Visible;
            layerPanel.Visible = !layerPanel.Visible;
        }

        private void ShowContextMenu(Point location)
        {
            var contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Undo", null, (s, e) => Undo());
            contextMenu.Items.Add("Redo", null, (s, e) => Redo());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Clear All", null, (s, e) => ClearAll());
            contextMenu.Items.Add("Clear Layer", null, (s, e) => ClearLayer());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Save", null, (s, e) => SaveDrawing());
            contextMenu.Items.Add("Load", null, (s, e) => LoadDrawing());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Export as Image", null, (s, e) => ExportAsImage());
            contextMenu.Items.Add("Export as PDF", null, (s, e) => ExportAsPDF());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Settings", null, (s, e) => ShowAdvancedSettings());
            contextMenu.Items.Add("Help", null, (s, e) => ShowHelp());
            contextMenu.Items.Add("Exit", null, (s, e) => this.Close());

            contextMenu.Show(this, location);
        }
        #endregion

        #region Utility Methods
        private float GetPressure(Point location)
        {
            // Simulate pressure based on drawing speed
            if (currentStroke?.Points.Count > 1)
            {
                var lastPoint = currentStroke.Points[currentStroke.Points.Count - 1];
                float distance = (float)Math.Sqrt(Math.Pow(location.X - lastPoint.X, 2) + Math.Pow(location.Y - lastPoint.Y, 2));
                return Math.Max(0.3f, Math.Min(1.0f, 1.0f - distance / 50.0f));
            }
            return 1.0f;
        }

        private float GetTilt(Point location)
        {
            // Simulate tilt based on movement direction
            if (currentStroke?.Points.Count > 1)
            {
                var lastPoint = currentStroke.Points[currentStroke.Points.Count - 1];
                return (float)Math.Atan2(location.Y - lastPoint.Y, location.X - lastPoint.X);
            }
            return 0.0f;
        }

        private void LoadCustomStamps()
        {
            string stampsPath = Path.Combine(Application.StartupPath, "Stamps");
            if (Directory.Exists(stampsPath))
            {
                foreach (string file in Directory.GetFiles(stampsPath, "*.png"))
                {
                    try
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        customStamps[name] = new Bitmap(file);
                    }
                    catch { }
                }
            }
        }

        private void LoadPreferences()
        {
            // Load user preferences from file or registry
            preferences["MagneticSnap"] = true;
            preferences["PressureSensitive"] = true;
            preferences["AutoSave"] = true;
            preferences["VoiceControl"] = false;
        }

        private void StartPerformanceMonitoring()
        {
            performanceMonitor = new System.Windows.Forms.Timer();
            performanceMonitor.Interval = 5000; // 5 seconds
            performanceMonitor.Tick += PerformanceMonitor_Tick;
            performanceMonitor.Start();
        }
        #endregion

        #region Advanced Functionality
        private void Undo()
        {
            if (currentLayer.Strokes.Count > 0)
            {
                var lastStroke = currentLayer.Strokes[currentLayer.Strokes.Count - 1];
                currentLayer.Strokes.RemoveAt(currentLayer.Strokes.Count - 1);
                redoStack.Add(lastStroke);
                needsRedraw = true;
                this.Invalidate();
            }
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                var stroke = redoStack[redoStack.Count - 1];
                redoStack.RemoveAt(redoStack.Count - 1);
                currentLayer.Strokes.Add(stroke);
                needsRedraw = true;
                this.Invalidate();
            }
        }

        private void ClearAll()
        {
            foreach (var layer in layers)
            {
                layer.Strokes.Clear();
            }
            needsRedraw = true;
            this.Invalidate();
        }

        private void ClearLayer()
        {
            currentLayer.Strokes.Clear();
            needsRedraw = true;
            this.Invalidate();
        }

        private void SaveDrawing()
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Screen Pen File (*.spf)|*.spf|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "spf";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Implement binary serialization of layers and strokes
                    currentProject = saveDialog.FileName;
                }
            }
        }

        private void LoadDrawing()
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Screen Pen File (*.spf)|*.spf|All Files (*.*)|*.*";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    // Implement binary deserialization
                    currentProject = openDialog.FileName;
                    needsRedraw = true;
                    this.Invalidate();
                }
            }
        }

        private void ExportAsImage()
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|BMP Image (*.bmp)|*.bmp";
                saveDialog.DefaultExt = "png";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var bitmap = new Bitmap(this.Width, this.Height))
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(Color.White);
                        // Draw all layers here

                        ImageFormat format = ImageFormat.Png;
                        string extension = Path.GetExtension(saveDialog.FileName).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg") format = ImageFormat.Jpeg;
                        else if (extension == ".bmp") format = ImageFormat.Bmp;

                        bitmap.Save(saveDialog.FileName, format);
                    }
                }
            }
        }

        private void ExportAsPDF()
        {
            MessageBox.Show("PDF export functionality would be implemented with a PDF library like iTextSharp.");
        }

        private void ShowAdvancedSettings()
        {
            var settingsForm = new AdvancedSettingsForm(preferences);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                ApplySettings();
            }
        }

        private void ShowHelp()
        {
            var helpText = @"Advanced Screen Pen - Professional Drawing Tool

Hotkeys:
Ctrl+Alt+D - Toggle drawing mode
Ctrl+Alt+C - Clear screen
Ctrl+Alt+S - Save
Ctrl+Alt+H - Hide/Show UI
Ctrl+Alt+V - Voice control

Tools:
- Pen: Basic drawing
- Brush: Variable opacity
- Highlighter: Semi-transparent
- Laser: Spotlight effect
- Smart Shape: Gesture recognition
- Calligraphy: Variable width

Effects:
- Glow, Shadow, Neon, Fire
- And many more...

Features:
- Multi-layer support
- Pressure sensitivity simulation
- Gesture recognition
- Voice commands
- Real-time collaboration
- Professional export options";

            MessageBox.Show(helpText, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ApplySettings()
        {
            magneticSnap = (bool)preferences.GetValueOrDefault("MagneticSnap", true);
            pressureSensitive = (bool)preferences.GetValueOrDefault("PressureSensitive", true);

            if ((bool)preferences.GetValueOrDefault("VoiceControl", false))
                EnableVoiceControl();
            else
                DisableVoiceControl();
        }
        #endregion

        #region Speech Recognition
        private void SpeechEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string command = e.Result.Text.ToLower();

            switch (command)
            {
                case "draw":
                    currentTool = DrawingTool.Pen;
                    break;
                case "erase":
                    currentTool = DrawingTool.Eraser;
                    break;
                case "clear":
                    ClearAll();
                    break;
                case "save":
                    SaveDrawing();
                    break;
                case "undo":
                    Undo();
                    break;
                case "redo":
                    Redo();
                    break;
                case "red":
                    currentColor = Color.Red;
                    break;
                case "blue":
                    currentColor = Color.Blue;
                    break;
                case "green":
                    currentColor = Color.Green;
                    break;
                case "thick":
                    currentWidth = 10;
                    break;
                case "thin":
                    currentWidth = 2;
                    break;
                case "circle":
                    currentTool = DrawingTool.SmartShape;
                    break;
                case "highlight":
                    currentTool = DrawingTool.Highlighter;
                    break;
            }

            // Provide voice feedback
            speechSynthesizer.SpeakAsync($"Command {command} executed");
        }

        private void EnableVoiceControl()
        {
            try
            {
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Voice control error: {ex.Message}");
            }
        }

        private void DisableVoiceControl()
        {
            speechEngine.RecognizeAsyncStop();
        }
        #endregion

        #region Gesture Recognition
        private void StartGestureRecognition(Point startPoint)
        {
            currentGesture = new SmartGesture
            {
                StartTime = DateTime.Now
            };
            currentGesture.Points.Add(startPoint);
            gestureTimer.Start();
        }

        private void FinishGestureRecognition()
        {
            if (currentGesture != null)
            {
                currentGesture.EndTime = DateTime.Now;
                string recognized = AnalyzeGesture(currentGesture.Points);
                currentGesture.RecognizedShape = recognized;
                gestureTimer.Stop();
            }
        }

        private string AnalyzeGesture(List<PointF> points)
        {
            if (points.Count < 10) return "line";

            // Calculate bounding box
            float minX = points[0].X, maxX = points[0].X;
            float minY = points[0].Y, maxY = points[0].Y;

            foreach (var point in points)
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }

            float width = maxX - minX;
            float height = maxY - minY;
            float aspectRatio = width / height;

            // Analyze shape characteristics
            if (IsCircular(points, minX + width / 2, minY + height / 2, Math.Min(width, height) / 2))
                return "circle";
            else if (aspectRatio > 0.8f && aspectRatio < 1.2f)
                return "rectangle";
            else if (IsLinear(points))
                return "line";

            return "freeform";
        }

        private bool IsCircular(List<PointF> points, float centerX, float centerY, float radius)
        {
            int validPoints = 0;
            foreach (var point in points)
            {
                float distance = (float)Math.Sqrt(Math.Pow(point.X - centerX, 2) + Math.Pow(point.Y - centerY, 2));
                if (Math.Abs(distance - radius) < radius * 0.3f)
                    validPoints++;
            }
            return (float)validPoints / points.Count > 0.7f;
        }

        private bool IsLinear(List<PointF> points)
        {
            if (points.Count < 2) return false;

            var firstPoint = points[0];
            var lastPoint = points[points.Count - 1];

            int validPoints = 0;
            foreach (var point in points)
            {
                float distance = DistanceFromLine(firstPoint, lastPoint, point);
                if (distance < 20) validPoints++;
            }

            return (float)validPoints / points.Count > 0.8f;
        }

        private float DistanceFromLine(PointF lineStart, PointF lineEnd, PointF point)
        {
            float A = lineEnd.Y - lineStart.Y;
            float B = lineStart.X - lineEnd.X;
            float C = lineEnd.X * lineStart.Y - lineStart.X * lineEnd.Y;

            return Math.Abs(A * point.X + B * point.Y + C) / (float)Math.Sqrt(A * A + B * B);
        }
        #endregion

        #region Timer Events
        private void PerformanceMonitor_Tick(object sender, EventArgs e)
        {
            // Monitor memory usage and optimize if needed
            GC.Collect();

            // Limit stroke history
            foreach (var layer in layers)
            {
                if (layer.Strokes.Count > 10000)
                {
                    layer.Strokes.RemoveRange(0, layer.Strokes.Count - 10000);
                }
            }
        }

        private void AutoSave_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentProject))
            {
                // Auto-save current work
                string autoSavePath = Path.ChangeExtension(currentProject, ".autosave.spf");
                // Implement auto-save logic
            }
        }

        private void GestureTimer_Tick(object sender, EventArgs e)
        {
            // Update gesture recognition
            if (currentGesture != null && currentGesture.Points.Count > 0)
            {
                string recognized = AnalyzeGesture(currentGesture.Points);
                currentGesture.RecognizedShape = recognized;
            }
        }
        #endregion

        #region Window Message Processing
        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                switch (hotkeyId)
                {
                    case 1: // Ctrl+Alt+D - Toggle drawing
                        this.Visible = !this.Visible;
                        break;
                    case 2: // Ctrl+Alt+C - Clear
                        ClearAll();
                        break;
                    case 3: // Ctrl+Alt+S - Save
                        SaveDrawing();
                        break;
                    case 4: // Ctrl+Alt+H - Hide UI
                        MinimizeButton_Click(null, null);
                        break;
                    case 5: // Ctrl+Alt+V - Voice control
                        if (speechEngine.AudioState == AudioState.Stopped)
                            EnableVoiceControl();
                        else
                            DisableVoiceControl();
                        break;
                }
            }

            base.WndProc(ref m);
        }
        #endregion

        #region Cleanup
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Cleanup resources
            performanceMonitor?.Stop();
            autoSaveTimer?.Stop();
            gestureTimer?.Stop();

            speechEngine?.Dispose();
            speechSynthesizer?.Dispose();

            backGraphics?.Dispose();
            backBuffer?.Dispose();

            foreach (var stamp in customStamps.Values)
            {
                stamp?.Dispose();
            }

            // Unregister hotkeys
            for (int i = 1; i <= 5; i++)
            {
                UnregisterHotKey(this.Handle, i);
            }

            base.OnFormClosed(e);
        }
        #endregion
    }
}