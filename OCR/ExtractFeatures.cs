using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCR;
using System.Drawing;

namespace OCR
{
    class ExtractFeatures : WorkThread
    {
        public PageImage Image;

        private static int[] CCos = new int[] {1, 0, -1, 0};
        private static int[] CSin = new int[] { 0, 1, 0, -1 }; 

        public struct cDirection {

            public Point Pixel;
            public int lDirection;
            };

        public void Execute()
        {
            WorkPackage WorkPackage;
            int[] Assignment;

            Console.WriteLine("Execute " + this.GetType().ToString());

            for (int i = 0; i < ThreadCount; i++)
            {
                WorkPackage = new WorkPackage();
                WorkPackage.Method = ExecuteActivity;
                WorkPackage.Image = Image;

                Assignment = new int[2];
                Assignment[0] = i; //start index
                Assignment[1] = ThreadCount; //step size through list

                WorkPackage.Assignment = (object)Assignment;
                WorkPackage.Parameter = (object)0;
                RunPackage(WorkPackage);
            }

            WaitForWorkDone(this.GetType().ToString());
        }

        public static void ExecuteActivity(object Parameter)
        {
            WorkPackage WorkPackage = (WorkPackage)Parameter;
            int[] Assignment = (int[])WorkPackage.Assignment;

            for (int index = Assignment[0];
                 index < WorkPackage.Image.Components.Count;
                 index += Assignment[1])
            {
                ExecuteExtractFeatures(WorkPackage.Image, WorkPackage.Image.Components[index], false);
            }

            SignalWorkDone();
        }

        public static void ExecuteExtractFeatures(PageComponent Component, bool NoDebug)
        {
            PageImage tempImage;

            tempImage = new PageImage();

            ExecuteExtractFeatures(Component, Component, NoDebug);
        }

            
            
        /// <summary>
        /// This function executes all feature extraction for the given component
        /// </summary>
        /// <param name="Component"></param>
        public static void ExecuteExtractFeatures(PageBase Image, PageComponent Component, bool NoDebug)
        {
            Bitmap Bitmap;
            String Filename;
            String ComponentID;

            try
            {

                if (Component.BinaryBytes.Length > 0)
                {

                    ComponentID = "000000" + Component.ID;
                    ComponentID = ComponentID.Substring(ComponentID.Length - 6);

                    //Gap detection
                    GapDetection(Component);

                    if (DebugTrace.DebugTrace.TraceFeatures && !NoDebug)
                    {
                        Bitmap = DebugTrace.DebugTrace.CreateBitmapFromByteArray(Component.Bytes, new Size(Component.Width, Component.Height));
                        Filename = DebugTrace.DebugTrace.TraceFeatureFolder + "image_" + ComponentID + "_org.bmp";
                        Bitmap.Save(Filename);
                        Bitmap = DebugTrace.DebugTrace.CreateBitmapFromByteArray(Component.BinaryBytes, new Size(Component.Width, Component.Height));
                        Filename = DebugTrace.DebugTrace.TraceFeatureFolder + "image_" + ComponentID + "_binary.bmp";
                        Bitmap.Save(Filename);
                    }

                    //Create compare matrix 32x32 pixels
                    CreateCompareMatrix(Image, Component);

                    if (DebugTrace.DebugTrace.TraceFeatures && !NoDebug)
                    {
                        Bitmap = DebugTrace.DebugTrace.CreateBitmapFromByteArray(Component.CompareMatrix, new Size(32, 32));
                        Filename = DebugTrace.DebugTrace.TraceFeatureFolder + "image_" + ComponentID + "_32.gif";
                        Bitmap.Save(Filename, System.Drawing.Imaging.ImageFormat.Gif);
                        Bitmap = new Bitmap(Filename);
                        Bitmap.Save(DebugTrace.DebugTrace.TraceFeatureFolder + "image_" + ComponentID + "_32.bmp",
                            System.Drawing.Imaging.ImageFormat.Bmp);
                    }

                    //ContourThinning(Component.CompareMatrix, 0, 255);
                    //MiddleThinning(Component.CompareMatrix, 0, 255);
                    for (int x = 0; x < 32; x++)
                    {
                        for (int y = 0; y < 32; y++)
                        {
                            if (Component.CompareMatrix[x, y] != 0) Component.CompareMatrix[x, y] = 255;

                        }
                    }


                    // ErodeThinning(Component.CompareMatrix, 0, 255);

                    //Thinning
                    //Thinning(Component.CompareMatrix, 0, 255, 255);

                    PreThinning(Component.CompareMatrix, 0, 255);
                    CondensedThinning(Image, Component, 0, 255, 255);
                    DoThinningSuperfluous(Component.CompareMatrix, 0, 255);

                    if (DebugTrace.DebugTrace.TraceFeatures && !NoDebug)
                    {
                        Bitmap = DebugTrace.DebugTrace.CreateBitmapFromByteArray(Component.CompareMatrix, new Size(32, 32));
                        Filename = DebugTrace.DebugTrace.TraceFeatureFolder + "image_" + ComponentID + "_T1.bmp";
                        Bitmap.Save(Filename);
                    }

                    //Stroke detection
                    for (int x = 0; x < 32; x++)
                    {
                        for (int y = 0; y < 32; y++)
                        {
                            Component.StrokeMatrix[x, y] = (Byte)(Component.CompareMatrix[x, y] == 0x00 ? 0x00 : 0xFF);
                        }
                    }

                    //Stroke detection
                    for (int x = 0; x < 32; x++)
                    {
                        for (int y = 0; y < 32; y++)
                        {
                            Component.StrokeMatrix[x, y] = Component.CompareMatrix[x, y];
                        }
                    }

                    int Strokes = FindStrokes(Component.StrokeMatrix, -1);
                    Component.Strokes = Strokes;

                    if (DebugTrace.DebugTrace.TraceFeatures && !NoDebug)
                    {
                        Bitmap = DebugTrace.DebugTrace.CreateBitmapFromByteArray(Component.StrokeMatrix, new Size(32, 32));
                        Filename = DebugTrace.DebugTrace.TraceFeatureFolder + "image_" + ComponentID + "_S1.bmp";
                        Bitmap.Save(Filename);
                    }

                    //Find endpoint and junctions
                    FindEndpointsAndJunctions(Component.PixelTypeMatrix, Component.CompareMatrix);

                    //Calculate the projection in the component data structures
                    CalculateProjections(Component);

                    //Compute the position of the component in relation to the
                    //sentence it belongs to
                    ComputePosition(Component);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message);
                Console.WriteLine("  In: " + e.StackTrace);
            }
        }

        /// <summary>
        /// This function does a pre thinning step. It removes possible pixels that might
        /// to noise in the thinning process
        /// </summary>
        /// <param name="pcCompareBitmap"></param>
        /// <param name="piForegroundColor"></param>
        /// <param name="piBackgroundColor"></param>
        public static void PreThinning(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    // . o
                    // o x
                    if (DoThinningGetPixel(x + 0, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                        DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                        DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                        DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) == 1)
                    {
                        pcCompareBitmap[x, y] = 0xFF;
                        pcCompareBitmap[x + 1, y + 1] = 0xFF;
                    } 

                    // 0 .
                    // x o
                    if (DoThinningGetPixel(x - 0, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                        DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                        DoThinningGetPixel(x - 0, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                        DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) == 1)
                    {
                        pcCompareBitmap[x, y] = 0xFF;
                        pcCompareBitmap[x - 1, y + 1] = 0xFF;
                    }
                }
            }
        }

        #region "ContourThinning"

        public class ContourPoint
        {
            public Point Point = new Point();
            public int Direction = 0; // 0 = E; 2 = S; 4 = W; 6 = N;
        }

        public class Contour
        {
            public List<ContourPoint> ContourPoints = new List<ContourPoint>(0);
        }

        #endregion

        #region "ErodeThinning"

        public static void ErodeThinning(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor, int iMaxIterations)
        {
            bool Continue = true;
            int PassesToDo = Math.Min(iMaxIterations, 16);
            List<Point> PointsToErase = new List<Point>(0);
            Point PointToErase;
            int i;

            do {
                Continue = false;

                PointsToErase.Clear();

                //i = PassesToDo % 4;
                
                for ( i=0; i<4; i++) 
                {
                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 31; x >=0; x--)
                        {
                            if (pcCompareBitmap[x, y] == piForegroundColor)
                            {
                                if (DoErodeThinningTest(pcCompareBitmap, piForegroundColor, piBackgroundColor, x, y, i))
                                {
                                    PointToErase = new Point(x, y);
                                    PointsToErase.Add(PointToErase);
                                    Continue = true;
                                }
                            }
                        }
                    }

                    foreach (Point Point in PointsToErase)
                    {
                        pcCompareBitmap[Point.X, Point.Y] = 0xFF;
                    }
                }

                PassesToDo--;

            } while (Continue && PassesToDo>0);
       }

        private static bool DoErodeThinningTest(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor, int X, int Y, int Angle)
        {
            bool Result = false;

            if (
                !DoThinningTestPixel(X, 0, Y, -1, Angle, pcCompareBitmap, piForegroundColor) &&
                //DoThinningTestPixel(X, 0, Y, 0, Angle, pcCompareBitmap, piForegroundColor) &&
                DoThinningTestPixel(X, 0, Y, 1, Angle, pcCompareBitmap, piForegroundColor))
            {
                if (
                    !(
                    DoThinningTestPixel(X, -1, Y, -1, Angle, pcCompareBitmap, piForegroundColor) &&
                    !DoThinningTestPixel(X, -1, Y, 0, Angle, pcCompareBitmap, piForegroundColor)
                    ) &&

                    !(
                    DoThinningTestPixel(X, 1, Y, -1, Angle, pcCompareBitmap, piForegroundColor) &&
                    !DoThinningTestPixel(X, 1, Y, 0, Angle, pcCompareBitmap, piForegroundColor)
                    )

                ) Result = true;
            }

            return Result;
        }

        #endregion

        #region "MiddleThinning"

        public static void MiddleThinning(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor)
        {
            int[] PixelRun;

            PixelRun = new int[32];
            for (int i = 0; i < 32; i++) PixelRun[i] = -1;

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (PixelRun[y]!=-1)
                    {
                        if (pcCompareBitmap[x, y] == piBackgroundColor) 
                        {
                            pcCompareBitmap[PixelRun[y] + (x-PixelRun[y])/2, y] = 16;
                            PixelRun[y] = -1;
                        }
                    }
                    else
                    {
                        if (pcCompareBitmap[x, y] != piBackgroundColor) PixelRun[y] = x;
                    }
                }
            }

            for (int i = 0; i < 32; i++)
            {
                if (PixelRun[i] != -1)
                {
                    pcCompareBitmap[PixelRun[i] + (32 - PixelRun[i]) / 2, i] = 16;
                    PixelRun[i] = -1;
                }
            }

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (PixelRun[x] != -1)
                    {
                        if (pcCompareBitmap[x,y] == piBackgroundColor)
                        {
                            pcCompareBitmap[x, PixelRun[x] + (y - PixelRun[x]) / 2] = 16;
                            PixelRun[x] = -1;
                        }
                    }
                    else
                    {
                        if (pcCompareBitmap[x, y] != piBackgroundColor) PixelRun[x] = y;
                    }
                }
            }
            for (int i = 0; i < 32; i++)
            {
                if (PixelRun[i] != -1)
                {
                    pcCompareBitmap[i, PixelRun[i] + (32 - PixelRun[i]) / 2] = 16;
                    PixelRun[i] = -1;
                }
            }

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (pcCompareBitmap[x, y] == 16)
                    {
                        pcCompareBitmap[x, y] = 0;
                    }
                    else
                    {
                        pcCompareBitmap[x, y] = 255;
                    }
                }
            }
        }

#endregion

        #region "ThinningWithCondensedPlate"

        public static void CondensedThinning(PageBase Image, PageComponent Component, int piForegroundColor, int piBackgroundColor, int Iterations)
        {
            DoThinningPruning3(Image, Component, piForegroundColor, piBackgroundColor, 3);
            
            int k = 0;
            bool Continue = false;
            Byte[,] CondensedPlate;
            List<Point> ToBeRemoved = new List<Point>(0);
            Byte[,] pcCompareBitmap = Component.CompareMatrix;

            do
            {
                Continue = false;

                //Create the condensed plate
                CondensedPlate = new Byte[32, 32];

                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        CondensedPlate[x, y] = (Byte)(CondensedThinningTestNeighbours(pcCompareBitmap, x, y, piForegroundColor) ? 0x00 : 0xFF);
                    }
                }

                //Check all black pixels if it's a border pixel
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        if (DoThinningGetPixel(x, y, pcCompareBitmap, piForegroundColor) == 1)
                        {
                            if (CondensedThinningTestTemplates(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor)) 
                            {
                                ToBeRemoved.Add(new Point(x, y));
                                Continue = true;
                            }
                        }
                    }
                }

                //Remove the points
                foreach (Point Point in ToBeRemoved)
                {
                    pcCompareBitmap[Point.X, Point.Y] = 0xFF;
                }

                k++;

            } while (Continue && k<Iterations);
        }

        public static bool CondensedThinningTestNeighbours(Byte[,] pcCompareBitmap, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 0, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 0, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) == 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplates(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            //Condition 1: Pixel its neighborhood is matching with any of the templates a - n or r.
            if (CondensedThinningTestTemplateA(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateB(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateC(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateD(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateE(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateF(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateG(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateH(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateI(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateJ(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateK(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateL(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateM(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateN(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateR(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor)) Result = true;

            //Condition 2: Pixel its neighborhood is matching with any of the template pairs 0 - q.
            if (CondensedThinningTestTemplateO(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateP(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor) ||
                CondensedThinningTestTemplateQ(pcCompareBitmap, CondensedPlate, x, y, piForegroundColor)) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateA(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateB(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateC(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 2, y, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateD(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y + 2, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 0, y - 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateE(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 0, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 0, y - 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateF(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 0, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 0, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateG(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 0, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateH(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateI(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateJ(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 0, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 0, y - 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateK(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) == 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateL(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateM(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateN(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) == 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateO(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y - 2, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 2, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 2, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 0, y + 1, CondensedPlate, piForegroundColor) == 1 ) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateP(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 2, y - 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y - 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x + 1, y - 1, CondensedPlate, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, CondensedPlate, piForegroundColor) == 1) Result = true;

            return Result;
        }

        public static bool CondensedThinningTestTemplateQ(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) != 1 &&
                DoThinningGetPixel(x - 1, y + 1, CondensedPlate, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, CondensedPlate, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, CondensedPlate, piForegroundColor) == 1) Result = true;

            return Result;
        }
        public static bool CondensedThinningTestTemplateR(Byte[,] pcCompareBitmap, Byte[,] CondensedPlate, int x, int y, int piForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x - 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y - 2, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y - 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 2, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 2, y + 0, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x - 1, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y + 1, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 0, y + 2, pcCompareBitmap, piForegroundColor) == 1 &&
                DoThinningGetPixel(x + 1, y + 1, pcCompareBitmap, piForegroundColor) != 1) Result = true;

            return Result;
        }

        #endregion

        //#region "ContourThinning"

        //public static void ContourThinning(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor)
        //{
        //    List<Contour> Contours = new List<Contour>(0);
            
        //    Point Pixel = new Point();
        //    Contour newContour;
        //    bool PixelFound = false;

        //    //find a first foreground pixel which is a neighbour of a non foregroundpixel
        //    //and isn't detected yet

        //    do
        //    {

        //        PixelFound = false;

        //        for (int y = 0; y < 32 && !PixelFound; y++)
        //        {
        //            for (int x = 0; x < 32 && !PixelFound; x++)
        //            {
        //                if (DoThinningGetPixel(x, y, pcCompareBitmap, piForegroundColor) == 1)
        //                {
        //                    Pixel = new Point(x, y);
        //                    PixelFound = DoContourThinningHasBackgroundNeighbour(Pixel, pcCompareBitmap, piBackgroundColor);
        //                }
        //            }
        //        }

        //        //walk the line/contour.
        //        if (PixelFound)
        //        {
        //            //Initiate a new contour
        //            ContourPoint newPoint;
        //            ContourPoint checkPoint;
        //            bool Continue = true;
        //            int Direction;
        //            Point CurrentPixel;
        //            double Angle;

        //            newContour = new Contour();
        //            newPoint = new ContourPoint();

        //            newPoint.Point.X = Pixel.X;
        //            newPoint.Point.Y = Pixel.Y;
        //            newPoint.Direction = 0;

        //            int iteration = 250;


        //            do
        //            {
        //                CurrentPixel = newPoint.Point;
        //                Direction = newPoint.Direction;
        //                Angle = Direction * Math.PI / 4;

        //                if (DoThinningTestPixel(newPoint.Point.X, 1, newPoint.Point.Y, 0, Angle, pcCompareBitmap, piForegroundColor) &&
        //                    !DoThinningTestPixel(newPoint.Point.X, 1, newPoint.Point.Y, -1, Angle, pcCompareBitmap, piForegroundColor))
        //                {
        //                    //Continue with walking
        //                    newContour.ContourPoints.Add(newPoint);
        //                    newPoint = new ContourPoint();
        //                    newPoint.Direction = Direction;
        //                    newPoint.Point.X = (int)(CurrentPixel.X + Math.Cos(Angle) * 1 + Math.Sin(Angle) * 0);
        //                    newPoint.Point.Y = (int)(CurrentPixel.Y - Math.Sin(Angle) * 1 + Math.Cos(Angle) * 0);
        //                }
        //                else
        //                {
        //                    if (DoThinningTestPixel(newPoint.Point.X, 1, newPoint.Point.Y, -1, Angle, pcCompareBitmap, piForegroundColor))
        //                    {
        //                        // One of the patterns below occurred. 
        //                        // o x     o x
        //                        // . x or  . o
        //                        // Our next pixel should be the pixel in the upper right corner.

        //                        Angle = Direction * Math.PI / 4;
        //                        Direction += 2;
        //                        if (Direction >= 8) Direction = 0;

        //                        newContour.ContourPoints.Add(newPoint);

        //                        newPoint = new ContourPoint();
        //                        newPoint.Direction = Direction;
        //                        newPoint.Point.X = (int)(CurrentPixel.X + Math.Cos(Angle) * 1 + Math.Sin(Angle) * -1);
        //                        newPoint.Point.Y = (int)(CurrentPixel.Y - Math.Sin(Angle) * 1 + Math.Cos(Angle) * -1);
        //                    }
        //                    else
        //                    {
        //                        //The direct neighbours of the current point are white. 
        //                        // o o 
        //                        // . o
        //                        // Rotation around the current pixel
        //                        Direction -= 2;
        //                        if (Direction < 0) Direction += 8;

        //                        newPoint.Direction = Direction;
        //                    }

        //                }

        //                if (newContour.ContourPoints.Count > 0)
        //                {
        //                    checkPoint = newContour.ContourPoints[0];
        //                    if ((checkPoint.Direction == newPoint.Direction) &&
        //                        (checkPoint.Point.X == newPoint.Point.X) &&
        //                        (checkPoint.Point.Y == newPoint.Point.Y))
        //                    {
        //                        //we have reached the end
        //                        Continue = false;
        //                    }
        //                }

        //                iteration--;

        //            } while (Continue && iteration > 0);

        //            foreach (ContourPoint Point in newContour.ContourPoints)
        //            {
        //                pcCompareBitmap[Point.Point.X, Point.Point.Y] = 16;
        //            }

        //            Contours.Add(newContour);
        //        }
        //    } while (PixelFound);

        //    for (int y = 0; y < 32; y++)
        //    {
        //        for (int x = 0; x < 32; x++)
        //        {
        //            ///pcCompareBitmap[x, y] = 0xFF;
        //        }
        //    }

        //    foreach (Contour Contour in Contours)
        //    {
        //        foreach (ContourPoint Point in Contour.ContourPoints)
        //        {
        //            pcCompareBitmap[Point.Point.X, Point.Point.Y] = 0xff;
        //        }
        //    }
            
        //    //iterate and shrink the inside of the contour(s)
        //    //for (int i = 0; i < 0; i++)
        //    //{
        //    //    Double Angle;
                
        //    //    foreach (Contour Contour in Contours)
        //    //    {
        //    //        foreach (ContourPoint Point in Contour.ContourPoints)
        //    //        {

        //    //            Angle = (Point.Direction) * Math.PI / 4;

        //    //            Point.Point.X += (int)(Math.Cos(Angle) * 0 + Math.Sin(Angle) * 1);
        //    //            Point.Point.Y += (int)(Math.Sin(Angle) * 0 + Math.Cos(Angle) * 1);

        //    //            if (Point.Point.X >= 0 && Point.Point.X < 32 && Point.Point.Y >= 0 && Point.Point.Y < 32)
        //    //            {
        //    //                pcCompareBitmap[Point.Point.X, Point.Point.Y] = (byte)(32 + i * 16);
        //    //            }
        //    //        }
        //    //    }
        //    //}

        //    //draw the result
        //    foreach (Contour Contour in Contours)
        //    {
        //        foreach (ContourPoint Point in Contour.ContourPoints)
        //        {
        //            for (int Size = 1; Size < 5; Size++)
        //            {
        //                ContourThinningDrawBox(pcCompareBitmap, 16 - Size, piBackgroundColor, Point.Point.X, Point.Point.Y, Size);
        //            }
        //        }
        //    }
        //}

        //private static void ContourThinningDrawBox(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor, int X, int Y, int Size)
        //{
        //    for (int PosX = X - Size; PosX <= X + Size; PosX++)
        //    {
        //        ContourThinningSetPixel(pcCompareBitmap, piForegroundColor, piForegroundColor, PosX, Y - Size);
        //    }
        //    for (int PosX = X - Size; PosX <= X + Size; PosX++)
        //    {
        //        ContourThinningSetPixel(pcCompareBitmap, piForegroundColor, piForegroundColor, PosX, Y + Size);
        //    }
        //    for (int PosY= Y - Size; PosY <= Y + Size; PosY++)
        //    {
        //        ContourThinningSetPixel(pcCompareBitmap, piForegroundColor, piForegroundColor, X-Size, PosY);
        //    }
        //    for (int PosY = Y - Size; PosY <= Y + Size; PosY++)
        //    {
        //        ContourThinningSetPixel(pcCompareBitmap, piForegroundColor, piForegroundColor, X + Size, PosY);
        //    }
        
        
        
        
        
        //}

        //private static void ContourThinningSetPixel(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor, int X, int Y)
        //{
        //    if (X >= 0 && X <= 31 && Y >= 0 && Y <= 31)
        //    {
        //        if (pcCompareBitmap[X, Y] != piBackgroundColor && pcCompareBitmap[X, Y] < piForegroundColor)
        //        {
        //            pcCompareBitmap[X, Y] = (Byte)piForegroundColor;
        //        }
        //    }
        //}


        ///// <summary>
        ///// This function checks if the given pixel has a neighbour that is a non-foreground pixel.
        ///// </summary>
        ///// <param name="Pixel"></param>
        ///// <param name="pcCompareBitmap"></param>
        ///// <param name="piForegroundColor"></param>
        ///// <returns></returns>
        //private static bool DoContourThinningHasBackgroundNeighbour(Point Pixel, Byte[,] pcCompareBitmap, int piBackgroundColor)
        //{
        //    bool Result = false;

        //    if (/*DoThinningGetPixel(Pixel.X - 1, Pixel.Y, pcCompareBitmap, piBackgroundColor) == 1 ||
        //        DoThinningGetPixel(Pixel.X + 1, Pixel.Y, pcCompareBitmap, piBackgroundColor) == 1 ||*/
        //        DoThinningGetPixel(Pixel.X, Pixel.Y - 1, pcCompareBitmap, piBackgroundColor) == 1/* ||
        //        DoThinningGetPixel(Pixel.X, Pixel.Y + 1, pcCompareBitmap, piBackgroundColor) == 1*/) Result = true;

        //    return Result;
        //}

        //#endregion

        #region "FindEndpointsAndJunctions"

        /// <summary>
        /// This function iterates through the compare matrix and determines which pixels
        /// are junctions or endpoints.
        /// </summary>
        /// <param name="PixelTypeMatrix"></param>
        /// <param name="CompareMatrix"></param>
        public static void FindEndpointsAndJunctions(ePixelType[,] PixelTypeMatrix, Byte[,] CompareMatrix)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    PixelTypeMatrix[x, y] = DoDetermineEndpointOrJunction(CompareMatrix, x, y);
                }
            }
        }

        public static ePixelType DoDetermineEndpointOrJunction(Byte[,] CompareMatrix, int xpos, int ypos)
        {
            ePixelType Result = ePixelType.Background;
            int PixelCount = 0;

            //count number of pixels around current position
            if (CompareMatrix[xpos, ypos] != 0xFF)
            {
                for (int x = xpos - 1; x < xpos + 2; x++)
                {
                    for (int y = ypos - 1; y < ypos + 2; y++)
                    {
                        if (x != xpos || y != ypos)
                        {
                            if (GetPixelBitmap(CompareMatrix, x, y) != 0xFF) PixelCount++;
                        }
                    }
                }

                //Translate pixel count to pixeltype
                switch (PixelCount)
                {
                    case 0:

                        Result = ePixelType.Regular;
                        break;

                    case 1:

                        Result = ePixelType.End;
                        break;

                    case 2:

                        Result = ePixelType.Regular;
                        break;

                    default:

                        Result = ePixelType.Junction;
                        break;

                }
            }

            return Result;
        }

        #endregion

        #region "CalculateProjections"

        /// <summary>
        /// This function transforms the compare and stroke matrix to 
        /// arrays. The arrays are projects of the columns(x) and the rows(y).
        /// </summary>
        /// <param name="Component"></param>
        public static void CalculateProjections(PageComponent Component)
        {
            //clear the variable spaces
            for (int i = 0; i < 32; i++)
            {
                Component.lPixelProjectionX[i] = 0;
                Component.lPixelProjectionY[i] = 0;
                Component.lStrokeDirectionX[i] = 0;
                Component.lStrokeDirectionY[i] = 0;
            }

            //Start extracting the pixel projection. 
            for (int lY = 0; lY < 32; lY++)
            {
                for (int lX = 0; lX < 32; lX++)
                {
                    if (Component.CompareMatrix[lX, lY] == 0x00)
                    {
                        //Pixel Count
                        Component.lPixelProjectionX[lX]++;
                        Component.lPixelProjectionY[lY]++;
                    }

                    if (Component.StrokeMatrix[lX, lY] == 16 || Component.StrokeMatrix[lX, lY] == 80)
                    {
                        //Stroke direction
                        Component.lStrokeDirectionX[lX] += Component.StrokeMatrix[lX, lY];
                    }
                    if (Component.StrokeMatrix[lX, lY] == 112 || Component.StrokeMatrix[lX, lY] == 48)
                    {
                        //Stroke direction
                       // Component.lStrokeDirectionX[lX] += Component.StrokeMatrix[lX, lY];
                        Component.lStrokeDirectionY[lY] += Component.StrokeMatrix[lX, lY];
                    }
                }
            }

            //Fill the matrix for the diagonals
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Component.lStrokeMatrixNW[x, y] = 0;
                    Component.lStrokeMatrixSW[x, y] = 0;
                }
            }

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (Component.StrokeMatrix[x, y] == 64 || Component.StrokeMatrix[x, y] == 128)
                    {
                        Component.lStrokeMatrixNW[x / 16, y / 16]++;
                    }
                    if (Component.StrokeMatrix[x, y] == 32 || Component.StrokeMatrix[x, y] == 96)
                    {
                        Component.lStrokeMatrixSW[x / 16, y / 16]++;
                    }
                }
            }

            //Fill the matrix for endpoints and junctions. The matrices are 3x3 and have the
            //value 1 in a cell when there is at least one endpoint or junction.
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (Component.PixelTypeMatrix[x, y] == ePixelType.End)
                    {
                        Component.PixelTypeProjectionEndpoint[x / 11, y / 11] = 1;
                    }
                    if (Component.PixelTypeMatrix[x, y] == ePixelType.Junction)
                    {
                        Component.PixelTypeProjectionJunction[x / 11, y / 11] = 1;
                    }
                }
            }
        }

        #endregion

        #region "GapDetection"

        /// <summary>
        /// This function counts the number of gaps within this component
        /// </summary>
        /// <param name="Component"></param>
        public static void GapDetection(PageComponent Component)
        {


        }

        #endregion

        #region "Thinning"

        /// <summary>
        ///  Functions for thinning. Based on algorithm as described by Gonzales and Woods
        ///  in their book 'Digital Image Processing'. Source partially based on
        ///  implementation by the ipl98 group (http://ipl98.sourceforge.net)
        /// </summary>
        /// <param name="pcCompareBitmap"></param>
        /// <param name="piForegroundColor"></param>
        /// <param name="piBackgroundColor"></param>
        public static void Thinning(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor, int Iterations) 
        {
            //DoThinningMB2(pcCompareBitmap, piForegroundColor, piBackgroundColor);
           // DoThinningSuperfluous(pcCompareBitmap, piForegroundColor, piBackgroundColor);
          //  DoThinningPruning(pcCompareBitmap, piForegroundColor, piBackgroundColor);

           // return;


            bool bPointsRemoved = false;
            List<Point> ToBeRemovedList = new List<Point>(0);
            List<Point> ForegroundPixelList = new List<Point>(0);
            Byte[,] bNeighbors = new Byte[3,3];
            Point oPoint = new Point();

            //collect the foreground pixels, and do the first step in seperating
            //pixels which can be removed from other foreground pixels.
            for (int y=0; y<32; y++) 
            {

                for (int x=0; x<32; x++) 
                {

                    oPoint.X = x;
                    oPoint.Y = y;

                    if (pcCompareBitmap[x,y]==piForegroundColor) 
                    {

                        if (DoThinningSearchNeighbors(x, y, pcCompareBitmap, bNeighbors, piForegroundColor) &&
                            DoThinningCheckTransitions(bNeighbors) &&
                            DoThinningStep1cdTests(bNeighbors)) 
                        {

                            bPointsRemoved = true;
                            ToBeRemovedList.Add(oPoint);
                        }
                        else 
                        {

                            ForegroundPixelList.Add(oPoint);
                        }
                    }
                }
            }

            //Set pixels in toberemovedlist to backgroundcolor
            for (int lIndex=0; lIndex<ToBeRemovedList.Count; lIndex++) 
            {
                pcCompareBitmap[ToBeRemovedList[lIndex].X, ToBeRemovedList[lIndex].Y] = (Byte)piBackgroundColor;
            }
            ToBeRemovedList.Clear();

            if (bPointsRemoved) 
            {

                for (int lIndex=0; lIndex<ForegroundPixelList.Count; lIndex++) 
                {

                    oPoint = ForegroundPixelList[lIndex];

                    if (DoThinningSearchNeighbors(oPoint.X, oPoint.Y, pcCompareBitmap, bNeighbors, piForegroundColor) &&
                        DoThinningCheckTransitions(bNeighbors) &&
                        DoThinningStep2cdTests(bNeighbors)) 
                    {

                        bPointsRemoved = true;

                        ToBeRemovedList.Add(oPoint);

                        ForegroundPixelList.RemoveAt(lIndex);

                        lIndex--;
                    }
                }
            }

            //Set pixels in toberemovedlist to backgroundcolor
            for (int lIndex=0; lIndex<ToBeRemovedList.Count(); lIndex++) 
            {

                pcCompareBitmap[ToBeRemovedList[lIndex].X, ToBeRemovedList[lIndex].Y] = (Byte)piBackgroundColor;
            }
            ToBeRemovedList.Clear();
            Iterations--;

            //iterate while no points are removed in the last iteration
            while (bPointsRemoved && Iterations>0) 
            {
                bPointsRemoved = false;
                Iterations--;

                //step 1
                for (int lIndex=0; lIndex<ForegroundPixelList.Count(); lIndex++) 
                {

                    oPoint = ForegroundPixelList[lIndex];

                    if (DoThinningSearchNeighbors(oPoint.X, oPoint.Y, pcCompareBitmap, bNeighbors, piForegroundColor) &&
                        DoThinningCheckTransitions(bNeighbors) &&
                        DoThinningStep1cdTests(bNeighbors)) 
                    {

                        bPointsRemoved = true;

                        ToBeRemovedList.Add(oPoint);

                        ForegroundPixelList.RemoveAt(lIndex);

                        lIndex--;
                    }
                }

                //Set pixels in toberemovedlist to backgroundcolor
                for (int lIndex=0; lIndex<ToBeRemovedList.Count(); lIndex++) 
                {

                    pcCompareBitmap[ToBeRemovedList[lIndex].X, ToBeRemovedList[lIndex].Y] = (Byte)piBackgroundColor;
                }
                ToBeRemovedList.Clear();


                //step 2
                for (int lIndex=0; lIndex<ForegroundPixelList.Count(); lIndex++) 
                {
                    oPoint = ForegroundPixelList[lIndex];

                    if (DoThinningSearchNeighbors(oPoint.X, oPoint.Y, pcCompareBitmap, bNeighbors, piForegroundColor) &&
                        DoThinningCheckTransitions(bNeighbors) &&
                        DoThinningStep2cdTests(bNeighbors)) 
                    {

                        bPointsRemoved = true;

                        ToBeRemovedList.Add(oPoint);

                        ForegroundPixelList.RemoveAt(lIndex);

                        lIndex--;
                    }
                }

                //Set pixels in toberemovedlist to backgroundcolor
                for (int lIndex=0; lIndex<ToBeRemovedList.Count(); lIndex++) 
                {
                    pcCompareBitmap[ToBeRemovedList[lIndex].X, ToBeRemovedList[lIndex].Y] = (Byte)piBackgroundColor;
                }
             
                ToBeRemovedList.Clear();
            }
            ForegroundPixelList.Clear();

        //    DoThinningSuperfluous(pcCompareBitmap, piForegroundColor, piBackgroundColor);
        }

        private static bool DoThinningSuperfluous(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor)
        {
            bool Result = false;

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (pcCompareBitmap[x, y] == (Byte)piForegroundColor)
                    {
                        if (DoThinningCheckSuperfluous(x, y, pcCompareBitmap, piForegroundColor))
                        {
                            pcCompareBitmap[x, y] = (Byte)piBackgroundColor;
                            Result = true;
                        }
                    }
                }
            }
            return Result;
        }

        private static bool DoThinningMB2(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor)
        {
            bool Result = false;
            List<Point> ToBeRemoved = new List<Point>(0);

            do
            {
                Result = false;

                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        if (pcCompareBitmap[x, y] == (Byte)piForegroundColor)
                        {
                            if (DoThinningTestMB2(x, y, pcCompareBitmap, piForegroundColor))
                            {
                               // pcCompareBitmap[x,y] = (Byte)piBackgroundColor;
                                ToBeRemoved.Add(new Point(x, y));
                                Result = true;
                            }
                        }
                    }
                }

                if (Result)
                {
                    foreach (Point point in ToBeRemoved)
                    {
                        pcCompareBitmap[point.X, point.Y] = (Byte)piBackgroundColor;
                    }
                    ToBeRemoved.Clear();
                }

            } while (Result);

            return Result;
        }

        private static bool DoThinningPruning(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor)
        {
            bool Result = false;
            List<Point> ToBeRemoved = new List<Point>(0);

            do
            {
                Result = false;

                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        if (pcCompareBitmap[x, y] == (Byte)piForegroundColor)
                        {
                            if (DoThinningTestPruning(x, y, pcCompareBitmap, piForegroundColor))
                            {
                                ToBeRemoved.Add(new Point(x, y));
                                Result = true;
                            }
                        }
                    }
                }

                if (Result)
                {
                    foreach (Point point in ToBeRemoved)
                    {
                        pcCompareBitmap[point.X, point.Y] = (Byte)piBackgroundColor;
                    }
                    ToBeRemoved.Clear();
                }

            } while (Result);

            return Result;
        }

        public static bool DoThinningPruning2(Byte[,] pcCompareBitmap, int piForegroundColor, int piBackgroundColor)
        {
            bool Result = false;
            bool PerpendicularRun = false;
            List<Point> ToBeRemoved = new List<Point>(0);
            int RunStart = -1;
            int LengthPerpendicular;
            int Angle = 0;

            do
            {
                Result = false;

                Angle = 0;
                {
                    for (int y = 0; y < 32; y++)
                    {
                        RunStart = -1;

                        for (int x = 0; x < 32; x++)
                        {
                            if (pcCompareBitmap[x, y] == (Byte)piForegroundColor)
                            {
                                if (RunStart == -1)
                                {
                                    if (DoThinningTestPruning2Start(x, y, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {
                                        RunStart = x;
                                    }
                                }
                            }

                            if (RunStart != -1)
                            {
                                if (!DoThinningTestPruning2Run(x, y, pcCompareBitmap, piForegroundColor, Angle) || !DoThinningTestPixel(x, 0, y, 0, 0, pcCompareBitmap, piForegroundColor))
                                {
                                    if (DoThinningTestPruning2End(x - 1, y, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {

                                        //Check how the length of this run compares to the perpendicular, and if the perpendicular
                                        //is longer. Remove this run of pixels
                                        LengthPerpendicular = 0;
                                        PerpendicularRun = true;

                                        while (PerpendicularRun && LengthPerpendicular < 32)
                                        {
                                            for (int i = RunStart; i<x; i++)
                                            {
                                                if (!DoThinningTestPixel(i, 0, y, LengthPerpendicular, 0, pcCompareBitmap, piForegroundColor))
                                                {
                                                    PerpendicularRun = false;
                                                }
                                            }

                                            if (PerpendicularRun) LengthPerpendicular++;
                                        }

                                        if (LengthPerpendicular > (x - RunStart))
                                        {
                                            for (int i = RunStart; i < x; i++)
                                            {
                                                pcCompareBitmap[i, y] = (Byte)piBackgroundColor;
                                            }

                                            Result = true;
                                        }
                                    }
                                    RunStart = -1;
                                }
                            }
                        }
                    }
                }

                Angle = 2;
                {
                    for (int y = 31; y >= 0; y--)
                    {
                        RunStart = -1;

                        for (int x = 31; x >= 0; x--)
                        {
                            if (pcCompareBitmap[x, y] == (Byte)piForegroundColor)
                            {
                                if (RunStart == -1)
                                {
                                    if (DoThinningTestPruning2Start(x, y, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {
                                        RunStart = x;
                                    }
                                }
                            }

                            if (RunStart != -1)
                            {
                                if (!DoThinningTestPruning2Run(x, y, pcCompareBitmap, piForegroundColor, Angle) || !DoThinningTestPixel(x, 0, y, 0, 0, pcCompareBitmap, piForegroundColor))
                                {
                                    if (DoThinningTestPruning2End(x + 1, y, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {

                                        //Check how the length of this run compares to the perpendicular, and if the perpendicular
                                        //is longer. Remove this run of pixels
                                        LengthPerpendicular = 0;
                                        PerpendicularRun = true;

                                        while (PerpendicularRun && LengthPerpendicular<32)
                                        {
                                            for (int i = x + 1; i<=RunStart; i++)
                                            {
                                                if (!DoThinningTestPixel(i, 0, y, -LengthPerpendicular, 0, pcCompareBitmap, piForegroundColor))
                                                {
                                                    PerpendicularRun = false;
                                                }
                                            }

                                            if (PerpendicularRun) LengthPerpendicular++;
                                        }

                                        if (LengthPerpendicular > (RunStart - x))
                                        {
                                            for (int i = x+1; i <= RunStart; i++)
                                            {
                                                pcCompareBitmap[i, y] = (Byte)piBackgroundColor;
                                            }

                                            Result = true;
                                        }
                                    }
                                    RunStart = -1;
                                }
                            }
                        }
                    }
                }

            } while (Result);

            do
            {
                Result = false;

                Angle = 3;
                {
                    for (int x = 31; x >= 0; x--)
                    {
                        RunStart = -1;

                        for (int y = 0; y < 32; y++)
                        {
                            if (pcCompareBitmap[x, y] == (Byte)piForegroundColor)
                            {
                                if (RunStart == -1)
                                {
                                    if (DoThinningTestPruning2Start(x, y, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {
                                        RunStart = y;
                                    }
                                }
                            }

                            if (RunStart != -1)
                            {
                                if (!DoThinningTestPruning2Run(x, y, pcCompareBitmap, piForegroundColor, Angle) || !DoThinningTestPixel(x, 0, y, 0, 0, pcCompareBitmap, piForegroundColor))
                                {
                                    if (DoThinningTestPruning2End(x, y - 1, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {

                                        //Check how the length of this run compares to the perpendicular, and if the perpendicular
                                        //is longer. Remove this run of pixels
                                        LengthPerpendicular = 0;
                                        PerpendicularRun = true;

                                        while (PerpendicularRun && LengthPerpendicular < 32)
                                        {
                                            for (int i = RunStart; i < y; i++)
                                            {
                                                if (!DoThinningTestPixel(x, -LengthPerpendicular, i, 0, 0, pcCompareBitmap, piForegroundColor))
                                                {
                                                    PerpendicularRun = false;
                                                }
                                            }

                                            if (PerpendicularRun) LengthPerpendicular++;
                                        }


                                        if (LengthPerpendicular > (y - RunStart))
                                        {
                                            for (int i = RunStart; i < y; i++)
                                            {
                                                pcCompareBitmap[x, i] = (Byte)piBackgroundColor;
                                            }

                                            Result = true;
                                        }
                                    }
                                    RunStart = -1;
                                }
                            }
                        }
                    }
                }

                Angle = 1;
                {
                    for (int x = 0; x < 32; x++)
                    {
                        RunStart = -1;

                        for (int y = 31; y >= 0; y--)
                        {
                            if (pcCompareBitmap[x, y] == (Byte)piForegroundColor)
                            {
                                if (RunStart == -1)
                                {
                                    if (DoThinningTestPruning2Start(x, y, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {
                                        RunStart = y;
                                    }
                                }
                            }

                            if (RunStart != -1)
                            {
                                if (!DoThinningTestPruning2Run(x, y, pcCompareBitmap, piForegroundColor, Angle) || !DoThinningTestPixel(x, 0, y, 0, 0, pcCompareBitmap, piForegroundColor))
                                {
                                    if (DoThinningTestPruning2End(x, y + 1, pcCompareBitmap, piForegroundColor, Angle, 3))
                                    {

                                        //Check how the length of this run compares to the perpendicular, and if the perpendicular
                                        //is longer. Remove this run of pixels
                                        LengthPerpendicular = 0;
                                        PerpendicularRun = true;

                                        while (PerpendicularRun && LengthPerpendicular<32)
                                        {
                                            for (int i = y; i < RunStart; i++)
                                            {
                                                if (!DoThinningTestPixel(x, LengthPerpendicular, i, 0, 0, pcCompareBitmap, piForegroundColor))
                                                {
                                                    PerpendicularRun = false;
                                                }
                                            }

                                            if (PerpendicularRun) LengthPerpendicular++;
                                        }


                                        if (LengthPerpendicular > (RunStart - y))
                                        {
                                            for (int i = y + 1; i <= RunStart; i++)
                                            {
                                                pcCompareBitmap[x, i] = (Byte)piBackgroundColor;
                                            }

                                            Result = true;
                                        }
                                    }
                                    RunStart = -1;
                                }
                            }
                        }
                    }
                }

            } while (Result);

            return Result;
        }

        public static bool DoThinningPruning3(PageBase Image, PageComponent Component, int piForegroundColor, int piBackgroundColor, int MaxDepth)
        {
            bool Result = false;
            List<Point> ToBeRemoved = new List<Point>(0);
            Point Point;
            int Angle = 0;
            int x, y = 0;
            int Depth = 0;
            int Cos, Sin;
            int[] StartPointX = new int[] {0, 0, 31, 31};
            int[] StartPointY = new int[] { 0, 31, 31, 0 };
            Byte[,] CompareMatrix = Component.CompareMatrix;

            do
            {
                Result = false;

                for (Angle = 0; Angle < 4; Angle++)
                {
                    Cos = CCos[Angle];
                    Sin = CSin[Angle];

                    for (int dy = 0; dy < 32; dy++)
                    {
                        ToBeRemoved.Clear();

                        for (int dx = 0; dx < 32; dx++)
                        {
                            x = StartPointX[Angle] + Cos * dx + Sin * dy;
                            y = StartPointY[Angle] -Sin * dx + Cos * dy;

                            if (CompareMatrix[x, y] != 0xFF) //(Byte)piForegroundColor)
                            {
                                if (ToBeRemoved.Count == 0)
                                {
                                    if (DoThinningTestPruning2Start(x, y, CompareMatrix, piForegroundColor, Angle, MaxDepth))
                                    {
                                        ToBeRemoved.Add(new Point(x,y));
                                    }
                                }
                            }

                            if (ToBeRemoved.Count != 0)
                            {
                                if (!DoThinningTestPruning2Run(x, y, CompareMatrix, piForegroundColor, Angle) || !DoThinningTestPixel(x, 0, y, 0, 0, CompareMatrix, piForegroundColor))
                                {
                                    Point = ToBeRemoved[ToBeRemoved.Count - 1];

                                    //if (DoThinningTestPruning2End(Point.X, Point.Y, CompareMatrix, piForegroundColor, Angle, MaxDepth))
                                    {

                                        //Check how the length of this run compares to the perpendicular, and if the perpendicular
                                        //is longer. Remove this run of pixels
                                        //if (DoThinningPruningTestRunAgainstPerpendicular(ToBeRemoved, Angle, pcCompareBitmap, piForegroundColor))
                                        if (DoThinningPruningTestRunAgainstGrayLevel(ToBeRemoved, Component, Image))
                                            {
                                            foreach (Point RunPoint in ToBeRemoved)
                                            {
                                                CompareMatrix[RunPoint.X, RunPoint.Y] = (Byte)piBackgroundColor;
                                            }

                                            Result = true;
                                        }
                                    }
                                    ToBeRemoved.Clear();
                                }
                                else
                                {
                                    //the run continues
                                    ToBeRemoved.Add(new Point(x, y));
                                }
                            }
                        }
                    }
                }

                Depth++;

            } while (Result && Depth < MaxDepth);

            return Result;
        }

        /// <summary>
        /// This function compare the pixelrun against its perpendicular pixelrun. If the pixel run
        /// is smaller then it returns true.
        /// </summary>
        /// <param name="ToBeRemoved"></param>
        /// <param name="Angle"></param>
        /// <param name="pcCompareBitmap"></param>
        /// <param name="piForegroundColor"></param>
        /// <returns></returns>
        private static bool DoThinningPruningTestRunAgainstPerpendicular(List<Point> ToBeRemoved,
                                                                         int Angle,
                                                                         Byte[,] pcCompareBitmap,
                                                                         int piForegroundColor)
        {
            int LengthPerpendicular;
            bool PerpendicularRun;
            
            //Check how the length of this run compares to the perpendicular, and if the perpendicular
            //is longer. Remove this run of pixels
            LengthPerpendicular = 0;
            PerpendicularRun = true;

            while (PerpendicularRun && LengthPerpendicular < 32)
            {
                foreach (Point RunPoint in ToBeRemoved)
                {
                    if (!DoThinningTestPixel(RunPoint.X, 0, RunPoint.Y, LengthPerpendicular, Angle, pcCompareBitmap, piForegroundColor))
                    {
                        PerpendicularRun = false;
                    }
                }

                if (PerpendicularRun) LengthPerpendicular++;
            }

            return (LengthPerpendicular >= ToBeRemoved.Count);
        }

        /// <summary>
        /// This function checks if any pixels has an original gray level which is very close
        /// to the gray value of the character its body. If so than this run is assumed not to be noise.
        /// The body gray level is determined as the highest peak in the histogram below the threshold.
        /// </summary>
        /// <param name="ToBeRemoved"></param>
        /// <param name="pcCompareBitmap"></param>
        /// <returns></returns>
        private static bool DoThinningPruningTestRunAgainstGrayLevel(List<Point> ToBeRemoved,
                                                                     PageComponent Component,
                                                                     PageBase Image)
        {
            int BlackThreshold;
            int NumberBlackPixels;

            NumberBlackPixels = 0;
            BlackThreshold = Image.PeakBlackLevel + ((Image.Threshold - Image.PeakBlackLevel) / 4);
            
            foreach (Point Pixel in ToBeRemoved)
            {
                if (Component.CompareMatrix[Pixel.X, Pixel.Y] <= BlackThreshold) NumberBlackPixels++;
            }
            
            return NumberBlackPixels == 0; //< (ToBeRemoved.Count / 4);
        }
        
        private static bool DoThinningTestPruning2Start(int x, int y, Byte[,] CompareMatrix, int ForegroundColor, int Angle, int MaxDepth)
        {
            bool Result = false;

            //Pruning 1
            // o o 
            // o . 
            // x x 

            if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                !DoThinningTestPixel(x, -1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, -1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, 0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                DoThinningTestPixel(x, -1, y, 1, Angle, CompareMatrix, ForegroundColor) &&
                DoThinningTestPixel(x, 0, y, 1, Angle, CompareMatrix, ForegroundColor))
            {
                Result = true;

            }

            if (MaxDepth == 1) return Result;

            //Pruning 2
            // o o 
            // o . 
            // o x 
            // x x 

            if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                !Result && 
                !DoThinningTestPixel(x, -1, y, +0, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, -1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +2, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, -1, y, +2, Angle, CompareMatrix, ForegroundColor))
            {
                Result = true;

            }

            if (MaxDepth == 2) return Result;

            //Pruning 3
            // o o 
            // o . 
            // o x 
            // o x 
            // x x 

            if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                !Result &&
                !DoThinningTestPixel(x, -1, y, +0, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, -1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, -1, y, +2, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +2, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +3, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, -1, y, +3, Angle, CompareMatrix, ForegroundColor))
            {
                Result = true;

            } 
            
            return Result;
        }

        private static bool DoThinningTestPruning2Run(int x, int y, Byte[,] CompareMatrix, int ForegroundColor, int Angle)
        {
            bool Result = false;

            //Pruning 1
            // o  
            // x  
            // x  

            if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                !DoThinningTestPixel(x, 0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                DoThinningTestPixel(x, 0, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                DoThinningTestPixel(x, 0, y, 1, Angle, CompareMatrix, ForegroundColor))
            {
                Result = true;

            }

            return Result;
        }


        private static bool DoThinningTestPruning2End(int x, int y, Byte[,] CompareMatrix, int ForegroundColor, int Angle, int MaxDepth)
        {
            bool Result = false;

            //Pruning 1
            // o o
            // . o
            // x x

            if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                !DoThinningTestPixel(x, +0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, +0, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +1, y, +1, Angle, CompareMatrix, ForegroundColor))
            {
                Result = true;

            }

            if (MaxDepth == 1) return Result;

            //Pruning 2
            // o o
            // . o
            // x o
            // x x

            if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                !Result &&
                !DoThinningTestPixel(x, +1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, +0, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +2, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +1, y, +2, Angle, CompareMatrix, ForegroundColor))
            {
                Result = true;

            }

            if (MaxDepth == 2) return Result;

            //Pruning 3
            // o o
            // . o
            // x o
            // x o
            // x x

            if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                !Result &&
                !DoThinningTestPixel(x, +0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, +0, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                !DoThinningTestPixel(x, +1, y, +2, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +1, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +2, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +0, y, +3, Angle, CompareMatrix, ForegroundColor) &&
                 DoThinningTestPixel(x, +1, y, +3, Angle, CompareMatrix, ForegroundColor))
            {
                Result = true;

            }

            return Result;
        }
        
        private static bool DoThinningStep2cdTests(Byte[,] pbNeighbors) 
        {

	        if ((pbNeighbors[1,0]*pbNeighbors[2,1]*pbNeighbors[0,1] == 0) &&
		        (pbNeighbors[1,0]*pbNeighbors[1,2]*pbNeighbors[0,1] == 0))
		        return true;
	        else
		        return false;
        }

        private static bool DoThinningStep1cdTests(Byte[,] pbNeighbors) 
        {

	        if ((pbNeighbors[1,0]*pbNeighbors[2,1]*pbNeighbors[1,2] == 0) &&
		        (pbNeighbors[2,1]*pbNeighbors[1,2]*pbNeighbors[0,1] == 0))
		        return true;
	        else
		        return false;
        }

        private static bool DoThinningCheckTransitions(Byte[,] pbNeighbors) 
        {

	        int iTransitions=0;
	        if ((pbNeighbors[0,0]==1) && (pbNeighbors[1,0]==0)){ ++iTransitions;}
	        if ((pbNeighbors[1,0]==1) && (pbNeighbors[2,0]==0)){ ++iTransitions;}
	        if ((pbNeighbors[2,0]==1) && (pbNeighbors[2,1]==0)){ ++iTransitions;}
	        if ((pbNeighbors[2,1]==1) && (pbNeighbors[2,2]==0)){ ++iTransitions;}
	        if ((pbNeighbors[2,2]==1) && (pbNeighbors[1,2]==0)){ ++iTransitions;}
	        if ((pbNeighbors[1,2]==1) && (pbNeighbors[0,2]==0)){ ++iTransitions;}
	        if ((pbNeighbors[0,2]==1) && (pbNeighbors[0,1]==0)){ ++iTransitions;}
	        if ((pbNeighbors[0,1]==1) && (pbNeighbors[0,0]==0)){ ++iTransitions;}
	        if (iTransitions==1)
		        return true;
	        else
		        return false;
        }

        private static bool DoThinningSearchNeighbors(int x, int y, Byte[,] pcCompareBitmap, Byte[,] pbNeighbors, int piForegroundColor) 
        {
	        int ForeGroundNeighbor=0;

	        if ((pbNeighbors[0,0]=(Byte)DoThinningGetPixel(x-1, y-1, pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}
	        if ((pbNeighbors[1,0]=(Byte)DoThinningGetPixel(x  , y-1, pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}
	        if ((pbNeighbors[2,0]=(Byte)DoThinningGetPixel(x+1, y-1, pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}
	        if ((pbNeighbors[0,1]=(Byte)DoThinningGetPixel(x-1, y  , pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}
	        if ((pbNeighbors[2,1]=(Byte)DoThinningGetPixel(x+1, y  , pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}
	        if ((pbNeighbors[0,2]=(Byte)DoThinningGetPixel(x-1, y+1, pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}
	        if ((pbNeighbors[1,2]=(Byte)DoThinningGetPixel(x  , y+1, pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}
	        if ((pbNeighbors[2,2]=(Byte)DoThinningGetPixel(x+1, y+1, pcCompareBitmap, piForegroundColor)) == 1){ForeGroundNeighbor++;}

	        if ((ForeGroundNeighbor>=2) && (ForeGroundNeighbor<=6))
		        return true;
	        else
		        return false;
            }

        private static bool DoThinningCheckSuperfluous(int x, int y, Byte[,] CompareMatrix, int ForegroundColor)
        {
            bool Result = false;

            if (DoThinningGetPixel(x, y - 1, CompareMatrix, ForegroundColor) == 1 && 
                DoThinningGetPixel(x - 1, y, CompareMatrix, ForegroundColor) == 1 && 
                DoThinningGetPixel(x + 1, y + 1, CompareMatrix, ForegroundColor) != 1) Result = true;

            if (DoThinningGetPixel(x, y + 1, CompareMatrix, ForegroundColor) == 1 && DoThinningGetPixel(x - 1, y, CompareMatrix, ForegroundColor) == 1 && DoThinningGetPixel(x + 1, y - 1, CompareMatrix, ForegroundColor) != 1) Result = true;
            if (DoThinningGetPixel(x, y - 1, CompareMatrix, ForegroundColor) == 1 && DoThinningGetPixel(x + 1, y, CompareMatrix, ForegroundColor) == 1 && DoThinningGetPixel(x - 1, y + 1, CompareMatrix, ForegroundColor) != 1) Result = true;
            if (DoThinningGetPixel(x, y + 1, CompareMatrix, ForegroundColor) == 1 && DoThinningGetPixel(x + 1, y, CompareMatrix, ForegroundColor) == 1 && DoThinningGetPixel(x - 1, y - 1, CompareMatrix, ForegroundColor) != 1) Result = true; 

            return Result;
        }

        private static bool DoThinningTestPruning(int x, int y, Byte[,] CompareMatrix, int ForegroundColor)
        {
            bool Result = false;

            int Angle = 0;
            int Loop = 4;

            while (!Result && Loop > 0)
            {

                Loop--;

                //Pruning 1
                // o o o
                // o . o
                // o ? ?

                if (/*!DoThinningTestMB2Pixel(x, -1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                    !DoThinningTestPixel(x, -1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, -1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 1, y, 0, Angle, CompareMatrix, ForegroundColor))
                {
                    Result = true;

                }

                //Pruning 1
                // o o o
                // o . o
                // ? ? 0

                if (/*!DoThinningTestMB2Pixel(x, 1, y, +1, Angle, CompareMatrix, ForegroundColor) &&*/
                    !DoThinningTestPixel(x, -1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, -1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 1, y, 0, Angle, CompareMatrix, ForegroundColor))
                {
                    Result = true;

                }

                //Angle += Math.PI / 2;
                Angle++;
            }

            return Result;
        }
        
        private static bool DoThinningTest3(int x, int y, Byte[,] CompareMatrix, int ForegroundColor)
        {
            bool Result = true;

            int Angle = 0;
            int Loop = 2;

            while (Result && Loop > 0)
            {

                Loop--;

                //A1
                // o . o

                if (!DoThinningTestPixel(x, -1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 1, y, 0, Angle, CompareMatrix, ForegroundColor))
                {
                    Result = false;

                }

                //Angle += (int)Math.PI / 2;
                Angle++;
            }

            return Result;
        }    
            
            
        private static bool DoThinningTestMB2(int x, int y, Byte[,] CompareMatrix, int ForegroundColor)
        {
            bool Result = false;
            int Angle = 0;
            int Loop = 4;

            while (!Result && Loop > 0) {

                Loop--;
                
                //A1
                //       x
                // o . x x x
                //       x

                if (!DoThinningTestPixel(x, -1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 2, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 1, y, 1, Angle, CompareMatrix, ForegroundColor)) Result = true;

                //A2
                // o o 
                // o . x 
                //   x x x  
                //     x
                if (!DoThinningTestPixel(x, -1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, -1, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    !DoThinningTestPixel(x, 0, y, -1, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 1, y, 0, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 0, y, 1, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 1, y, 1, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 2, y, 1, Angle, CompareMatrix, ForegroundColor) &&
                    DoThinningTestPixel(x, 1, y, 2, Angle, CompareMatrix, ForegroundColor)) Result = true;

                //B
                // . o
                // o x
                if (Result)
                {
                    int AngleB = 0;

                    for (int i = 0; i < 4 && Result; i++)
                    {                        
                        if (!DoThinningTestPixel(x, 1, y, 0, AngleB, CompareMatrix, ForegroundColor) &&
                            !DoThinningTestPixel(x, 0, y, 1, AngleB, CompareMatrix, ForegroundColor) &&
                            DoThinningTestPixel(x, 1, y, 1, AngleB, CompareMatrix, ForegroundColor)) Result = false;

                        AngleB++;
                       // AngleB = (int)Math.PI / 2;
                    }
                }

                Angle++;
                //Angle += (int)0.5 * Math.PI;
            }

            return Result;
        }

        private static bool DoThinningTestPixel(int x, int deltax, int y, int deltay, int angle,
                                    Byte[,] pcCompareBitmap, int piForegroundColor)
        {
            bool Result = false;

            //int Cos = (int)Math.Cos(angle);
            //int Sin = (int)Math.Sin(angle);
            int Cos = CCos[angle];
            int Sin = CSin[angle];

            Result = (DoThinningGetPixel((int)(x + Cos * deltax + Sin * deltay),
                                         (int)(y - Sin * deltax + Cos * deltay),
                                         pcCompareBitmap, piForegroundColor) == 1);

            return Result;
        }

        /// <summary>
        /// This function checks if the requested pixel (x,y) has the foreground color
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="pcCompareBitmap"></param>
        /// <param name="piForegroundColor"></param>
        /// <returns>1 if the pixel is a foreground pixel</returns>
        private static int DoThinningGetPixel(int x, int y, Byte[,] pcCompareBitmap, int piForegroundColor) 
        {

            int iReturnValue;

            iReturnValue = 0; //by default return the pixel as background

            if (x>=0 && x<32 && y>=0 && y<32) 
            {

                if (pcCompareBitmap[x,y] != (Byte)0xff)//piForegroundColor) 
                {

                    iReturnValue = 1; //this pixel has the foreground color
                }
            }

            return iReturnValue;
        }

        #endregion

        #region "FindStrokes"

        public static int FindStrokes(Byte[,] cBitmap, int lCharacter) 
        {
            int Strokes = 0;

	        //Search for points of thinned image starting from the lower left corner
	        for (int lDistance=0; lDistance<32; lDistance++) 
            {

		        for (int lX=0; lX<lDistance; lX++) 
                {

			        if (cBitmap[lX, 31 - lDistance]==0x00) 
                    {

				        if (InvestigateStroke(lX, 31 - lDistance, cBitmap, lCharacter)) Strokes++;
				        }
			        }

		        for (int lY=31-lDistance; lY<32 ; lY++) 
                {

			        if (cBitmap[lDistance, lY]==0x00) 
                    {

				        if (InvestigateStroke(lDistance, lY, cBitmap, lCharacter)) Strokes++;
                    }
		        }
	        }

            //Clean the bitmap, remove all pixels not belonging to a stroke
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (cBitmap[x, y] == 0x00) cBitmap[x, y] = 0xFF;
                }
            }

            return Strokes;
        }

        private static bool InvestigateStroke(int lX, int lY, Byte[,] cBitmap, int lCharacter) 
        {

	        int lFirstDirection;
	        List <cDirection> oStroke;
            bool bStroke;

            oStroke = new List<cDirection>(0);
            bStroke = false;

	        lFirstDirection = 0;
	        lFirstDirection = FollowStroke(cBitmap, oStroke, lX, lY, lFirstDirection);

	        if (lFirstDirection!=-1) 
            {

		        lFirstDirection = (4 + lFirstDirection) & 7; 

		        FollowStroke(cBitmap, oStroke, lX, lY, lFirstDirection);
            }


	        //Optimize strokes, remove some 'noisy' pictures
	       // OptimizeStrokes(oStroke);

	        //place direction marks into bitmap
	        for (int lIndex=0; lIndex<oStroke.Count; lIndex++) 
            {
		        cBitmap[oStroke[lIndex].Pixel.X, oStroke[lIndex].Pixel.Y] = (Byte)(16 + 16 * (oStroke[lIndex].lDirection & 3));
    	    }
            bStroke = oStroke.Count > 4;

            //cBitmap[0, 0] = 128;
            //cBitmap[1, 0] = 16;
            //cBitmap[2, 0] = 32;
            //cBitmap[0, 1] = 112;
            //cBitmap[2, 1] = 48;
            //cBitmap[0, 2] = 96;
            //cBitmap[1, 2] = 80;
            //cBitmap[2, 2] = 64;

	        //SplitAndSaveStrokes(oStroke, lCharacter);

            return bStroke;
	    }

        private static int FollowStroke(Byte[,] cBitmap, List<cDirection> oStroke, int lX, int lY, int lDirection) 
        {
	        int lTestDirection;
	        int lCounter;
	        bool bContinueFollowStroke;
	        int[] lDeltaX = {0, 1, 1, 1, 0, -1, -1, -1};
	        int[] lDeltaY = {-1, -1, 0, 1, 1, 1, 0, -1};
	        int lFirstDirection;
	        cDirection oDirection;

	        bContinueFollowStroke = true;
	        lFirstDirection = -1;

	        do {

		        //find the next point on the stroke

		        bContinueFollowStroke = false;
		        lTestDirection = lDirection;
		        lCounter = 0;

		        do {

			        lTestDirection = (lDirection + lCounter) & 7;

			        bContinueFollowStroke = GetPixelBitmap(cBitmap, lX+lDeltaX[lTestDirection], lY+lDeltaY[lTestDirection])==0x00;

			        if (!bContinueFollowStroke) {

				        lTestDirection = (lDirection - lCounter) & 7;

				        bContinueFollowStroke = GetPixelBitmap(cBitmap, lX+lDeltaX[lTestDirection], lY+lDeltaY[lTestDirection])==0x00;
				        }

			        if (bContinueFollowStroke) {

		                cBitmap[lX, lY]=0x05;

				        if (lFirstDirection == -1) {

					        lFirstDirection = lTestDirection;
                        
                            oDirection = new cDirection();
                            oDirection.Pixel = new Point(lX, lY);
                            oDirection.lDirection = lTestDirection;

                            oStroke.Add(oDirection);
                        }				

				        lDirection = lTestDirection;
				        lX += lDeltaX[lTestDirection];
				        lY += lDeltaY[lTestDirection];

                        oDirection = new cDirection();
                        oDirection.Pixel = new Point(lX, lY);
                        oDirection.lDirection = lDirection;

				        oStroke.Add(oDirection);
				        }

			        lCounter++;

			        } while (!bContinueFollowStroke && lCounter<4);

		        } while (bContinueFollowStroke);

	        return lFirstDirection;
	        }

        private static void OptimizeStrokes(List<cDirection> oStroke) 
        {

	        //this function smoothes the strokes somewhat, it helps the identification
	        //of strokes when small irregularities are removed.

	        int lSize;
	        int lComplementTest;
            cDirection oDirection;

	        lSize = oStroke.Count;

	        for (int lIndex=0; lIndex<lSize; lIndex++) {


		        //pattern 1: two pixels are in the middle of two the same directions, and these two pixels are both
		        //           eachoters complement (ie, direction nw+ne, or nw+sw)

		        if (lIndex>0 && lIndex<lSize-2) {

			        if (oStroke[lIndex-1].lDirection == oStroke[lIndex+2].lDirection) {

				        lComplementTest = (oStroke[lIndex].lDirection+oStroke[lIndex+1].lDirection) & 7;

				        if (lComplementTest == 4 || lComplementTest==0) {

                            oDirection = oStroke[lIndex];
                            oDirection.lDirection = oStroke[lIndex-1].lDirection;
                            oStroke[lIndex] = oDirection;

                            oDirection = oStroke[lIndex+1];
                            oDirection.lDirection = oStroke[lIndex - 1].lDirection;
                            oStroke[lIndex+1] = oDirection;
					        }
				        }
			        }

		        //pattern 2: two pixels are in the middle of two the same directions, and these two pixels are both
		        //           eachoters complement (ie, direction nw+ne, or nw+sw)

		        if (lIndex>0 && lIndex<lSize-3) {

			        if (oStroke[lIndex-1].lDirection == oStroke[lIndex+3].lDirection) {

				        lComplementTest = (oStroke[lIndex].lDirection+oStroke[lIndex+2].lDirection) & 7;

				        if (lComplementTest == 4 || lComplementTest==0) {

                            oDirection = oStroke[lIndex];
                            oDirection.lDirection = oStroke[lIndex - 1].lDirection;
                            oStroke[lIndex] = oDirection;

                            oDirection = oStroke[lIndex+2];
                            oDirection.lDirection = oStroke[lIndex - 1].lDirection;
                            oStroke[lIndex+2] = oDirection;
					        }
				        }
			        }
		        }
	        }

        private static void SplitAndSaveStrokes(List<int> oStroke, int lCharacter) 
        {

	        List<float> oSlidingAverageUp = new List<float>(0);
            List<float> oSlidingAverageDown = new List<float>(0);
	        int lSize;
	        float lAverageUp, lAverageDown;

	        lSize = oStroke.Count;

	        //we only look at strokes which are at least 4 pixels in length.

	        //problem: irregular results when going from direction 0 to 7 and visa versa.
	        if (lSize>4) {

		        //fill the sliding average vectors
		        for (int lIndex=0; lIndex<lSize-4; lIndex++) {

			        lAverageUp = lAverageDown = 0;

			        for (int lIndex2=0; lIndex2<4; lIndex2++) {

				        lAverageUp+=oStroke[lIndex + lIndex2];
				        lAverageDown+=oStroke[lSize -lIndex - lIndex2 - 1];

				        }

			        oSlidingAverageUp.Add(lAverageUp / 4);
			        oSlidingAverageDown.Add(lAverageDown / 4);
			        }
		        }

	        //compare the two sliding average sequences, the parts that are similar point
	        //to a stroke.
	        lSize = oSlidingAverageUp.Count;
        }

        #endregion


        public static void CreateCompareMatrix(PageComponent Component)
        {
            PageImage tempImage;

            tempImage = new PageImage();
            CreateCompareMatrix(tempImage, Component);
        }
            
        public static void CreateCompareMatrix(PageBase Image, PageComponent Component)
        {
            ThinningPruningOnOriginalImage(Image, Component, 0, 1);

            CreateCompareMatrixWithoutPruning(Component);
        
        }

        public static void ThinningPruningOnOriginalImage(PageBase Image, PageComponent Component, int PruningAlgorithm, int MaxDepth)
        {
            Byte Pixel;

            if ((Component.Width < 32 && Component.Height < 32) &&
                    Component.Width * Component.Height < 1024)
            {
                //clean the matrix
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        Component.CompareMatrix[x, y] = 0xff;
                    }
                }

                //Set the original image in the matrix and do the pruning on it.
                for (int y = 0; y < Component.Height; y++)
                {

                    for (int x = 0; x < Component.Width; x++)
                    {

                        Pixel = Component.Bytes[x, y];

                        Component.CompareMatrix[x, y] = Pixel;
                    }
                }

                switch (PruningAlgorithm)
                {
                    case 1:
                        DoThinningPruning(Component.CompareMatrix, 0, 255);
                        break;

                    case 2:
                        DoThinningPruning2(Component.CompareMatrix, 0, 255);
                        break;

                    case 3:
                        DoThinningPruning3(Image, Component, 0, 255, MaxDepth);
                        break;

                    default:
                        DoThinningPruning3(Image, Component, 0, 255, MaxDepth);
                        break;
                }

                //set it back
                //Set the original image in the matrix and do the pruning on it.
                for (int y = 0; y < Component.Height; y++)
                {

                    for (int x = 0; x < Component.Width; x++)
                    {
                        Pixel = Component.CompareMatrix[x, y];
                        Component.Bytes[x, y] = Pixel;
                        Component.BinaryBytes[x, y] = (Pixel == 0xFF ? (byte)0xFF : (byte)0x00);
                    }
                }

                //check if we need to resize the component (empty rows on top and bottom?)
                Rectangle SizeComponent;

                SizeComponent = new Rectangle(0, 0, Component.Width, Component.Height);

                while (SizeComponent.Height > 0 &&
                       Component.NumberPixelsOnRow(Component.BinaryBytes, SizeComponent, 0, 0x00) == 0)
                {
                    SizeComponent.Y++;
                    SizeComponent.Height--;
                }

                while (SizeComponent.Height > 0 &&
                       Component.NumberPixelsOnRow(Component.BinaryBytes, SizeComponent, SizeComponent.Height - 1, 0x00) == 0)
                {
                    SizeComponent.Height--;
                }

                while (SizeComponent.Width > 0 &&
                       Component.NumberPixelsOnColumn(Component.BinaryBytes, SizeComponent, 0, 0x00) == 0)
                {
                    SizeComponent.X++;
                    SizeComponent.Width--;
                }

                while (SizeComponent.Width > 0 &&
                       Component.NumberPixelsOnColumn(Component.BinaryBytes, SizeComponent, SizeComponent.Width - 1, 0x00) == 0)
                {
                    SizeComponent.Width--;
                }

                if (SizeComponent.Width > 0 && SizeComponent.Height > 0)
                {
                    if (Component.Width != SizeComponent.Width || Component.Height != SizeComponent.Height)
                    {
                        PageComponent newComponent = Component.PartialCopy(SizeComponent);

                        SizeComponent = Component.Area;
                        SizeComponent.X += newComponent.Area.X;
                        SizeComponent.Y += newComponent.Area.Y;
                        SizeComponent.Width = newComponent.Width;
                        SizeComponent.Height = newComponent.Height;

                        Component.Area = SizeComponent;
                        Component.BinaryBytes = newComponent.BinaryBytes;
                        Component.Bytes = newComponent.Bytes;
                    }
                }
            }
        }

        public static void CreateCompareMatrixWithoutPruning(PageComponent Component)
        {
            CreateCompareMatrixWithoutPruning(Component, Component.BinaryBytes);
        }

        public static void CreateCompareMatrixWithoutPruning(PageComponent Component, byte[,] SourceBytes)
        {

            Point Source;
            Byte Pixel;

            Source = new Point();

            double RatioX;
            double RatioY;
            Rectangle ComponentRect;

            RatioX = ((double)32 / (double)Component.Width);
            RatioY = ((double)32 / (double)Component.Height);

            double Ratio = Math.Min(RatioX, RatioY);

            if (Math.Max(RatioX, RatioY) > Ratio * 2)
            {
                if (RatioX > RatioY) RatioX = 2 * RatioY;
                if (RatioY > RatioX) RatioY = 2 * RatioX;
            }

            ComponentRect = new Rectangle(0, 0, (int)(Component.Width * RatioX), (int)(Component.Height * RatioY));
            ComponentRect.X = (32 - ComponentRect.Width) / 2;
            ComponentRect.Y = (32 - ComponentRect.Height) / 2;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Component.CompareMatrix[x, y] = 0xFF;
                }
            }

            for (int y = ComponentRect.Top; y < ComponentRect.Bottom; y++)
            {
                Source.Y = (int)((y - ComponentRect.Top) / RatioY);

                for (int x = ComponentRect.Left; x < ComponentRect.Right; x++)
                {
                    Source.X = (int)((x - ComponentRect.Left) / RatioX);

                    Pixel = SourceBytes[Source.X, Source.Y];

                    Component.CompareMatrix[x, y] = Pixel;
                }
            }
        }
        
        private static Byte GetPixelBitmap(Byte[,] cBitmap, int lX, int lY) 
        {
            //get a pixel from the bitmap if it is an existing pixel (X in range 0..31
	        //and Y in range 0..31). If it is nog an existing pixel then return 0

	        Byte bReturn;

	        bReturn = 0xFF;

	        if (lX>=0 && lX<32 && lY>=0 && lY<32) {


		        bReturn = cBitmap[lX,lY];
		        }

	        return bReturn;
	    }

        /// <summary>
        /// This function computes the position of the give Component. The
        /// position is related to the sentence it is part of.
        /// </summary>
        /// <param name="Component"></param>
        private static void ComputePosition(PageComponent Component)
        {
            //Determine the top position
            if (Component.Sentence != null)
            {
                if (Component.Area.Top < Component.Sentence.TresholdAscentHeight)
                {
                    Component.Position.Ascent = true;
                }
                else
                {
                    if (Component.Area.Top < Component.Sentence.Center)
                    {
                        Component.Position.Height = true;
                    }
                    else
                    {
                        Component.Position.Center = true;
                    }
                }

            //Determine the bottom position
                if (Component.Area.Bottom > Component.Sentence.TresholdBaseDescent)
                {
                    Component.Position.Descent = true;
                }
                else
                {
                    if (Component.Area.Bottom > Component.Sentence.Center)
                    {
                        Component.Position.Base = true;
                    }
                    else
                    {
                        Component.Position.Center = true;
                    }
                }
            }
        }
    }
}
