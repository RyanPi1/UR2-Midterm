/*HW 8, 9, Midterm C# Code
    Programmer: Ryan Pizzirusso
    Due Date:   Oct. 10-12, 2016
    Main Source Code*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Drawing;
using System.IO.Ports;

namespace Midterm_PC_Side
{
    struct PointPolar
    {/*point type for storing polar coordinates*/
        public float r;
        public float theta;
    }

    class UR2MidtermSource
    {
        //images for different processes
        Image<Bgr, Byte> BaseImg = null;
        Image<Bgr, Byte> BgrImg = null;
        //Image<Hsv, Byte> HsvImg = null;
        Image<Gray, Byte> CannyImg = null;

        //ratios (number of pixels per inch)
        double Xratio = 1;
        double Yratio = 1;

        //item lists
        public List<MCvBox2D> squares = new List<MCvBox2D>();
        public List<Triangle2DF> triangles = new List<Triangle2DF>();

        //other
        //MCvBox2D field;
        PointF[] fieldCorners = new PointF[4];

        //public functions
        public void getBaseImage(Image<Bgr, Byte> input)
        {/*collect the raw image to use (camera capture)*/
            BaseImg = input;
        }//end getBaseImage

        public void AutomaticCalibration(int CThreshold, int CLThreshold)
        {/*find the largest square in the base image (should be the paper for the field), then record it's corners*/
            Image<Gray, Byte> contourCheck = BaseImg.Convert<Gray, Byte>().PyrDown().PyrUp();
            contourCheck = contourCheck.Canny(CThreshold, CLThreshold);

            List<MCvBox2D> boxList = new List<MCvBox2D>();

            using (MemStorage storage = new MemStorage())
            {
                for (Contour<Point> contours = contourCheck.FindContours(); contours != null; contours = contours.HNext)
                {
                    Contour<Point> currentSquare = contours.ApproxPoly(contours.Perimeter * .1, storage);
                    if (contours.Area > 250 && currentSquare.Total == 4)
                    {
                        bool isRec = true;  //used to confirm shape is a rectangle
                        Point[] pts = currentSquare.ToArray();  //get all points comprising rectangle
                        LineSegment2D[] edges = PointCollection.PolyLine(pts, true);    //convert pts to a line. is closed shape

                        //check angles, confirm rectangle
                        for (int i = 0; i < edges.Length; i++)
                        {
                            double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));
                            //calculate angle of each vertex
                            if (angle < 80 || angle > 100)
                            {
                                isRec = false;  //if angle is not in range 80 - 100, confirm not a rectangle
                                break; //out of for (int i = 0; i < edges.Length; i++)
                            }// end if right angle

                        }//end for each side

                        if (isRec) //==true
                        {
                            boxList.Add(currentSquare.GetMinAreaRect()); //add current shape to box list
                        }//end if (isRec) //==true

                    }//end if not small area and 4 sides

                }//end for contours

            }//end using storage

            MCvBox2D largestBox = new MCvBox2D();   //make blank rectangle to put largest box in
            double largestArea = 0;  //variable for comparing areas bounded by rectangle

            foreach (MCvBox2D box in boxList)
            {
                PointF[] currentBox = box.GetVertices();    //get vertices of current box
                currentBox = currentBox.OrderBy(h => h.Y).ThenBy(h => h.X).ToArray();   //make sure vertex points are in correct order

                //compare points 1 and 2 to 0 to get width and height
                double width = Math.Sqrt(Convert.ToDouble(((currentBox[1].X-currentBox[0].X)*(currentBox[1].X-currentBox[0].X))+((currentBox[1].Y-currentBox[0].Y)*(currentBox[1].Y-currentBox[0].Y))));
                double height = Math.Sqrt(Convert.ToDouble(((currentBox[2].X - currentBox[0].X) * (currentBox[2].X - currentBox[0].X)) + ((currentBox[2].Y - currentBox[0].Y) * (currentBox[2].Y - currentBox[0].Y))));
                
                double currentArea = width * height;
                if (currentArea > largestArea)
                {
                    largestBox = box;
                    largestArea = currentArea;
                }//end if bigger box
            }//end for each rectangle

            //store points of largest box
            fieldCorners = largestBox.GetVertices();
            fieldCorners = fieldCorners.OrderBy(k => k.Y).ThenBy(k => k.X).ToArray();   //make sure vertex points are in correct order (matching Images and frames)
        }//end AutomaticCalibration

        //public void ManualCalibration(PointF[] corners)
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        fieldCorners[i] = corners[i];
        //    }
        //}//end Manual Calibration

        public Image<Bgr, Byte> WarpField()
        {/*warp the base image to make bgrImage line up with the field*/
            BgrImg = BaseImg.CopyBlank();
            PointF[] p2 = new PointF[4]
                { new PointF(0, 0), new PointF(BgrImg.Width, 0), new PointF(0, BgrImg.Height), new PointF(BgrImg.Width, BgrImg.Height) };
            HomographyMatrix homography = CameraCalibration.GetPerspectiveTransform(fieldCorners, p2);
            MCvScalar s = new MCvScalar(0, 0, 0);

            CvInvoke.cvWarpPerspective(BaseImg, BgrImg, homography, (int)Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, s);
            setRatios();
            return BgrImg;
        }

        public void centerFieldBypass()
        {/*does the job of setting up the BgrImage and ratios without attempting WarpPrespective to center the field
            assumes field is already centered and makes the working image the raw feed*/
            BgrImg = BaseImg;
            setRatios();
        }//end centerFieldBypass

        private void setRatios()
        {/*takes the working image (should already be lined up with field) and records the height and width ratios*/
            //known width and height of paper (assuming Landscape)
            double widthInches = 11;
            double heightInches = 8.5;

            //ratios are pixels per inch
            Xratio = BgrImg.Width / widthInches;
            Yratio = BgrImg.Height / heightInches;
        }//end setRatios

        public void setLists(int CThreshold, int CLThreshold)
        {/*find contours to define the lists of squares and triangles*/
            squares.Clear();
            triangles.Clear();

            CannyImg = BgrImg.Convert<Gray, Byte>().PyrDown().PyrUp();
            CannyImg = CannyImg.Canny(CThreshold, CLThreshold);

            using (MemStorage storage = new MemStorage())
            {
                for (Contour<Point> contours = CannyImg.FindContours(); contours != null; contours = contours.HNext)
                {
                    Contour<Point> currentShape = contours.ApproxPoly(contours.Perimeter * .1, storage);

                    if (contours.Area > 250)
                    {
                        if (currentShape.Total == 3)
                        {
                            Point[] pts = currentShape.ToArray();
                            triangles.Add(new Triangle2DF(pts[0], pts[1], pts[2]));
                        }//end if 3 points
                        else if (currentShape.Total == 4)
                        {
                            bool isRec = true;  //used to confirm shape is a rectangle
                            Point[] pts = currentShape.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);    //convert pts to a line. is closed shape

                            //check angles, confirm rectangle
                            for (int i = 0; i < edges.Length; i++)
                            {
                                double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));
                                //calculate angle of each vertex
                                if (angle < 75 || angle > 105)
                                {
                                    isRec = false;  //if angle is not in range 80 - 100, confirm not a rectangle
                                    break; //out of for (int i = 0; i < edges.Length; i++)
                                }// end if (angle < 80 || angle > 100)
                            }//end for (int i = 0; i < edges.Length; i++)

                            if (isRec)
                            {
                                squares.Add(currentShape.GetMinAreaRect());
                            }//end if isRec
                        }//end if 4 points
                    }//end if not small shape
                }//end for all contours
            }//end using

        }//end setLists

        //public PointPolar convertCoordinateSystemMidterm(PointF BasePixel, PointF newOrigin)
        //{/*converts a drawing point from the camera feed from cartesian and pixels to */
        //    //first, convert from pixels to inches
        //    PointF BaseInch = new PointF();
        //    BaseInch.X = BasePixel.X / (float)Xratio;
        //    BaseInch.Y = BasePixel.Y / (float)Yratio;

        //    //next, convert to new origin (centered around arm base instead of corner of paper)
        //    PointF ArmInch = new PointF();
        //    //ArmInch.X = BaseInch.X - newOrigin.X;
        //    //ArmInch.Y = BaseInch.Y - newOrigin.Y;

        //    ArmInch.X = newOrigin.X - BaseInch.X;
        //    ArmInch.Y = newOrigin.Y - BaseInch.Y;

        //    //finally, convert to polar, the angles needed from the base.
        //    PointPolar polarInch = new PointPolar();
        //    polarInch.r = (float)Math.Sqrt((ArmInch.X * ArmInch.X) + (ArmInch.Y * ArmInch.Y));

        //    float radTheta = (float)Math.Atan(ArmInch.Y / ArmInch.X);   //calculate angle in radians
        //    polarInch.theta = (radTheta * (180 / (float)Math.PI)) + 90;  //convert angle to degrees

        //    return polarInch;
        //}//end convertCoordinateSystemMidterm

        public PointPolar convertCoordinateSystemMidterm(PointF BasePixel, PointF newOrigin)
        {
            double Xpos = BasePixel.X / Xratio - newOrigin.X;
            double Ypos = newOrigin.Y - BasePixel.Y / Yratio;

            PointPolar result = new PointPolar();
            result.r = (float)Math.Sqrt((Xpos*Xpos)+(Ypos+Ypos));
            result.theta = Convert.ToInt32((Math.Atan(Xpos / Ypos) / 3.14 * 180) + 90);
            return result;
        }//end convertCoordinateSystemMidterm

        public void posServo(SerialPort sp, PointPolar pos)
        {/*sends the required positin data to the arduino*/
            byte[] send = new byte[1];
            send[0] = Convert.ToByte(pos.theta);
            sp.Write(send, 0, 1);
        }//end posServo

    }//end class
}//end namespace
