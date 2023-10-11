using System;
using System.Drawing;
using System.Windows.Forms;
namespace CaYaPen2
{
    public partial class Form1 : Form
    {
        private bool isEraser = false;  // Bu deðiþken, silgi modunu temsil eder
        private bool drawing = false;  // Çizim durumunu kontrol eder
        private bool drawingPer = false;  // Çizim iznini kontrol eder
        private Point previousPoint;
        public Form1()
        {
            this.DoubleBuffered = true;
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            // Formun Opaklýðýný Sýfýr Yaparak Tamamen Saydam Hale Getirme
        }


        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (drawingPer)
            {
                drawing = true;
                previousPoint = e.Location;
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawing)
            {
                Pen pen;
                if (isEraser)
                {
                    pen = new Pen(panel1.BackColor, 20);  // Silgi rengi
                }
                else
                {
                    pen = new Pen(Color.Black, 2);  // Kalem rengi
                }

                Graphics g = panel1.CreateGraphics();
                g.DrawLine(pen, previousPoint, e.Location);
                previousPoint = e.Location;
                g.Dispose();
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (drawing && drawingPer)
            {
                drawing = false;
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }


        private void button1_Click(object sender, EventArgs e)
        {
            isEraser = false;
            drawingPer = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            isEraser = false;
            drawingPer = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackColor = Color.White;
            this.TransparencyKey = Color.White;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            drawingPer = true;
            isEraser = true;
        }
    }
}