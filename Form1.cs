/*HW 8, 9, Midterm C# Code
    Programmer: Ryan Pizzirusso
    Due Date:   Oct. 10-12, 2016
    GUI Code*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO.Ports;

namespace Midterm_PC_Side
{
    public partial class Form1 : Form
    {
        UR2MidtermSource source = new UR2MidtermSource();
        Capture capture = null;
        Image<Bgr, Byte> img = null;

        enum Shapes { Tri, Sqr };
        Shapes currentShape;

        PointPolar TriPile = new PointPolar();
        PointPolar SquarePile = new PointPolar();

        int cannyThresh;
        int cannyLinkThresh;
        PointF[] corners = new PointF[4];
        int i;

        SerialPort sp1 = new SerialPort();

        public Form1()
        {
            InitializeComponent();
        }//end constructor

        private void Form1_Load(object sender, EventArgs e)
        {
            capture = new Capture(0);
            capture.ImageGrabbed += DisplayCaptured;
            capture.Start();

            label3.Text = trackBar1.Value.ToString();
            cannyThresh = trackBar1.Value;
            label4.Text = trackBar2.Value.ToString();
            cannyLinkThresh = trackBar2.Value;

            sp1.BaudRate = 115200;
            sp1.PortName = "COM3";
            sp1.Open();
            PointPolar temp = new PointPolar();
            temp.r = 1;
            temp.theta = 90;
            source.posServo(sp1, temp);

            img = capture.RetrieveBgrFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            imageBox1.Image = img;
            source.getBaseImage(img);
            source.AutomaticCalibration(cannyThresh, cannyLinkThresh);
            imageBox2.Image = source.WarpField();

            TriPile.r = 1;
            TriPile.theta = 0;
            SquarePile.r = 1;
            SquarePile.theta = 180;

            imageBox1.MouseClick += ibox_MouseClick;
            i=0;
        }//end load

        private void ibox_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            source.setLists(cannyThresh, cannyLinkThresh);
            imageBox3.Image = null;

            Image<Bgr, Byte> drawing = img.CopyBlank();

            foreach (Triangle2DF triangle in source.triangles)
            {
                drawing.Draw(triangle, new Bgr(Color.Yellow), 0);   //draw triangle on-screen
            }

            foreach (MCvBox2D square in source.squares)
            {
                drawing.Draw(square, new Bgr(Color.DarkOrange), 10);   //draw rectangle on screen
            }

            imageBox3.Image = drawing;
        }//end button1 click

        private void DisplayCaptured(object sender, EventArgs e)
        {
            img = capture.RetrieveBgrFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            imageBox1.Image = img;

            source.getBaseImage(img);
            imageBox2.Image = source.WarpField();
        }//end display captured

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label3.Text = trackBar1.Value.ToString();
            cannyThresh = trackBar1.Value;
        }//end trackbar1 scroll

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label4.Text = trackBar2.Value.ToString();
            cannyLinkThresh = trackBar2.Value;
        }//end trackbar2 scroll

        private void button2_Click(object sender, EventArgs e)
        {
            PointF armCenter = new PointF();
            armCenter.Y=(float)(22);
            armCenter.X=(float)(5.25);

            PointPolar position = new PointPolar();
            position.r = 1;
            position.theta = 90;

            if (source.triangles.Count > 0)
            {
                position = source.convertCoordinateSystemMidterm(source.triangles.ElementAt(1).Centeroid, armCenter);
                currentShape = Shapes.Tri;
            }
            else if (source.squares.Count > 0)
            {
                position = source.convertCoordinateSystemMidterm(source.squares.ElementAt(0).center, armCenter);
                currentShape = Shapes.Sqr;
            }

            source.posServo(sp1, position);
        }//end button2 click

        private void button3_Click(object sender, EventArgs e)
        {
            source.AutomaticCalibration(cannyThresh, cannyLinkThresh);
            imageBox2.Image = source.WarpField();
        }//end button3 click

        private void button4_Click(object sender, EventArgs e)
        {
            PointPolar position = new PointPolar();
            position.r = 1;
            position.theta = 90;

            PointF armCenter = new PointF();
            armCenter.Y = (float)(22);
            armCenter.X = (float)(5.25);

            if (currentShape == Shapes.Tri)
            {
                position = TriPile;
            }
            else if (currentShape == Shapes.Sqr)
            {
                position = SquarePile;
            }

            source.posServo(sp1, position);
        }//end button4 click

    }//end Form1 class
}//end namespace
