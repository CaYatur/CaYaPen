using System;
using System.Drawing;
using System.Windows.Forms;

namespace AdvancedScreenPen
{
    public partial class AdvancedSettingsForm : Form
    {
        private TabControl settingsTabControl;
        private Button okButton;
        private Button cancelButton;
        private Button applyButton;
        private Button resetButton;

        // Performans kontrolleri
        private CheckBox enableHardwareAcceleration;
        private CheckBox enableMultithreading;
        private CheckBox enableGpuAcceleration;
        private TrackBar maxFrameRateTrackBar;
        private Label maxFrameRateLabel;
        private TrackBar cacheSizeTrackBar;
        private Label cacheSizeLabel;
        private ComboBox performanceModeComboBox;
        private CheckBox enableVSyncCheckBox;
        private CheckBox enableLODCheckBox;

        // Ses kontrolleri
        private CheckBox enableVoiceControlCheckBox;
        private ComboBox voiceLanguageComboBox;
        private TrackBar confidenceThresholdTrackBar;
        private Label confidenceThresholdLabel;
        private CheckBox enableFeedbackCheckBox;
        private CheckBox continuousListeningCheckBox;
        private TrackBar speechRateTrackBar;
        private Label speechRateLabel;
        private TrackBar volumeTrackBar;
        private Label volumeLabel;
        private ComboBox preferredVoiceComboBox;

        // Hareket tanıma kontrolleri
        private CheckBox enableGestureRecognitionCheckBox;
        private TrackBar gestureConfidenceTrackBar;
        private Label gestureConfidenceLabel;
        private CheckBox enableLearningCheckBox;
        private CheckBox enableRotationInvarianceCheckBox;
        private CheckBox enableScaleInvarianceCheckBox;
        private TrackBar maxTemplatePointsTrackBar;
        private Label maxTemplatePointsLabel;

        // İş birliği kontrolleri
        private CheckBox enableCollaborationCheckBox;
        private NumericUpDown serverPortNumeric;
        private NumericUpDown maxConnectionsNumeric;
        private CheckBox enableVoiceChatCheckBox;
        private CheckBox enableScreenSharingCheckBox;
        private CheckBox autoReconnectCheckBox;
        private TrackBar connectionTimeoutTrackBar;
        private Label connectionTimeoutLabel;

        // Efekt kontrolleri
        private CheckBox enableEffectsCheckBox;
        private CheckBox enableRealTimePreviewCheckBox;
        private CheckBox enableAnimatedEffectsCheckBox;
        private CheckBox enableParticleEffectsCheckBox;
        private ComboBox defaultQualityComboBox;
        private CheckBox enableShaderEffectsCheckBox;

        // Matematik araçları kontrolleri
        private CheckBox enableMathToolsCheckBox;
        private TrackBar defaultScaleTrackBar;
        private Label defaultScaleLabel;
        private CheckBox enableGridSnappingCheckBox;
        private CheckBox showCoordinatesCheckBox;
        private TrackBar gridSizeTrackBar;
        private Label gridSizeLabel;

        // Güvenlik kontrolleri
        private CheckBox enableEncryptionCheckBox;
        private CheckBox requireAuthenticationCheckBox;
        private ComboBox encryptionAlgorithmComboBox;
        private TrackBar sessionTimeoutTrackBar;
        private Label sessionTimeoutLabel;
        private CheckBox enableAuditLogCheckBox;
        private CheckBox allowRemoteAccessCheckBox;

        // Debug kontrolleri
        private CheckBox enableDebugModeCheckBox;
        private CheckBox enableAdvancedLoggingCheckBox;
        private ComboBox logLevelComboBox;
        private CheckBox enablePerformanceCountersCheckBox;
        private CheckBox enableMemoryProfilingCheckBox;
        private CheckBox enableCrashReportingCheckBox;

        public AdvancedSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        

        private void CreateControls()
        {
            // Tab Control
            settingsTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            // Sekmeleri oluştur
            CreatePerformanceTab();
            CreateVoiceControlTab();
            CreateGestureRecognitionTab();
            CreateCollaborationTab();
            CreateEffectsTab();
            CreateMathToolsTab();
            CreateSecurityTab();
            CreateDebugTab();

            Controls.Add(settingsTabControl);

            // Butonlar paneli
            var buttonsPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            // Tamam butonu
            okButton = new Button
            {
                Text = "Tamam",
                Size = new Size(100, 35),
                Location = new Point(Width - 450, 12),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += OkButton_Click;

            // Uygula butonu
            applyButton = new Button
            {
                Text = "Uygula",
                Size = new Size(100, 35),
                Location = new Point(Width - 340, 12),
                BackColor = Color.FromArgb(0, 100, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Click += ApplyButton_Click;

            // Sıfırla butonu
            resetButton = new Button
            {
                Text = "Sıfırla",
                Size = new Size(100, 35),
                Location = new Point(Width - 230, 12),
                BackColor = Color.FromArgb(180, 100, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            resetButton.FlatAppearance.BorderSize = 0;
            resetButton.Click += ResetButton_Click;

            // İptal butonu
            cancelButton = new Button
            {
                Text = "İptal",
                Size = new Size(100, 35),
                Location = new Point(Width - 120, 12),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += CancelButton_Click;

            buttonsPanel.Controls.Add(okButton);
            buttonsPanel.Controls.Add(applyButton);
            buttonsPanel.Controls.Add(resetButton);
            buttonsPanel.Controls.Add(cancelButton);
            Controls.Add(buttonsPanel);
        }

        private void CreatePerformanceTab()
        {
            var performanceTab = new TabPage("Performans")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var y = 20;

            // Donanım İvmelendirme
            var hardwareGroup = new GroupBox
            {
                Text = "Donanım İvmelendirme",
                Size = new Size(400, 120),
                Location = new Point(20, y),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableHardwareAcceleration = new CheckBox
            {
                Text = "Donanım ivmelendirmeyi etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(300, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableGpuAcceleration = new CheckBox
            {
                Text = "GPU ivmelendirmeyi etkinleştir",
                Location = new Point(15, 50),
                Size = new Size(300, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableMultithreading = new CheckBox
            {
                Text = "Çoklu işlemciyi etkinleştir",
                Location = new Point(15, 75),
                Size = new Size(300, 20),
                ForeColor = Color.White,
                Checked = true
            };

            hardwareGroup.Controls.Add(enableHardwareAcceleration);
            hardwareGroup.Controls.Add(enableGpuAcceleration);
            hardwareGroup.Controls.Add(enableMultithreading);
            performanceTab.Controls.Add(hardwareGroup);

            y += 140;

            // FPS ve Kalite
            var fpsGroup = new GroupBox
            {
                Text = "FPS ve Kalite Ayarları",
                Size = new Size(400, 150),
                Location = new Point(20, y),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var maxFpsLabel = new Label
            {
                Text = "Maksimum FPS:",
                Location = new Point(15, 25),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            maxFrameRateTrackBar = new TrackBar
            {
                Minimum = 30,
                Maximum = 144,
                Value = 60,
                TickFrequency = 15,
                Location = new Point(15, 45),
                Size = new Size(250, 45)
            };
            maxFrameRateTrackBar.ValueChanged += (s, e) =>
            {
                maxFrameRateLabel.Text = $"FPS: {maxFrameRateTrackBar.Value}";
            };

            maxFrameRateLabel = new Label
            {
                Text = "FPS: 60",
                Location = new Point(280, 55),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            enableVSyncCheckBox = new CheckBox
            {
                Text = "V-Sync etkinleştir",
                Location = new Point(15, 95),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableLODCheckBox = new CheckBox
            {
                Text = "LOD (Level of Detail) etkinleştir",
                Location = new Point(15, 120),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = true
            };

            fpsGroup.Controls.Add(maxFpsLabel);
            fpsGroup.Controls.Add(maxFrameRateTrackBar);
            fpsGroup.Controls.Add(maxFrameRateLabel);
            fpsGroup.Controls.Add(enableVSyncCheckBox);
            fpsGroup.Controls.Add(enableLODCheckBox);
            performanceTab.Controls.Add(fpsGroup);

            // Sağ taraf - Cache ayarları
            var cacheGroup = new GroupBox
            {
                Text = "Önbellek Ayarları",
                Size = new Size(400, 120),
                Location = new Point(450, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var cacheSizeLabel_ = new Label
            {
                Text = "Önbellek Boyutu:",
                Location = new Point(15, 25),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            cacheSizeTrackBar = new TrackBar
            {
                Minimum = 50,
                Maximum = 1024,
                Value = 256,
                TickFrequency = 100,
                Location = new Point(15, 45),
                Size = new Size(250, 45)
            };
            cacheSizeTrackBar.ValueChanged += (s, e) =>
            {
                cacheSizeLabel.Text = $"{cacheSizeTrackBar.Value} MB";
            };

            cacheSizeLabel = new Label
            {
                Text = "256 MB",
                Location = new Point(280, 55),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            cacheGroup.Controls.Add(cacheSizeLabel_);
            cacheGroup.Controls.Add(cacheSizeTrackBar);
            cacheGroup.Controls.Add(cacheSizeLabel);
            performanceTab.Controls.Add(cacheGroup);

            // Performans Modu
            var performanceModeGroup = new GroupBox
            {
                Text = "Performans Modu",
                Size = new Size(400, 80),
                Location = new Point(450, 160),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var performanceModeLabel = new Label
            {
                Text = "Mod:",
                Location = new Point(15, 30),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            performanceModeComboBox = new ComboBox
            {
                Location = new Point(70, 27),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            performanceModeComboBox.Items.AddRange(new[] { "Güç Tasarrufu", "Dengeli", "Performans", "Oyun" });
            performanceModeComboBox.SelectedIndex = 2;

            performanceModeGroup.Controls.Add(performanceModeLabel);
            performanceModeGroup.Controls.Add(performanceModeComboBox);
            performanceTab.Controls.Add(performanceModeGroup);

            settingsTabControl.TabPages.Add(performanceTab);
        }

        private void CreateVoiceControlTab()
        {
            var voiceTab = new TabPage("Ses Kontrolü")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var y = 20;

            // Ses Kontrolü Ana Ayarları
            var voiceMainGroup = new GroupBox
            {
                Text = "Ses Kontrolü Ayarları",
                Size = new Size(400, 200),
                Location = new Point(20, y),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableVoiceControlCheckBox = new CheckBox
            {
                Text = "Ses kontrolünü etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = true
            };

            var voiceLanguageLabel = new Label
            {
                Text = "Dil:",
                Location = new Point(15, 55),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            voiceLanguageComboBox = new ComboBox
            {
                Location = new Point(70, 52),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            voiceLanguageComboBox.Items.AddRange(new[] { "Türkçe (tr-TR)", "English (en-US)", "Deutsch (de-DE)", "Français (fr-FR)" });
            voiceLanguageComboBox.SelectedIndex = 0;

            var confidenceLabel = new Label
            {
                Text = "Güven Eşiği:",
                Location = new Point(15, 85),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            confidenceThresholdTrackBar = new TrackBar
            {
                Minimum = 40,
                Maximum = 95,
                Value = 70,
                TickFrequency = 10,
                Location = new Point(15, 105),
                Size = new Size(250, 45)
            };
            confidenceThresholdTrackBar.ValueChanged += (s, e) =>
            {
                confidenceThresholdLabel.Text = $"%{confidenceThresholdTrackBar.Value}";
            };

            confidenceThresholdLabel = new Label
            {
                Text = "%70",
                Location = new Point(280, 115),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            enableFeedbackCheckBox = new CheckBox
            {
                Text = "Sesli geri bildirim",
                Location = new Point(15, 155),
                Size = new Size(150, 20),
                ForeColor = Color.White,
                Checked = true
            };

            continuousListeningCheckBox = new CheckBox
            {
                Text = "Sürekli dinleme",
                Location = new Point(200, 155),
                Size = new Size(150, 20),
                ForeColor = Color.White,
                Checked = true
            };

            voiceMainGroup.Controls.Add(enableVoiceControlCheckBox);
            voiceMainGroup.Controls.Add(voiceLanguageLabel);
            voiceMainGroup.Controls.Add(voiceLanguageComboBox);
            voiceMainGroup.Controls.Add(confidenceLabel);
            voiceMainGroup.Controls.Add(confidenceThresholdTrackBar);
            voiceMainGroup.Controls.Add(confidenceThresholdLabel);
            voiceMainGroup.Controls.Add(enableFeedbackCheckBox);
            voiceMainGroup.Controls.Add(continuousListeningCheckBox);
            voiceTab.Controls.Add(voiceMainGroup);

            // Ses Çıkışı Ayarları
            var voiceOutputGroup = new GroupBox
            {
                Text = "Ses Çıkışı Ayarları",
                Size = new Size(400, 180),
                Location = new Point(450, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var preferredVoiceLabel = new Label
            {
                Text = "Tercih Edilen Ses:",
                Location = new Point(15, 25),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            preferredVoiceComboBox = new ComboBox
            {
                Location = new Point(15, 45),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            preferredVoiceComboBox.Items.AddRange(new[] { "Kadın Ses", "Erkek Ses", "Sistem Varsayılanı" });
            preferredVoiceComboBox.SelectedIndex = 0;

            var speechRateLabel_ = new Label
            {
                Text = "Konuşma Hızı:",
                Location = new Point(15, 80),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            speechRateTrackBar = new TrackBar
            {
                Minimum = -10,
                Maximum = 10,
                Value = 0,
                TickFrequency = 5,
                Location = new Point(15, 100),
                Size = new Size(200, 45)
            };
            speechRateTrackBar.ValueChanged += (s, e) =>
            {
                speechRateLabel.Text = speechRateTrackBar.Value.ToString();
            };

            speechRateLabel = new Label
            {
                Text = "0",
                Location = new Point(230, 110),
                Size = new Size(30, 20),
                ForeColor = Color.White
            };

            var volumeLabel_ = new Label
            {
                Text = "Ses Seviyesi:",
                Location = new Point(270, 80),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            volumeTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 80,
                TickFrequency = 20,
                Location = new Point(270, 100),
                Size = new Size(100, 45)
            };
            volumeTrackBar.ValueChanged += (s, e) =>
            {
                volumeLabel.Text = $"%{volumeTrackBar.Value}";
            };

            volumeLabel = new Label
            {
                Text = "%80",
                Location = new Point(280, 145),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            voiceOutputGroup.Controls.Add(preferredVoiceLabel);
            voiceOutputGroup.Controls.Add(preferredVoiceComboBox);
            voiceOutputGroup.Controls.Add(speechRateLabel_);
            voiceOutputGroup.Controls.Add(speechRateTrackBar);
            voiceOutputGroup.Controls.Add(speechRateLabel);
            voiceOutputGroup.Controls.Add(volumeLabel_);
            voiceOutputGroup.Controls.Add(volumeTrackBar);
            voiceOutputGroup.Controls.Add(volumeLabel);
            voiceTab.Controls.Add(voiceOutputGroup);

            settingsTabControl.TabPages.Add(voiceTab);
        }

        private void CreateGestureRecognitionTab()
        {
            var gestureTab = new TabPage("Hareket Tanıma")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var gestureMainGroup = new GroupBox
            {
                Text = "Hareket Tanıma Ayarları",
                Size = new Size(400, 250),
                Location = new Point(20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableGestureRecognitionCheckBox = new CheckBox
            {
                Text = "Hareket tanımayı etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = true
            };

            var gestureConfidenceLabel_ = new Label
            {
                Text = "Güven Eşiği:",
                Location = new Point(15, 55),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            gestureConfidenceTrackBar = new TrackBar
            {
                Minimum = 30,
                Maximum = 90,
                Value = 60,
                TickFrequency = 10,
                Location = new Point(15, 75),
                Size = new Size(250, 45)
            };
            gestureConfidenceTrackBar.ValueChanged += (s, e) =>
            {
                gestureConfidenceLabel.Text = $"%{gestureConfidenceTrackBar.Value}";
            };

            gestureConfidenceLabel = new Label
            {
                Text = "%60",
                Location = new Point(280, 85),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            enableLearningCheckBox = new CheckBox
            {
                Text = "Öğrenme modunu etkinleştir",
                Location = new Point(15, 125),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableRotationInvarianceCheckBox = new CheckBox
            {
                Text = "Döndürme bağımsızlığı",
                Location = new Point(15, 150),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableScaleInvarianceCheckBox = new CheckBox
            {
                Text = "Ölçek bağımsızlığı",
                Location = new Point(15, 175),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            var maxTemplatePointsLabel_ = new Label
            {
                Text = "Maksimum Şablon Noktası:",
                Location = new Point(15, 205),
                Size = new Size(150, 20),
                ForeColor = Color.White
            };

            maxTemplatePointsTrackBar = new TrackBar
            {
                Minimum = 32,
                Maximum = 128,
                Value = 64,
                TickFrequency = 16,
                Location = new Point(170, 200),
                Size = new Size(150, 45)
            };
            maxTemplatePointsTrackBar.ValueChanged += (s, e) =>
            {
                maxTemplatePointsLabel.Text = maxTemplatePointsTrackBar.Value.ToString();
            };

            maxTemplatePointsLabel = new Label
            {
                Text = "64",
                Location = new Point(330, 210),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            gestureMainGroup.Controls.Add(enableGestureRecognitionCheckBox);
            gestureMainGroup.Controls.Add(gestureConfidenceLabel_);
            gestureMainGroup.Controls.Add(gestureConfidenceTrackBar);
            gestureMainGroup.Controls.Add(gestureConfidenceLabel);
            gestureMainGroup.Controls.Add(enableLearningCheckBox);
            gestureMainGroup.Controls.Add(enableRotationInvarianceCheckBox);
            gestureMainGroup.Controls.Add(enableScaleInvarianceCheckBox);
            gestureMainGroup.Controls.Add(maxTemplatePointsLabel_);
            gestureMainGroup.Controls.Add(maxTemplatePointsTrackBar);
            gestureMainGroup.Controls.Add(maxTemplatePointsLabel);
            gestureTab.Controls.Add(gestureMainGroup);

            settingsTabControl.TabPages.Add(gestureTab);
        }

        private void CreateCollaborationTab()
        {
            var collaborationTab = new TabPage("İş Birliği")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var collaborationMainGroup = new GroupBox
            {
                Text = "İş Birliği Ayarları",
                Size = new Size(400, 220),
                Location = new Point(20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableCollaborationCheckBox = new CheckBox
            {
                Text = "İş birliğini etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            var serverPortLabel = new Label
            {
                Text = "Sunucu Portu:",
                Location = new Point(15, 55),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            serverPortNumeric = new NumericUpDown
            {
                Minimum = 1024,
                Maximum = 65535,
                Value = 8080,
                Location = new Point(120, 52),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            var maxConnectionsLabel = new Label
            {
                Text = "Maksimum Bağlantı:",
                Location = new Point(15, 85),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            maxConnectionsNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                Location = new Point(140, 82),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            enableVoiceChatCheckBox = new CheckBox
            {
                Text = "Ses sohbetini etkinleştir",
                Location = new Point(15, 115),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = false
            };

            enableScreenSharingCheckBox = new CheckBox
            {
                Text = "Ekran paylaşımını etkinleştir",
                Location = new Point(15, 140),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = false
            };

            autoReconnectCheckBox = new CheckBox
            {
                Text = "Otomatik yeniden bağlan",
                Location = new Point(15, 165),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            var connectionTimeoutLabel_ = new Label
            {
                Text = "Bağlantı Zaman Aşımı (sn):",
                Location = new Point(15, 190),
                Size = new Size(150, 20),
                ForeColor = Color.White
            };

            connectionTimeoutTrackBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 120,
                Value = 30,
                TickFrequency = 20,
                Location = new Point(170, 185),
                Size = new Size(150, 45)
            };
            connectionTimeoutTrackBar.ValueChanged += (s, e) =>
            {
                connectionTimeoutLabel.Text = $"{connectionTimeoutTrackBar.Value}s";
            };

            connectionTimeoutLabel = new Label
            {
                Text = "30s",
                Location = new Point(330, 195),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            collaborationMainGroup.Controls.Add(enableCollaborationCheckBox);
            collaborationMainGroup.Controls.Add(serverPortLabel);
            collaborationMainGroup.Controls.Add(serverPortNumeric);
            collaborationMainGroup.Controls.Add(maxConnectionsLabel);
            collaborationMainGroup.Controls.Add(maxConnectionsNumeric);
            collaborationMainGroup.Controls.Add(enableVoiceChatCheckBox);
            collaborationMainGroup.Controls.Add(enableScreenSharingCheckBox);
            collaborationMainGroup.Controls.Add(autoReconnectCheckBox);
            collaborationMainGroup.Controls.Add(connectionTimeoutLabel_);
            collaborationMainGroup.Controls.Add(connectionTimeoutTrackBar);
            collaborationMainGroup.Controls.Add(connectionTimeoutLabel);
            collaborationTab.Controls.Add(collaborationMainGroup);

            settingsTabControl.TabPages.Add(collaborationTab);
        }

        private void CreateEffectsTab()
        {
            var effectsTab = new TabPage("Efektler")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var effectsMainGroup = new GroupBox
            {
                Text = "Efekt Ayarları",
                Size = new Size(400, 180),
                Location = new Point(20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableEffectsCheckBox = new CheckBox
            {
                Text = "Efektleri etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableRealTimePreviewCheckBox = new CheckBox
            {
                Text = "Gerçek zamanlı önizleme",
                Location = new Point(15, 50),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableAnimatedEffectsCheckBox = new CheckBox
            {
                Text = "Animasyonlu efektler",
                Location = new Point(15, 75),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableParticleEffectsCheckBox = new CheckBox
            {
                Text = "Parçacık efektleri",
                Location = new Point(15, 100),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableShaderEffectsCheckBox = new CheckBox
            {
                Text = "Shader efektleri (Deneysel)",
                Location = new Point(15, 125),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = false
            };

            var defaultQualityLabel = new Label
            {
                Text = "Varsayılan Kalite:",
                Location = new Point(15, 155),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            defaultQualityComboBox = new ComboBox
            {
                Location = new Point(140, 152),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            defaultQualityComboBox.Items.AddRange(new[] { "Düşük", "Orta", "Yüksek", "Ultra" });
            defaultQualityComboBox.SelectedIndex = 2;

            effectsMainGroup.Controls.Add(enableEffectsCheckBox);
            effectsMainGroup.Controls.Add(enableRealTimePreviewCheckBox);
            effectsMainGroup.Controls.Add(enableAnimatedEffectsCheckBox);
            effectsMainGroup.Controls.Add(enableParticleEffectsCheckBox);
            effectsMainGroup.Controls.Add(enableShaderEffectsCheckBox);
            effectsMainGroup.Controls.Add(defaultQualityLabel);
            effectsMainGroup.Controls.Add(defaultQualityComboBox);
            effectsTab.Controls.Add(effectsMainGroup);

            settingsTabControl.TabPages.Add(effectsTab);
        }

        private void CreateMathToolsTab()
        {
            var mathTab = new TabPage("Matematik Araçları")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var mathMainGroup = new GroupBox
            {
                Text = "Matematik Araçları Ayarları",
                Size = new Size(400, 200),
                Location = new Point(20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableMathToolsCheckBox = new CheckBox
            {
                Text = "Matematik araçlarını etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = true
            };

            var defaultScaleLabel_ = new Label
            {
                Text = "Varsayılan Ölçek:",
                Location = new Point(15, 55),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            defaultScaleTrackBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 200,
                Value = 50,
                TickFrequency = 30,
                Location = new Point(15, 75),
                Size = new Size(250, 45)
            };
            defaultScaleTrackBar.ValueChanged += (s, e) =>
            {
                defaultScaleLabel.Text = defaultScaleTrackBar.Value.ToString();
            };

            defaultScaleLabel = new Label
            {
                Text = "50",
                Location = new Point(280, 85),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            enableGridSnappingCheckBox = new CheckBox
            {
                Text = "Izgaraya yaslama",
                Location = new Point(15, 125),
                Size = new Size(150, 20),
                ForeColor = Color.White,
                Checked = true
            };

            showCoordinatesCheckBox = new CheckBox
            {
                Text = "Koordinatları göster",
                Location = new Point(180, 125),
                Size = new Size(150, 20),
                ForeColor = Color.White,
                Checked = true
            };

            var gridSizeLabel_ = new Label
            {
                Text = "Izgara Boyutu:",
                Location = new Point(15, 155),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            gridSizeTrackBar = new TrackBar
            {
                Minimum = 5,
                Maximum = 50,
                Value = 20,
                TickFrequency = 10,
                Location = new Point(120, 150),
                Size = new Size(150, 45)
            };
            gridSizeTrackBar.ValueChanged += (s, e) =>
            {
                gridSizeLabel.Text = $"{gridSizeTrackBar.Value}px";
            };

            gridSizeLabel = new Label
            {
                Text = "20px",
                Location = new Point(280, 160),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            mathMainGroup.Controls.Add(enableMathToolsCheckBox);
            mathMainGroup.Controls.Add(defaultScaleLabel_);
            mathMainGroup.Controls.Add(defaultScaleTrackBar);
            mathMainGroup.Controls.Add(defaultScaleLabel);
            mathMainGroup.Controls.Add(enableGridSnappingCheckBox);
            mathMainGroup.Controls.Add(showCoordinatesCheckBox);
            mathMainGroup.Controls.Add(gridSizeLabel_);
            mathMainGroup.Controls.Add(gridSizeTrackBar);
            mathMainGroup.Controls.Add(gridSizeLabel);
            mathTab.Controls.Add(mathMainGroup);

            settingsTabControl.TabPages.Add(mathTab);
        }

        private void CreateSecurityTab()
        {
            var securityTab = new TabPage("Güvenlik")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var securityMainGroup = new GroupBox
            {
                Text = "Güvenlik Ayarları",
                Size = new Size(400, 220),
                Location = new Point(20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableEncryptionCheckBox = new CheckBox
            {
                Text = "Şifrelemeyi etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = false
            };

            requireAuthenticationCheckBox = new CheckBox
            {
                Text = "Kimlik doğrulama gerekli",
                Location = new Point(15, 50),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = false
            };

            var encryptionAlgorithmLabel = new Label
            {
                Text = "Şifreleme Algoritması:",
                Location = new Point(15, 80),
                Size = new Size(150, 20),
                ForeColor = Color.White
            };

            encryptionAlgorithmComboBox = new ComboBox
            {
                Location = new Point(170, 77),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            encryptionAlgorithmComboBox.Items.AddRange(new[] { "AES128", "AES256", "RSA2048" });
            encryptionAlgorithmComboBox.SelectedIndex = 1;

            var sessionTimeoutLabel_ = new Label
            {
                Text = "Oturum Zaman Aşımı (dk):",
                Location = new Point(15, 115),
                Size = new Size(150, 20),
                ForeColor = Color.White
            };

            sessionTimeoutTrackBar = new TrackBar
            {
                Minimum = 5,
                Maximum = 240,
                Value = 60,
                TickFrequency = 30,
                Location = new Point(15, 135),
                Size = new Size(250, 45)
            };
            sessionTimeoutTrackBar.ValueChanged += (s, e) =>
            {
                sessionTimeoutLabel.Text = $"{sessionTimeoutTrackBar.Value} dk";
            };

            sessionTimeoutLabel = new Label
            {
                Text = "60 dk",
                Location = new Point(280, 145),
                Size = new Size(50, 20),
                ForeColor = Color.White
            };

            enableAuditLogCheckBox = new CheckBox
            {
                Text = "Denetim günlüğünü etkinleştir",
                Location = new Point(15, 185),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Checked = false
            };

            allowRemoteAccessCheckBox = new CheckBox
            {
                Text = "Uzak erişime izin ver",
                Location = new Point(220, 185),
                Size = new Size(150, 20),
                ForeColor = Color.White,
                Checked = false
            };

            securityMainGroup.Controls.Add(enableEncryptionCheckBox);
            securityMainGroup.Controls.Add(requireAuthenticationCheckBox);
            securityMainGroup.Controls.Add(encryptionAlgorithmLabel);
            securityMainGroup.Controls.Add(encryptionAlgorithmComboBox);
            securityMainGroup.Controls.Add(sessionTimeoutLabel_);
            securityMainGroup.Controls.Add(sessionTimeoutTrackBar);
            securityMainGroup.Controls.Add(sessionTimeoutLabel);
            securityMainGroup.Controls.Add(enableAuditLogCheckBox);
            securityMainGroup.Controls.Add(allowRemoteAccessCheckBox);
            securityTab.Controls.Add(securityMainGroup);

            settingsTabControl.TabPages.Add(securityTab);
        }

        private void CreateDebugTab()
        {
            var debugTab = new TabPage("Hata Ayıklama")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(20)
            };

            var debugMainGroup = new GroupBox
            {
                Text = "Hata Ayıklama ve Günlük Ayarları",
                Size = new Size(400, 200),
                Location = new Point(20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            enableDebugModeCheckBox = new CheckBox
            {
                Text = "Hata ayıklama modunu etkinleştir",
                Location = new Point(15, 25),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = false
            };

            enableAdvancedLoggingCheckBox = new CheckBox
            {
                Text = "Gelişmiş günlük tutmayı etkinleştir",
                Location = new Point(15, 50),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = false
            };

            var logLevelLabel = new Label
            {
                Text = "Günlük Seviyesi:",
                Location = new Point(15, 80),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            logLevelComboBox = new ComboBox
            {
                Location = new Point(140, 77),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            logLevelComboBox.Items.AddRange(new[] { "Error", "Warning", "Information", "Debug", "Trace" });
            logLevelComboBox.SelectedIndex = 2;

            enablePerformanceCountersCheckBox = new CheckBox
            {
                Text = "Performans sayaçlarını etkinleştir",
                Location = new Point(15, 110),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = true
            };

            enableMemoryProfilingCheckBox = new CheckBox
            {
                Text = "Bellek profillemeyi etkinleştir",
                Location = new Point(15, 135),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = false
            };

            enableCrashReportingCheckBox = new CheckBox
            {
                Text = "Çökme raporlamasını etkinleştir",
                Location = new Point(15, 160),
                Size = new Size(250, 20),
                ForeColor = Color.White,
                Checked = true
            };

            debugMainGroup.Controls.Add(enableDebugModeCheckBox);
            debugMainGroup.Controls.Add(enableAdvancedLoggingCheckBox);
            debugMainGroup.Controls.Add(logLevelLabel);
            debugMainGroup.Controls.Add(logLevelComboBox);
            debugMainGroup.Controls.Add(enablePerformanceCountersCheckBox);
            debugMainGroup.Controls.Add(enableMemoryProfilingCheckBox);
            debugMainGroup.Controls.Add(enableCrashReportingCheckBox);
            debugTab.Controls.Add(debugMainGroup);

            settingsTabControl.TabPages.Add(debugTab);
        }

        private void LoadSettings()
        {
            try
            {
                //// Performans ayarlarını yükle
                //enableHardwareAcceleration.Checked = Properties.Settings.Default.EnableHardwareAcceleration;
                //enableMultithreading.Checked = Properties.Settings.Default.EnableMultithreading;
                //enableGpuAcceleration.Checked = Properties.Settings.Default.EnableGpuAcceleration;
                //maxFrameRateTrackBar.Value = Properties.Settings.Default.MaxFrameRate;
                //cacheSizeTrackBar.Value = Properties.Settings.Default.CacheSize;
                //enableVSyncCheckBox.Checked = Properties.Settings.Default.EnableVSync;
                //enableLODCheckBox.Checked = Properties.Settings.Default.EnableLOD;

                //// Ses kontrolü ayarlarını yükle
                //enableVoiceControlCheckBox.Checked = Properties.Settings.Default.EnableVoiceControl;
                //voiceLanguageComboBox.SelectedIndex = GetLanguageIndex(Properties.Settings.Default.VoiceLanguage);
                //confidenceThresholdTrackBar.Value = (int)(Properties.Settings.Default.VoiceConfidenceThreshold * 100);
                //enableFeedbackCheckBox.Checked = Properties.Settings.Default.VoiceFeedbackEnabled;
                //continuousListeningCheckBox.Checked = Properties.Settings.Default.ContinuousListening;
                //speechRateTrackBar.Value = Properties.Settings.Default.SpeechRate;
                //volumeTrackBar.Value = Properties.Settings.Default.SpeechVolume;

                //// Hareket tanıma ayarlarını yükle
                //enableGestureRecognitionCheckBox.Checked = Properties.Settings.Default.EnableGestureRecognition;
                //gestureConfidenceTrackBar.Value = (int)(Properties.Settings.Default.GestureConfidenceThreshold * 100);
                //enableLearningCheckBox.Checked = Properties.Settings.Default.EnableGestureLearning;
                //enableRotationInvarianceCheckBox.Checked = Properties.Settings.Default.EnableRotationInvariance;
                //enableScaleInvarianceCheckBox.Checked = Properties.Settings.Default.EnableScaleInvariance;
                //maxTemplatePointsTrackBar.Value = Properties.Settings.Default.MaxTemplatePoints;

                //// İş birliği ayarlarını yükle
                //enableCollaborationCheckBox.Checked = Properties.Settings.Default.EnableCollaboration;
                //serverPortNumeric.Value = Properties.Settings.Default.ServerPort;
                //maxConnectionsNumeric.Value = Properties.Settings.Default.MaxConnections;
                //enableVoiceChatCheckBox.Checked = Properties.Settings.Default.EnableVoiceChat;
                //enableScreenSharingCheckBox.Checked = Properties.Settings.Default.EnableScreenSharing;
                //autoReconnectCheckBox.Checked = Properties.Settings.Default.AutoReconnect;
                //connectionTimeoutTrackBar.Value = Properties.Settings.Default.ConnectionTimeout / 1000;

                //// Efekt ayarlarını yükle
                //enableEffectsCheckBox.Checked = Properties.Settings.Default.EnableEffects;
                //enableRealTimePreviewCheckBox.Checked = Properties.Settings.Default.EnableRealTimePreview;
                //enableAnimatedEffectsCheckBox.Checked = Properties.Settings.Default.EnableAnimatedEffects;
                //enableParticleEffectsCheckBox.Checked = Properties.Settings.Default.EnableParticleEffects;
                //defaultQualityComboBox.SelectedIndex = GetQualityIndex(Properties.Settings.Default.DefaultEffectQuality);
                //enableShaderEffectsCheckBox.Checked = Properties.Settings.Default.EnableShaderEffects;

                //// Matematik araçları ayarlarını yükle
                //enableMathToolsCheckBox.Checked = Properties.Settings.Default.EnableMathematicalTools;
                //defaultScaleTrackBar.Value = Properties.Settings.Default.DefaultMathScale;
                //enableGridSnappingCheckBox.Checked = Properties.Settings.Default.EnableGridSnapping;
                //showCoordinatesCheckBox.Checked = Properties.Settings.Default.ShowCoordinates;
                //gridSizeTrackBar.Value = Properties.Settings.Default.GridSize;

                //// Güvenlik ayarlarını yükle
                //enableEncryptionCheckBox.Checked = Properties.Settings.Default.EnableEncryption;
                //requireAuthenticationCheckBox.Checked = Properties.Settings.Default.RequireAuthentication;
                //encryptionAlgorithmComboBox.SelectedIndex = GetEncryptionIndex(Properties.Settings.Default.EncryptionAlgorithm);
                //sessionTimeoutTrackBar.Value = Properties.Settings.Default.SessionTimeout / 60;
                //enableAuditLogCheckBox.Checked = Properties.Settings.Default.EnableAuditLog;
                //allowRemoteAccessCheckBox.Checked = Properties.Settings.Default.AllowRemoteAccess;

                //// Debug ayarlarını yükle
                //enableDebugModeCheckBox.Checked = Properties.Settings.Default.EnableDebugMode;
                //enableAdvancedLoggingCheckBox.Checked = Properties.Settings.Default.EnableAdvancedLogging;
                //logLevelComboBox.SelectedIndex = GetLogLevelIndex(Properties.Settings.Default.LogLevel);
                //enablePerformanceCountersCheckBox.Checked = Properties.Settings.Default.EnablePerformanceCounters;
                //enableMemoryProfilingCheckBox.Checked = Properties.Settings.Default.EnableMemoryProfiling;
                //enableCrashReportingCheckBox.Checked = Properties.Settings.Default.EnableCrashReporting;

                UpdateLabels();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveSettings()
        {
            try
            {
                //// Performans ayarlarını kaydet
                //Properties.Settings.Default.EnableHardwareAcceleration = enableHardwareAcceleration.Checked;
                //Properties.Settings.Default.EnableMultithreading = enableMultithreading.Checked;
                //Properties.Settings.Default.EnableGpuAcceleration = enableGpuAcceleration.Checked;
                //Properties.Settings.Default.MaxFrameRate = maxFrameRateTrackBar.Value;
                //Properties.Settings.Default.CacheSize = cacheSizeTrackBar.Value;
                //Properties.Settings.Default.EnableVSync = enableVSyncCheckBox.Checked;
                //Properties.Settings.Default.EnableLOD = enableLODCheckBox.Checked;

                //// Ses kontrolü ayarlarını kaydet
                //Properties.Settings.Default.EnableVoiceControl = enableVoiceControlCheckBox.Checked;
                //Properties.Settings.Default.VoiceLanguage = GetLanguageCode(voiceLanguageComboBox.SelectedIndex);
                //Properties.Settings.Default.VoiceConfidenceThreshold = confidenceThresholdTrackBar.Value / 100f;
                //Properties.Settings.Default.VoiceFeedbackEnabled = enableFeedbackCheckBox.Checked;
                //Properties.Settings.Default.ContinuousListening = continuousListeningCheckBox.Checked;
                //Properties.Settings.Default.SpeechRate = speechRateTrackBar.Value;
                //Properties.Settings.Default.SpeechVolume = volumeTrackBar.Value;

                //// Hareket tanıma ayarlarını kaydet
                //Properties.Settings.Default.EnableGestureRecognition = enableGestureRecognitionCheckBox.Checked;
                //Properties.Settings.Default.GestureConfidenceThreshold = gestureConfidenceTrackBar.Value / 100f;
                //Properties.Settings.Default.EnableGestureLearning = enableLearningCheckBox.Checked;
                //Properties.Settings.Default.EnableRotationInvariance = enableRotationInvarianceCheckBox.Checked;
                //Properties.Settings.Default.EnableScaleInvariance = enableScaleInvarianceCheckBox.Checked;
                //Properties.Settings.Default.MaxTemplatePoints = maxTemplatePointsTrackBar.Value;

                //// İş birliği ayarlarını kaydet
                //Properties.Settings.Default.EnableCollaboration = enableCollaborationCheckBox.Checked;
                //Properties.Settings.Default.ServerPort = (int)serverPortNumeric.Value;
                //Properties.Settings.Default.MaxConnections = (int)maxConnectionsNumeric.Value;
                //Properties.Settings.Default.EnableVoiceChat = enableVoiceChatCheckBox.Checked;
                //Properties.Settings.Default.EnableScreenSharing = enableScreenSharingCheckBox.Checked;
                //Properties.Settings.Default.AutoReconnect = autoReconnectCheckBox.Checked;
                //Properties.Settings.Default.ConnectionTimeout = connectionTimeoutTrackBar.Value * 1000;

                //// Efekt ayarlarını kaydet
                //Properties.Settings.Default.EnableEffects = enableEffectsCheckBox.Checked;
                //Properties.Settings.Default.EnableRealTimePreview = enableRealTimePreviewCheckBox.Checked;
                //Properties.Settings.Default.EnableAnimatedEffects = enableAnimatedEffectsCheckBox.Checked;
                //Properties.Settings.Default.EnableParticleEffects = enableParticleEffectsCheckBox.Checked;
                //Properties.Settings.Default.DefaultEffectQuality = GetQualityString(defaultQualityComboBox.SelectedIndex);
                //Properties.Settings.Default.EnableShaderEffects = enableShaderEffectsCheckBox.Checked;

                //// Matematik araçları ayarlarını kaydet
                //Properties.Settings.Default.EnableMathematicalTools = enableMathToolsCheckBox.Checked;
                //Properties.Settings.Default.DefaultMathScale = defaultScaleTrackBar.Value;
                //Properties.Settings.Default.EnableGridSnapping = enableGridSnappingCheckBox.Checked;
                //Properties.Settings.Default.ShowCoordinates = showCoordinatesCheckBox.Checked;
                //Properties.Settings.Default.GridSize = gridSizeTrackBar.Value;

                //// Güvenlik ayarlarını kaydet
                //Properties.Settings.Default.EnableEncryption = enableEncryptionCheckBox.Checked;
                //Properties.Settings.Default.RequireAuthentication = requireAuthenticationCheckBox.Checked;
                //Properties.Settings.Default.EncryptionAlgorithm = GetEncryptionString(encryptionAlgorithmComboBox.SelectedIndex);
                //Properties.Settings.Default.SessionTimeout = sessionTimeoutTrackBar.Value * 60;
                //Properties.Settings.Default.EnableAuditLog = enableAuditLogCheckBox.Checked;
                //Properties.Settings.Default.AllowRemoteAccess = allowRemoteAccessCheckBox.Checked;

                //// Debug ayarlarını kaydet
                //Properties.Settings.Default.EnableDebugMode = enableDebugModeCheckBox.Checked;
                //Properties.Settings.Default.EnableAdvancedLogging = enableAdvancedLoggingCheckBox.Checked;
                //Properties.Settings.Default.LogLevel = GetLogLevelString(logLevelComboBox.SelectedIndex);
                //Properties.Settings.Default.EnablePerformanceCounters = enablePerformanceCountersCheckBox.Checked;
                //Properties.Settings.Default.EnableMemoryProfiling = enableMemoryProfilingCheckBox.Checked;
                //Properties.Settings.Default.EnableCrashReporting = enableCrashReportingCheckBox.Checked;

                //Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateLabels()
        {
            maxFrameRateLabel.Text = $"FPS: {maxFrameRateTrackBar.Value}";
            cacheSizeLabel.Text = $"{cacheSizeTrackBar.Value} MB";
            confidenceThresholdLabel.Text = $"%{confidenceThresholdTrackBar.Value}";
            speechRateLabel.Text = speechRateTrackBar.Value.ToString();
            volumeLabel.Text = $"%{volumeTrackBar.Value}";
            gestureConfidenceLabel.Text = $"%{gestureConfidenceTrackBar.Value}";
            maxTemplatePointsLabel.Text = maxTemplatePointsTrackBar.Value.ToString();
            connectionTimeoutLabel.Text = $"{connectionTimeoutTrackBar.Value}s";
            defaultScaleLabel.Text = defaultScaleTrackBar.Value.ToString();
            gridSizeLabel.Text = $"{gridSizeTrackBar.Value}px";
            sessionTimeoutLabel.Text = $"{sessionTimeoutTrackBar.Value} dk";
        }

        private void ResetToDefaults()
        {
            var result = MessageBox.Show(
                "Tüm ayarları varsayılan değerlere sıfırlamak istediğinizden emin misiniz?\n\nBu işlem geri alınamaz.",
                "Ayarları Sıfırla",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                //Properties.Settings.Default.Reset();
                LoadSettings();
                MessageBox.Show("Ayarlar varsayılan değerlere sıfırlandı.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Helper methods
        private int GetLanguageIndex(string languageCode)
        {
            return languageCode switch
            {
                "tr-TR" => 0,
                "en-US" => 1,
                "de-DE" => 2,
                "fr-FR" => 3,
                _ => 0
            };
        }

        private string GetLanguageCode(int index)
        {
            return index switch
            {
                0 => "tr-TR",
                1 => "en-US",
                2 => "de-DE",
                3 => "fr-FR",
                _ => "tr-TR"
            };
        }

        private int GetQualityIndex(string quality)
        {
            return quality switch
            {
                "Düşük" => 0,
                "Orta" => 1,
                "Yüksek" => 2,
                "Ultra" => 3,
                _ => 2
            };
        }

        private string GetQualityString(int index)
        {
            return index switch
            {
                0 => "Düşük",
                1 => "Orta",
                2 => "Yüksek",
                3 => "Ultra",
                _ => "Yüksek"
            };
        }

        private int GetEncryptionIndex(string algorithm)
        {
            return algorithm switch
            {
                "AES128" => 0,
                "AES256" => 1,
                "RSA2048" => 2,
                _ => 1
            };
        }

        private string GetEncryptionString(int index)
        {
            return index switch
            {
                0 => "AES128",
                1 => "AES256",
                2 => "RSA2048",
                _ => "AES256"
            };
        }

        private int GetLogLevelIndex(string logLevel)
        {
            return logLevel switch
            {
                "Error" => 0,
                "Warning" => 1,
                "Information" => 2,
                "Debug" => 3,
                "Trace" => 4,
                _ => 2
            };
        }

        private string GetLogLevelString(int index)
        {
            return index switch
            {
                0 => "Error",
                1 => "Warning",
                2 => "Information",
                3 => "Debug",
                4 => "Trace",
                _ => "Information"
            };
        }

        // Event handlers
        private void OkButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("Ayarlar başarıyla uygulandı.", "Bilgi",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            ResetToDefaults();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
            {
                var result = MessageBox.Show(
                    "Değişiklikleri kaydetmeden çıkmak istediğinizden emin misiniz?",
                    "Onay",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else if (result == DialogResult.Yes)
                {
                    SaveSettings();
                }
            }

            base.OnFormClosing(e);
        }
    }

    // Settings extension sınıfı
    public static class AdvancedSettingsExtensions
    {
        //public static void AddAdvancedSettings(this Properties.Settings settings)
        //{
        //    // Varsayılan değerler
        //    if (string.IsNullOrEmpty(settings.VoiceLanguage))
        //        settings.VoiceLanguage = "tr-TR";

        //    if (settings.VoiceConfidenceThreshold == 0)
        //        settings.VoiceConfidenceThreshold = 0.7f;

        //    if (settings.GestureConfidenceThreshold == 0)
        //        settings.GestureConfidenceThreshold = 0.6f;

        //    if (settings.MaxFrameRate == 0)
        //        settings.MaxFrameRate = 60;

        //    if (settings.CacheSize == 0)
        //        settings.CacheSize = 256;

        //    if (settings.ServerPort == 0)
        //        settings.ServerPort = 8080;

        //    if (settings.MaxConnections == 0)
        //        settings.MaxConnections = 10;

        //    if (settings.ConnectionTimeout == 0)
        //        settings.ConnectionTimeout = 30000;

        //    if (settings.DefaultMathScale == 0)
        //        settings.DefaultMathScale = 50;

        //    if (settings.GridSize == 0)
        //        settings.GridSize = 20;

        //    if (settings.MaxTemplatePoints == 0)
        //        settings.MaxTemplatePoints = 64;

        //    if (settings.SessionTimeout == 0)
        //        settings.SessionTimeout = 3600;

        //    if (settings.SpeechVolume == 0)
        //        settings.SpeechVolume = 80;

        //    if (string.IsNullOrEmpty(settings.DefaultEffectQuality))
        //        settings.DefaultEffectQuality = "Yüksek";

        //    if (string.IsNullOrEmpty(settings.EncryptionAlgorithm))
        //        settings.EncryptionAlgorithm = "AES256";

        //    if (string.IsNullOrEmpty(settings.LogLevel))
        //        settings.LogLevel = "Information";
        //}
    }
}