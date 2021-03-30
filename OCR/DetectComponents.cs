using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;

namespace OCR
{
    class DetectComponents : WorkThread
    {
        public DetectComponents() { }

        public PageImage Image;
        static object ImageWriteLocker = new object();

        public void Execute() {

            Console.WriteLine("Execute " + this.GetType().ToString());
           
            WorkPackage WorkPackage;
            int AreaHeight = Image.Area.Height / ThreadCount;

            //Reset the ID counter in PageComponent.
            PageComponent.newID = 0;

            //Create the workpackages
            for (int i = 0; i < ThreadCount; i++)
            {
                WorkPackage = new WorkPackage();
                WorkPackage.Method = ExecuteActivity;
                WorkPackage.Image = Image;
                WorkPackage.Assignment = (object)new Rectangle(Image.Area.Left, 
                                                        Image.Area.Top + AreaHeight * i, 
                                                        Image.Area.Width,
                                                        AreaHeight);
                WorkPackage.Parameter = (object)(Byte)(i+1);
                RunPackage(WorkPackage);
                }

            WaitForWorkDone(this.GetType().ToString());

            //Cleanup components
            CleanUpComponents(Image);
            

            OCR.Statistics.AddCounter("Detected components", Image.Components.Count);
        }

        public static void ExecuteActivity(object Parameter)
        {
            WorkPackage WorkPackage;
            Rectangle Assignment;
            PageComponent newComponent;

            WorkPackage = (WorkPackage)Parameter;
            Assignment = (Rectangle)WorkPackage.Assignment;

            int y = Assignment.Top;
            int x = Assignment.Left;
            int pointer;
            List<Point> Pixels = new List<Point>(0);

            while (y < Assignment.Bottom) 
            {
                x = Assignment.Left + ((y & 2) / 2);

                // we are using a mesh, therefor
                // we use y&1 to differentiate the starting point of each row.
                // the mesh looks like:
                // +-+-+-+-+
                // ---------
                // -+-+-+-+-
                // ---------
                // +-+-+-+-+
                // ---------
                // -+-+-+-+-
                // we check every other pixel, that way we can check half of the image
                // and still find all the component we want to find, except for lines
                // at exactly 45 degrees and 1 pixel wide. But at this point we find
                // those irrelevant;

                pointer = x + y * WorkPackage.Image.Stride;

                while (x < Assignment.Right)
                {

                    if (WorkPackage.Image.BinaryBytes[x, y]==0) {

                        //We found a new pixel with the
                        lock (WorkPackage.Image)
                        {
                            newComponent = new PageComponent();
                        }
                        newComponent.Area = new Rectangle(x, y, 1, 1);

                        Pixels.Clear();

                        if (GenerateConnectedComponent(WorkPackage, newComponent, Pixels))
                        {
                            
                            if (newComponent.Area.Height < WorkPackage.Image.Height * 0.9 &&
                                newComponent.Area.Width < WorkPackage.Image.Width * 0.9) {

                                    newComponent.PixelCount = Pixels.Count;

                                    GenerateBitmap(WorkPackage.Image, newComponent, Pixels); 
                                    
                                    lock(WorkPackage.Image)
                                    {
                                        WorkPackage.Image.Components.Add(newComponent);
                                    }
                                }
                            }
                        }

                    x += 2;
                    pointer += 2;
                    }

                y += 2;
                }
            SignalWorkDone();
        }

        private static bool GenerateConnectedComponent(WorkPackage WorkPackage, PageComponent newComponent, List<Point> Pixels)
        {
            //this functies growes the newComponent in such manner that it
            //surrounds connected pixels.

            //the function returns true if there are more than 8 connected pixels present.
            //The number of 8 comes from the article bu L.A.Fletcher and R.Kasturi,
            // 'A Robust Algorithm for Text String Separation from Mixed Text/Graphics Images'

            //in 'A word extraction algorithm for machine-printed documents using a
            //    3D neighborhood graph algorithm' a number of 6 pixels was mentioned
            //but for now the number of 8 seems to work allright.

            int pointer = 0;
            int Index=0;
            Rectangle newArea;
            Point Pixel = newComponent.Area.Location;
            Point TopLeft = new Point(Pixel.X, Pixel.Y);
            Point BottomRight = new Point(Pixel.X, Pixel.Y);

            try
            {
                Pixels.Add(TopLeft);

                do
                {
                    //get pixel from the queue
                    Pixel = Pixels[Index];

                    //determine if we need to increase the area of the component.
                    if (Pixel.X < TopLeft.X) TopLeft.X = Pixel.X;
                    if (Pixel.Y < TopLeft.Y) TopLeft.Y = Pixel.Y;
                    if (Pixel.X >= BottomRight.X) BottomRight.X = Pixel.X+1;
                    if (Pixel.Y >= BottomRight.Y) BottomRight.Y = Pixel.Y+1;

                    //calculate pointer and change color of pixel to make sure
                    //it will not be detected for other connected components
                    pointer = Pixel.X + WorkPackage.Image.Stride * Pixel.Y;

                    lock (ImageWriteLocker)
                    {
                        if (WorkPackage.Image.BinaryBytes[Pixel.X, Pixel.Y] == (Byte)WorkPackage.Parameter ||
                            WorkPackage.Image.BinaryBytes[Pixel.X, Pixel.Y] == 0)
                        {
                            WorkPackage.Image.BinaryBytes[Pixel.X, Pixel.Y] = 64; //pixel is set
                        }
                        else
                        {
                            //detected a conflict with another thread, roll back!
                            while (Index > 0)
                            {
                                Index--;
                                Pixel = Pixels[Index];
                                pointer = Pixel.X + WorkPackage.Image.Stride * Pixel.Y;
                                WorkPackage.Image.BinaryBytes[Pixel.X, Pixel.Y] = 0x00;
                            }

                            //throw an exception, leave this as quick as possible
                            throw new ApplicationException("Conflict with another thread");
                        }
                    }

                    //look around and add interesting pixels to queue
                    GCC_CheckNeighbour(WorkPackage, Pixel, -1,  0, Pixels);
                    GCC_CheckNeighbour(WorkPackage, Pixel,  0, -1, Pixels);
                    GCC_CheckNeighbour(WorkPackage, Pixel,  1,  0, Pixels);
                    GCC_CheckNeighbour(WorkPackage, Pixel,  0,  1, Pixels);
                    GCC_CheckNeighbour(WorkPackage, Pixel, -1,  1, Pixels);
                    GCC_CheckNeighbour(WorkPackage, Pixel,  1, -1, Pixels);
                    GCC_CheckNeighbour(WorkPackage, Pixel, -1, -1, Pixels);
                    GCC_CheckNeighbour(WorkPackage, Pixel,  1,  1, Pixels);

                    Index++;

                } while (Index < Pixels.Count);

                //Set the area of the component
                newArea = new Rectangle(TopLeft.X, TopLeft.Y, BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);
                newComponent.Area = newArea;
            }
            catch(ApplicationException exp) 
            {               
                Console.WriteLine("Exception caught: " + exp.Message);
                Console.WriteLine("   in: " + exp.StackTrace);
                return false;
            }

            //return true if there are more than 8 connected pixels
            return (Pixels.Count >= 8);
        }

        private static void GCC_CheckNeighbour(WorkPackage WorkPackage, Point Pixel, int dx, int dy, List<Point> Pixels)
        {
            int pointer = 0;
            Point NextPixel = new Point(Pixel.X + dx, Pixel.Y + dy);        
                
           //check if pixel is withing boundaries
            if (WorkPackage.Image.Area.Contains(NextPixel)) 
            {
                pointer = NextPixel.X + (NextPixel.Y * WorkPackage.Image.Stride);

                //check color code of pixel
                lock (ImageWriteLocker)
                {
                    if (WorkPackage.Image.BinaryBytes[NextPixel.X, NextPixel.Y] == 0x00)
                    {
                        //set the pixel to status (discovered)
                        WorkPackage.Image.BinaryBytes[NextPixel.X, NextPixel.Y] = (Byte)WorkPackage.Parameter;
                        //add pixel to cache
                        Pixels.Add(NextPixel);
                    }
                }
            }
        }

        private static void GenerateBitmap(PageImage Image, PageComponent Component, List<Point> Pixels)
        {
            //Generate a bitmap of the connected component. The bitmap contains only
            //pixels of the connected component and no other ones. The bitmaps has the
            //size of the bounding rectangle of the connected component

            int Size;

            Size = Component.Stride * Component.Height;

            Component.Bytes = new Byte[Component.Width, Component.Height];
            Component.BinaryBytes = new Byte[Component.Width, Component.Height];

            for (int x = 0; x < Component.Width; x++)
            {
                for (int y = 0; y < Component.Height; y++)
                {
                    Component.Bytes[x, y] = 0xFF;
                    Component.BinaryBytes[x, y] = 0xFF;
                }
            }

            foreach (Point Pixel in Pixels)
            {
                Component.Bytes[Pixel.X - Component.Area.X, Pixel.Y - Component.Area.Y] = Image.Bytes[Pixel.X, Pixel.Y];
                Component.BinaryBytes[Pixel.X - Component.Area.X, Pixel.Y - Component.Area.Y] = 0x00;
            }

            Pixels.Clear();


            //int Source;
            //int Destination;
            //    int y = Component.Area.Height;
        //    int x = Component.Area.Width;

        //    while (y > 0)
        //    {
        //        y--;

        //        Source = (Component.Area.X + x) + (Component.Area.Y + y) * Image.Stride;
        //        Destination = x + y * Component.Stride;

        //        while (x > 0)
        //        {
        //            x--;
        //            Destination--;
        //            Source--;

        //            if (Image.ImageBytes[Source] != 0xFF)
        //            {
        //                Component.ComponentBytes[Destination] = 0x00;
        //            }
        //            else
        //            {
        //                Component.ComponentBytes[Destination] = 0xFF;
        //            }
        //        }
        //    }
        }

        /// <summary>
        /// This function remove components for which it is highly unlikely that they are character
        /// </summary>
        /// <param name="Image"></param>
        private void CleanUpComponents(PageImage Image)
        {
            double AverageHeight;
            double AverageWidth;

            //step 1: calculate average height and width of all components
            AverageHeight = 0;
            AverageWidth = 0;

            foreach (PageComponent Component in Image.Components)
            {
                AverageWidth += Component.Width;
                AverageHeight += Component.Height;
            }

            AverageWidth /= Image.Components.Count;
            AverageHeight /= Image.Components.Count;
            
            //step 2: remove relatively large components. Probably images or noise
            int index = Image.Components.Count;

            while (index > 0)
            {
                index--;

                if (Image.Components[index].Width > AverageWidth * 10 ||
                    Image.Components[index].Height > AverageHeight * 10)
                {
                    Image.Components.RemoveAt(index);
                }
            }
             index = Image.Components.Count;

        }
    }
}
