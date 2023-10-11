using System;
using System.Drawing;
using System.Windows.Forms;

namespace CaYaPen
{
    public partial class Form1 : Form
    {
        //private Pen pen;
        private bool drawing = false;  // Çizim durumunu kontrol eder
        private bool drawingPer = true;  // Çizim iznini kontrol eder
        private Point previousPoint;
        //private bool isDrawing = false;
        //private Point lastPoint;
        //private Pen pen;
        //private bool isWritingEnabled = true;

        public Form1()
        {
            InitializeComponent();
            //pen = new Pen(Color.Black, 2);
            //this.FormBorderStyle = FormBorderStyle.None;
            //this.WindowState = FormWindowState.Maximized;
            //this.TransparencyKey = Color.Magenta;  // Formun þeffaflýk rengini belirliyoruz
            //this.BackColor = Color.Magenta;  // Formun arka plan rengini ayarlýyoruz
            //this.DoubleBuffered = true;
            // Formun Opaklýðýný Sýfýr Yaparak Tamamen Saydam Hale Getirme
            //this.Opacity = 90;
            //panel1.BackColor = Color.FromArgb(50, Color.White);
            //this.FormBorderStyle = FormBorderStyle.None;
            //this.WindowState = FormWindowState.Maximized;
            //this.TransparencyKey = Color.Magenta;
            //this.BackColor = Color.Magenta;
            //this.DoubleBuffered = true;
            //this.TopMost = true;  // Formu en üstte tut

            // Kalem özelliklerini ayarla
            //pen = new Pen(Color.Black, 4);
            //pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void button1_Click(object sender, EventArgs e)
        {
            drawingPer = true;
            //isDrawing = !isDrawing;  // Çizim durumunu tersine çevir
        }

        private void button2_Click(object sender, EventArgs e)
        {
            drawingPer = false;
            //isDrawing = false;  // Çizim durumunu kapat
        }


        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (drawingPer == true)
            {
                drawing = true;
                previousPoint = e.Location;
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawing)
            {
                Pen pen = new Pen(Color.Black, 2);
                Graphics g = panel1.CreateGraphics();
                g.DrawLine(pen, previousPoint, e.Location);
                previousPoint = e.Location;
                g.Dispose();
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (drawingPer == true)
            {
                drawing = false;
            }
        }

    }
}
