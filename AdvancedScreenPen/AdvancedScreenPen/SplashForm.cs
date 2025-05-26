using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdvancedScreenPen
{
    public partial class SplashForm : Form
    {
        private ProgressBar splashProgressBar;
        private Label splashStatusLabel;
        private PictureBox logoPictureBox;
        private Label titleLabel;
        private Label versionLabel;
        private Label copyrightLabel;
        private Label developerLabel;
        private Label dateLabel;

        public SplashForm()
        {
            InitializeComponent();
            SetupSplashScreen();
        }

        

        private void SetupSplashScreen()
        {
            // Logo
            logoPictureBox = new PictureBox
            {
                Size = new Size(128, 128),
                Location = new Point((Width - 128) / 2, 40),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent
            };

            // Gelişmiş logo oluştur
            var logoBitmap = new Bitmap(128, 128);
            using (var g = Graphics.FromImage(logoBitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Ana daire
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(10, 10, 108, 108),
                    Color.FromArgb(0, 120, 215),
                    Color.FromArgb(0, 90, 180),
                    45f))
                {
                    g.FillEllipse(brush, 10, 10, 108, 108);
                }

                // Kalem simgesi
                using (var pen = new Pen(Color.White, 6))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLine(pen, 35, 35, 93, 93);
                }

                // Kalem ucu
                using (var brush = new SolidBrush(Color.FromArgb(255, 200, 0)))
                {
                    g.FillEllipse(brush, 88, 88, 15, 15);
                }

                // Çizgi efekti
                using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 2))
                {
                    g.DrawCurve(pen, new PointF[] {
                        new PointF(45, 50),
                        new PointF(60, 45),
                        new PointF(75, 55),
                        new PointF(85, 50)
                    });
                }
            }
            logoPictureBox.Image = logoBitmap;
            Controls.Add(logoPictureBox);

            // Başlık
            titleLabel = new Label
            {
                Text = "Advanced Screen Pen",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 180)
            };
            titleLabel.Location = new Point((Width - titleLabel.PreferredWidth) / 2, 180);
            Controls.Add(titleLabel);

            // Versiyon
            versionLabel = new Label
            {
                Text = "Professional v2.0",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 120, 215),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 215)
            };
            versionLabel.Location = new Point((Width - versionLabel.PreferredWidth) / 2, 215);
            Controls.Add(versionLabel);

            // Geliştirici
            developerLabel = new Label
            {
                Text = "by CaYatur",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 245)
            };
            developerLabel.Location = new Point((Width - developerLabel.PreferredWidth) / 2, 245);
            Controls.Add(developerLabel);

            // Tarih
            dateLabel = new Label
            {
                Text = $"Build: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(140, 140, 140),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 270)
            };
            dateLabel.Location = new Point((Width - dateLabel.PreferredWidth) / 2, 270);
            Controls.Add(dateLabel);

            // Progress Bar
            splashProgressBar = new ProgressBar
            {
                Size = new Size(400, 8),
                Location = new Point((Width - 400) / 2, 310),
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.FromArgb(0, 120, 215),
                BackColor = Color.FromArgb(60, 60, 60),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            Controls.Add(splashProgressBar);

            // Status Label
            splashStatusLabel = new Label
            {
                Text = "Başlatılıyor...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 330)
            };
            splashStatusLabel.Location = new Point((Width - splashStatusLabel.PreferredWidth) / 2, 330);
            Controls.Add(splashStatusLabel);

            // Copyright
            copyrightLabel = new Label
            {
                Text = "© 2025 CaYatur. Tüm hakları saklıdır.",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 365)
            };
            copyrightLabel.Location = new Point((Width - copyrightLabel.PreferredWidth) / 2, 365);
            Controls.Add(copyrightLabel);
        }

        public void UpdateProgress(int percentage, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string>(UpdateProgress), percentage, status);
                return;
            }

            splashProgressBar.Value = Math.Min(Math.Max(percentage, 0), 100);
            splashStatusLabel.Text = status;
            splashStatusLabel.Location = new Point((Width - splashStatusLabel.PreferredWidth) / 2, 330);

            // Progress bar rengini değiştir
            if (percentage >= 90)
            {
                splashProgressBar.ForeColor = Color.FromArgb(0, 180, 0);
            }
            else if (percentage >= 50)
            {
                splashProgressBar.ForeColor = Color.FromArgb(255, 200, 0);
            }

            Refresh();
        }

        public async Task ShowSplashAsync(int durationMs = 3000)
        {
            Show();
            BringToFront();
            Refresh();

            var steps = new[]
            {
                (5, "Sistem kontrolleri yapılıyor..."),
                (15, "Bileşenler yükleniyor..."),
                (25, "Çizim motoru hazırlanıyor..."),
                (35, "Performans optimizasyonu..."),
                (45, "Ses sistemi başlatılıyor..."),
                (55, "Hareket tanıma sistemi yükleniyor..."),
                (65, "İş birliği modülü hazırlanıyor..."),
                (75, "Efekt motoru başlatılıyor..."),
                (85, "Katman sistemi yükleniyor..."),
                (95, "Kullanıcı arayüzü hazırlanıyor..."),
                (100, "Hazır! Hoş geldiniz.")
            };

            foreach (var (progress, status) in steps)
            {
                UpdateProgress(progress, status);
                await Task.Delay(durationMs / steps.Length);

                // Rastgele kısa duraksamalar ekle
                if (progress % 20 == 0)
                {
                    await Task.Delay(200);
                }
            }

            await Task.Delay(800); // Son mesajı göster
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Gradient background
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(45, 45, 48),
                Color.FromArgb(25, 25, 28),
                45f))
            {
                graphics.FillRectangle(brush, ClientRectangle);
            }

            // Glow effect border
            using (var pen = new Pen(Color.FromArgb(100, 0, 120, 215), 3))
            {
                graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
            }

            // Inner border
            using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1))
            {
                graphics.DrawRectangle(pen, 2, 2, Width - 5, Height - 5);
            }

            // Corner accents
            using (var brush = new SolidBrush(Color.FromArgb(50, 0, 120, 215)))
            {
                graphics.FillRectangle(brush, 0, 0, 20, 3);
                graphics.FillRectangle(brush, Width - 20, 0, 20, 3);
                graphics.FillRectangle(brush, 0, Height - 3, 20, 3);
                graphics.FillRectangle(brush, Width - 20, Height - 3, 20, 3);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Fade in effect
            this.Opacity = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 50 };
            timer.Tick += (s, args) =>
            {
                if (this.Opacity < 1.0)
                {
                    this.Opacity += 0.1;
                }
                else
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        public void FadeOut(Action onComplete = null)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 50 };
            timer.Tick += (s, args) =>
            {
                if (this.Opacity > 0)
                {
                    this.Opacity -= 0.1;
                }
                else
                {
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };
            timer.Start();
        }

        
    }
}