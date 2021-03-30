using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using AForge.Imaging.Filters;
using AForge.Imaging;


namespace OCR
{
    class PreprocessPage : WorkThread
    {
      
        public PreprocessPage() 
        {
            Treshold = 128;
        }

        public int Treshold {get; set;}
        public PageImage Image;

        public void Execute()
        {
            Console.WriteLine("Execute " + this.GetType().ToString());
            
            WorkPackage WorkPackage;
            int AreaHeight = Image.Area.Height / 1/*ProcessorCount*/;

            for (int i = 0; i < 1/*ProcessorCount*/; i++)
            {
                WorkPackage = new WorkPackage();
                WorkPackage.Method = ExecuteActivity;
                WorkPackage.Image = Image;
                WorkPackage.Assignment = new Rectangle(Image.Area.Left, 
                                                        Image.Area.Top + AreaHeight * i, 
                                                        Image.Area.Width,
                                                        AreaHeight);
                WorkPackage.Parameter = (object)Treshold;
                RunPackage(WorkPackage);
            }

            WaitForWorkDone(this.GetType().ToString());
        }

        public static void ExecuteActivity(object Parameter)
        {
            WorkPackage WorkPackage = (WorkPackage)Parameter;
            Rectangle Assignment = (Rectangle)WorkPackage.Assignment;




            //Step 1: Deskew
                Bitmap ImageToDeskew;

                ImageToDeskew = WorkPackage.Image.Image;

                DocumentSkewChecker skewChecker = new DocumentSkewChecker();
                skewChecker.MinBeta = -5;
                skewChecker.MaxBeta = 5;

                // get documents skew angle
                double angle = skewChecker.GetSkewAngle(ImageToDeskew);

                if (Math.Abs(angle) > 2)
                {
                    Console.WriteLine(" Deskewing original image");
                    
                    // create rotation filter
                    RotateBilinear rotationFilter = new RotateBilinear(-angle);
                    rotationFilter.FillColor = Color.White;

                    // rotate image applying the filter
                    Bitmap rotatedImage = rotationFilter.Apply(ImageToDeskew);

                    //set the rotated image in the PageImage of the Workpackage
                    WorkPackage.Image.Image = rotatedImage;
                }

            //WorkPackage WorkPackage = (WorkPackage)Parameter;
            //Rectangle Assignment = (Rectangle)WorkPackage.Assignment;
            int x = Assignment.X;
            int y = Assignment.Y;
            while (y < Assignment.Bottom)
            {
                x = 0;
                while (x < Assignment.Right)
                {
                    if (WorkPackage.Image.Bytes[x, y] > WorkPackage.Image.Threshold)
                    {
                        WorkPackage.Image.BinaryBytes[x, y] = 0xFF;
                    }
                    else
                    {
                        WorkPackage.Image.BinaryBytes[x, y] = 0x0;
                    }
                    x++;
                }
                y++;
            }

            SignalWorkDone();
        }
    }
}
