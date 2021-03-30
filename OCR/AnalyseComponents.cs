using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCR
{
    class AnalyseComponents : WorkThread
    {
        public PageImage Image;
        public int ComponentTypeChanged;
        private static double ComponentAverageArea;

        public void Execute() {

            WorkPackage WorkPackage;
            int[] Assignment;

            Console.WriteLine("Execute " + this.GetType().ToString());

            ComponentTypeChanged = 0;
            ComponentAverageArea = Image.AverageAreaComponents();

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

            int TypeCharacter = 0;
            int TypeImage = 0;
            int TypeInverted = 0;

            foreach (PageComponent ChildComponent in Image.Components)
            {
                if (ChildComponent.Type == ePageComponentType.eCharacter) TypeCharacter++;
                if (ChildComponent.Type == ePageComponentType.eImageRect) TypeImage++;
                if (ChildComponent.Type == ePageComponentType.eInvertedCharacter) TypeInverted++;
            }

            OCR.Statistics.AddCounter("Component Type Character", TypeCharacter);
            OCR.Statistics.AddCounter("Component Type Image", TypeImage);
            OCR.Statistics.AddCounter("Component Type Inverted", TypeInverted);
        }
 
        public static void ExecuteActivity(object Parameter)
        {
            WorkPackage WorkPackage = (WorkPackage)Parameter;
            int[] Assignment = (int[])WorkPackage.Assignment;

            for (int index = Assignment[0]; 
                 index < WorkPackage.Image.Components.Count; 
                 index += Assignment[1])
            {
                AnalyseComponentType(WorkPackage.Image.Components[index]);
            }

            SignalWorkDone();
        }

        public static void AnalyseComponentType(PageComponent Component)
        {
            //see 'A word extraction algorithm for machine-printed documents using
            //     a 3D neighborhood graph model' by H.Park, S.Ok, Y. Yu and H.CHo, 2001
            //     International Journal on Document Analysis and Recognition

            Component.Type = ePageComponentType.eCharacter;

            if ((Component.AreaSize) > (ComponentAverageArea * 25)) 
                {
                Component.Type = ePageComponentType.eImageRect;
                }

            return;

            //const double cL0 = 0.8;
            //const double cD0 = 0.7;
            //const double cC1 = -1.09;
            //const double cC2 = 2.8;
            
            ////step 1, compare the area of the rectangle
            // //with the average rectangle size
            // if ((Component.AreaSize()) <= ComponentAverageArea) 
            // {
            //    Component.Type = eRectangleType.eCharacter;
            // }
            // else 
            // {
            //    //we have a rather large rectangle, proceed to step 2
            //    //checking of elongation.
            //     double Elong = (double)System.Math.Min(Component.Width, Component.Height) / (double)System.Math.Max(Component.Width, Component.Height);

            //    if (Elong <= cL0) {
            //        //Step 3, compare density. Pictures normally have larger
            //        //density than characters.
            //        if (Component.Density >= cD0) 
            //        {
            //            Component.Type = eRectangleType.eImageRect;
            //        }
            //        else 
            //        {
            //            if (cC1 * CalculateRowVariance(Component) + cC2 <= CalculateColumnVariance(Component)) 
            //            {
            //                Component.Type = eRectangleType.eInvertedCharacter;
            //            }
            //            else 
            //            {
            //                Component.Type = eRectangleType.eImageRect;
            //            }
            //        }
            //    }
            //    else 
            //    {
            //        if (cC1 * CalculateRowVariance(Component) + cC2 <= CalculateColumnVariance(Component)) 
            //        {
            //            Component.Type = eRectangleType.eCharacter;
            //        }
            //        else 
            //        {
            //            Component.Type = eRectangleType.eImageRect;
            //        }
            //    }
            //}
        }

        private static double CalculateRowVariance(PageComponent Component) 
        {

            List<int> RunsPerRow = new List<int>();
            int Runs;
            int Pointer;
            Byte CurrentRun;
            double Average;
            double Variance;

            //Count number of pixels per row
            int y = Component.Height;
            int x = 0;
            
            while (y>0)
            {
                y--;

                Runs = 1;
                x = Component.Width;
                Pointer = x + y*Component.Stride;
                CurrentRun = Component.BinaryBytes[x-1, y];

                while (x>0)
                {
                    x--;
                    Pointer--;

                    if (CurrentRun != Component.BinaryBytes[x, y])
                    {
                        Runs++;
                        CurrentRun = Component.BinaryBytes[x, y];
                    }
                }

                RunsPerRow.Add(Runs);
            }

            //calculate average runs
            Average = 0;
            int i;

            i = RunsPerRow.Count-1;
            while (i>0)
            {
                i--;

                Average += System.Math.Abs(RunsPerRow[i+1] - RunsPerRow[i]);
            }
            Average = Average / (RunsPerRow.Count-1);

            //calculate variance;
            Variance = 0;
            i = RunsPerRow.Count-1;
            while (i>0)
            {
                i--;

                Variance += System.Math.Pow((System.Math.Abs(RunsPerRow[i+1] - RunsPerRow[i]) - Average), 2);
            }
            Variance = Variance / (RunsPerRow.Count-1);

            return Variance;
            }

        private static double CalculateColumnVariance(PageComponent Component)
        {

            List<int> RunsPerRow = new List<int>();
            int Runs;
            int Pointer;
            Byte CurrentRun;
            double Average;
            double Variance;

            //Count number of pixels per column
            int y = 0;
            int x = Component.Width;

            while (x > 0)
            {
                x--;

                Runs = 1;
                y = Component.Height;
                Pointer = x + y * Component.Stride;
                CurrentRun = Component.BinaryBytes[x, y-1];

                while (y > 0)
                {
                    y--;
                    Pointer-=Component.Stride;

                    if (CurrentRun != Component.BinaryBytes[x, y])
                    {
                        Runs++;
                        CurrentRun = Component.BinaryBytes[x, y];
                    }
                }

                RunsPerRow.Add(Runs);
            }

            //calculate average runs
            Average = 0;
            int i;

            i = RunsPerRow.Count - 1;
            while (i > 0)
            {
                i--;

                Average += System.Math.Abs(RunsPerRow[i + 1] - RunsPerRow[i]);
            }
            Average = Average / (RunsPerRow.Count - 1);

            //calculate variance;
            Variance = 0;
            i = RunsPerRow.Count - 1;
            while (i > 0)
            {
                i--;

                Variance += System.Math.Pow((System.Math.Abs(RunsPerRow[i + 1] - RunsPerRow[i]) - Average), 2);
            }
            Variance = Variance / (RunsPerRow.Count - 1);

            return Variance;
        }
    }
}
