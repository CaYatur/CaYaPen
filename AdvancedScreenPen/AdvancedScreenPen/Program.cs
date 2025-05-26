using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdvancedScreenPen
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            // Enable visual styles and DPI awareness
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            try
            {
                // Show splash screen
                var splashForm = new SplashForm();
                splashForm.Show();

                // Simulate loading process with status updates
                await splashForm.ShowSplashAsync(3000);

                // Hide splash screen
                splashForm.Hide();

                // Create and show main form
                var mainForm = new MainForm();

                // Close splash form
                splashForm.Close();
                splashForm.Dispose();

                // Run main application
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uygulama baþlatýlýrken hata oluþtu:\n\n{ex.Message}\n\nDetaylar:\n{ex.StackTrace}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}