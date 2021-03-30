using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;


/// <summary>
/// This enumaration contains the possible values for a rectangle type
/// </summary>
enum ePageComponentType
{
    eUnknownRect = -1,
    eCharacter = 0,
    eImageRect = 1,
    eInvertedCharacter = 2,
    eSplitCharacter = 3,
    eSpace = 4
};

/// <summary>
/// This enumaration contains the possible values for a pixel type
/// </summary>
enum ePixelType
{
    Background = 0,
    Regular = 1,
    End = 2,
    Junction = 3
};

namespace OCR
{
    class RecognitionResult
    {
        public string Content;
        public double Probability;
    }    
    
    class PageComponent : PageBase
    {

        public PageComponent() 
        {
            Components = new List<PageComponent>(0);
            RecognitionResults = new List<RecognitionResult>(0);
            m_Stride = 0;
            m_Area = new Rectangle();
            Type = ePageComponentType.eUnknownRect;
            CompareMatrix = new Byte[32,32];
            StrokeMatrix = new Byte[32, 32];
            PixelTypeMatrix = new ePixelType[32,32];
            PixelTypeProjectionEndpoint = new int[3, 3];
            PixelTypeProjectionJunction = new int[3, 3];
            Position = new ShapePosition();

            ID = newID;
            newID++;
        }

        public static int newID;
            
        public Boolean Add(PageComponent Child) {

            Components.Add(Child);
            return true;
            }

        public Boolean Delete(int index) 
        {

            Components.RemoveAt(index);
            return true;
        }

        public int Count
        {
            get { return Components.Count; }
        }

        public PageComponent GetChild(int index) 
        {

            return Components[index];
        }

        public Boolean CoordinateInMe(Point pValue) 
        {

            return Area.Contains(pValue);
        }

        public Boolean RectangleOverlap(Rectangle pRectangle) 
        {

            return Area.IntersectsWith(pRectangle);
        }

        public Point CenterPoint
        {
            get
            {
                Point Result;

                Result = Area.Location;
                Result.X += Area.Width / 2;
                Result.Y += Area.Height / 2;

                return Result;
            }
        }

        public Sentence Sentence { get; set; }

        public double Density
        {
            get { return (double)PixelCount / AreaSize; }
        }


        public bool CouldBeConnected
        {
            get
            {
                bool Result;

                Result = false;

                if (RecognitionResults.Count > 0)
                {
                    if (RecognitionResults[0].Probability < 0.2) Result = true;
                }

                if (!Result)
                {
                    foreach (RecognitionResult item in RecognitionResults)
                    {
                        if (item.Content == "connected")
                        {
                            Result = true;
                        }
                    }
                }

                return Result;
            }
        }

        /// <summary>
        /// This property contains the content of this component.
        /// </summary>
        public String Content
        {
            get
            {
                switch (Type)
                {
                    case ePageComponentType.eSpace:
                    {
                        return " ";
                    }

                    case ePageComponentType.eCharacter:
                    case ePageComponentType.eSplitCharacter:
                    {
                        if (RecognitionResults.Count > 0)
                        {
                            return RecognitionResults[0].Content;
                        }
                        else
                        {
                            return "";
                        }
                    }

                    default:
                    {
                        return "";
                    }
                }
            }
        }

        public double ContentProbability 
        {
            get
            {
                double Result = 0;

                if (RecognitionResults.Count > 0) 
                {
                    Result = RecognitionResults[0].Probability;
                }

                return Result;
            }
        }

        public ShapePosition Position { get; set; }

        /// <summary>
        /// Add a recognition result to the list of results. The list is orderd by probability
        /// </summary>
        /// <param name="Probability"></param>
        /// <param name="Value"></param>
        public void AddRecognitionResult(double Probability, String Value)
        {
            RecognitionResult newResult;
            newResult = null;
            int iNewPosition = RecognitionResults.Count;

            if (Probability > 0.01)
            {
                newResult = new RecognitionResult();
                newResult.Content = Value;
                newResult.Probability = Probability;

                while (iNewPosition > 0 && RecognitionResults[iNewPosition - 1].Probability < Probability)
                {
                    iNewPosition--;
                }

                if (iNewPosition < 4)
                {
                    RecognitionResults.Insert(iNewPosition, newResult);
                }
            }
        }

        /// <summary>
        /// This function calculates the distance between the center points of two components
        /// </summary>
        /// <param name="From"></param>
        /// <param name="To"></param>
        /// <returns></returns>
        public static long Distance(PageComponent From, PageComponent To) 
        {
        
            long lRetValue;
            Point Pos1;
            Point Pos2;

            double root1, root2;

            Pos1 = From.CenterPoint;
            Pos2 = To.CenterPoint;

            root1 = System.Math.Pow(System.Math.Abs(Pos1.X - Pos2.X), 2);
            root2 = System.Math.Pow(System.Math.Abs(Pos1.Y - Pos2.Y), 2);
            lRetValue = (long)System.Math.Sqrt(root1 + root2);

            return lRetValue;
        }

        /// <summary>
        /// This function calculates the distance between the bounding boxes of two components
        /// </summary>
        /// <param name="ChildFrom"></param>
        /// <param name="ChildTo"></param>
        /// <returns></returns>
        public static int DistanceBetweenComponents(PageComponent From, PageComponent To) 
        {
        
            int lRetValue;

            int xDelta, yDelta;
            int xMoved, yMoved;
            double Direction;
            Point PointFrom, PointTo, Position, Origin;

            lRetValue = 0;
            Direction = 0;

            PointFrom = From.CenterPoint;
            PointTo = To.CenterPoint;
            Origin = PointFrom;

            lRetValue = (int)Distance(From, To) + 1;

            if (System.Math.Abs(PointTo.X - PointFrom.X) > System.Math.Abs(PointTo.Y - PointFrom.Y))
            {

                if (PointTo.X != PointFrom.X)
                { //just to be sure we don't get a division by zero
                    Direction = (double)(PointTo.Y - PointFrom.Y) / (double)(PointTo.X - PointFrom.X);
                    }

                Position = PointFrom;

                if (PointFrom.X>PointTo.X) {
                    xDelta = -1;
                    }
                else {
                    xDelta = 1;
                    }
                xMoved = 0;

                while (From.CoordinateInMe(Position)) {

                    lRetValue--;

                    xMoved += xDelta;
                    Position.X += xDelta;
                    Position.Y = (Origin.Y + (int)(xMoved * Direction));
                    }

                Position = PointTo;
                xMoved = 0;
                xDelta = xDelta * -1; //we walk to the different side

                while (To.CoordinateInMe(Position)) 
                    {

                    lRetValue--;

                    xMoved += xDelta;
                    Position.X += xDelta;
                    Position.Y = (Origin.Y + (int)(xMoved * Direction));
                    }
                }
            else {
                if (PointTo.Y != PointFrom.Y) { //just to be sure we don't get a division by zero
                    Direction = (double)(PointTo.X - PointFrom.X) / (double)(PointTo.Y - PointFrom.Y);
                    }

                Position = PointFrom;
                yDelta = (PointFrom.Y>PointTo.Y) ? -1 : 1;
                yMoved = 0;

                while (From.CoordinateInMe(Position)) 
                {

                    lRetValue--;

                    yMoved += yDelta;
                    Position.Y += yDelta;
                    Position.X = Origin.X + (int)(yMoved * Direction);
                }

                Position = PointTo;
                yMoved = 0;
                yDelta = yDelta * -1; //we walk to the different side

                while (To.CoordinateInMe(Position))
                {
                    lRetValue--;

                    yMoved += yDelta;
                    Position.Y += yDelta;
                    Position.X = Origin.X + (int)(yMoved * Direction);
                }
            }

        return lRetValue;        
        }


        /// <summary>
        /// This function calculates the angle between two component.
        /// </summary>
        /// <param name="From"></param>
        /// <param name="To"></param>
        /// <returns></returns>
        public static double AngleBetweenComponents(PageComponent From, PageComponent To)
        {
            double dx, dy, dInput, dAngle;
            int xTo,xFrom,yTo,yFrom;

            xTo = To.Area.X;
            xFrom = From.Area.X;
            yTo = To.Area.Y;
            yFrom = From.Area.Y;

                if (xTo <= xFrom && (xTo+To.Area.Width) > xFrom) {
                    xTo = xFrom;
                    }
                if (xFrom <= xTo && (xFrom + From.Area.Width) > xTo)
                {
                    xFrom = xTo;
                    }
                if (yTo <= yFrom && (yTo + To.Area.Height) > yFrom)
                {
                    yTo = yFrom;
                    }
                if (yFrom <= yTo && (yFrom + From.Area.Height) > yTo)
                {
                    yFrom = yTo;
                    }


                dx = System.Math.Abs(xTo - xFrom);
                dy = System.Math.Abs(yTo - yFrom);

                if (dx!=0) {
                    dInput = dy / dx;

                    dAngle = ((System.Math.Atan(dInput) * 360) / (2 * System.Math.PI));
                    }
                else {
                    if (yTo!=yFrom) {
                        dAngle = 90;
                        }
                    else {
                        dAngle = 0;
                        }
                    }
            return dAngle;
        }

        /// <summary>
        /// This function checks if this component is recognised as a connected component, meaning that
        /// it may consist of multiple characters. 
        /// Rules
        /// 1. Recognised as connected
        /// 2. Low probability in recognition
        /// </summary>
        public void CheckAndRepairConnected(ShapeNet ShapeNet)
        {
            if (CouldBeConnected)
            {
                RecogniseComponent.NumberConnectedComponents++;

                if (DebugTrace.DebugTrace.ApplySplitOnConnectedComponents)
                {
                    Split(ShapeNet);
                }
            }
        }


        /// <summary>
        /// This functions splits a character in multiple components. Used for splitting connected characters
        /// </summary>
        public void Split(ShapeNet ShapeNet)
        {
            List<int> SplitLines;

            //Step 1: Get the position for possible splits
            SplitLines = SplitDeterminePositions();

            if (SplitLines.Count == 0) return;

            RecognitionResults.Clear();
            
            //Step 2: Find the combination of the best (highest scores) recognised components
            List<PageComponent> Components = new List<PageComponent>(0);
            PageComponent newComponent;
            PageComponent prevComponent;
            Rectangle SplitArea;
            int start, end;

            SplitLines.Insert(0, 0);
            SplitLines.Add(Width-1);

            start = 0;
            end = 1;

            while (end < SplitLines.Count)
            {
                SplitArea = new Rectangle(SplitLines[start] + 1, 0, SplitLines[end] - SplitLines[start] - 2, Height);

                if (SplitArea.Width > 0 && SplitArea.Height > 0)
                {

                    while (NumberPixelsOnRow(BinaryBytes, SplitArea, 0, 0) == 0)
                    {
                        SplitArea.Y = SplitArea.Y + 1;
                        SplitArea.Height = SplitArea.Height - 1;
                    }

                    while (NumberPixelsOnRow(BinaryBytes, SplitArea, SplitArea.Height - 1, 0) == 0)
                    {
                        SplitArea.Height = SplitArea.Height - 1;
                    }

                    newComponent = PartialCopy(SplitArea);

                    ExtractFeatures.ExecuteExtractFeatures(newComponent, false);
                    RecogniseComponent.RecogniseWithoutConnectedRepair(ShapeNet, newComponent);

                    if (Components.Count > 0 && end - start > 1)
                    {
                        prevComponent = Components.Last();

                        if (prevComponent.ContentProbability < newComponent.ContentProbability && newComponent.Content != "connected" && newComponent.Content != "garbage")
                        {
                            Components.Remove(prevComponent);
                            Components.Add(newComponent);
                        }
                        else
                        {
                            start = end - 1;
                            end--;
                        }
                    }
                    else
                    {
                        Components.Add(newComponent);
                    }
                }
                end++;
            }

            //Add the new recognition result
            RecognitionResult newResult;

            newResult = new RecognitionResult();
            newResult.Content = "";
            newResult.Probability = 0;

            foreach (PageComponent Component in Components) 
            {
                newResult.Content += Component.Content;
                newResult.Probability += Component.ContentProbability;
            }

            newResult.Probability = newResult.Probability / Components.Count;

            RecognitionResults.Add(newResult);

            //Save a copy of the image to the disc
            if (DebugTrace.DebugTrace.TraceFeatures)
            {
                String ComponentID = "000000" + ID;
                ComponentID = ComponentID.Substring(ComponentID.Length - 6);

                foreach (int SplitLine in SplitLines)
                {
                    int pointer = SplitLine;

                    for (int y = 0; y < Height; y++)
                    {
                        if (BinaryBytes[SplitLine, y] == 0xFF) BinaryBytes[SplitLine, y] = 0x10;
                        pointer += Stride;
                    }
                }

                Bitmap Bitmap = DebugTrace.DebugTrace.CreateBitmapFromByteArray(BinaryBytes, new Size(Width, Height));
                String Filename = DebugTrace.DebugTrace.TraceFeatureFolder + "image_" + ComponentID + "_split.bmp";
                Bitmap.Save(Filename);
            }
        }

        //public void Split(ShapeNet ShapeNet)
        //{
        //    List<int> SplitLines;

        //    //Step 1: Get the position for possible splits
        //    SplitLines = SplitDeterminePositions();

        //    if (SplitLines.Count == 0) return;

        //    //Step 2: Find the combination of the best (highest scores) recognised components
        //    PageComponent Component1, Component2;
        //    double newProbabilty;
        //    Rectangle SplitArea;

        //    RecognitionResults.Clear();

        //    foreach (int SplitLine in SplitLines)
        //    {
        //        SplitArea = new Rectangle(0, 0, SplitLine, Height);

        //        while (NumberPixelsOnRow(SplitArea, 0, 0) == 0)
        //        {
        //            SplitArea.Y = SplitArea.Y + 1;
        //            SplitArea.Height = SplitArea.Height - 1;
        //        }

        //        while (NumberPixelsOnRow(SplitArea, SplitArea.Height - 1, 0) == 0)
        //        {
        //            SplitArea.Height = SplitArea.Height - 1;
        //        }

        //        Component1 = PartialCopy(SplitArea);

        //        ExtractFeatures.ExecuteExtractFeatures(Component1);
        //        RecogniseComponent.Recognise(ShapeNet, Component1);


        //        SplitArea = new Rectangle(SplitLine + 1, 0, Width - SplitLine - 1, Height);

        //        while (NumberPixelsOnRow(SplitArea, 0, 0) == 0)
        //        {
        //            SplitArea.Y = SplitArea.Y + 1;
        //            SplitArea.Height = SplitArea.Height - 1;
        //        }

        //        while (NumberPixelsOnRow(SplitArea, SplitArea.Height - 1, 0) == 0)
        //        {
        //            SplitArea.Height = SplitArea.Height - 1;
        //        }

        //        Component2 = PartialCopy(SplitArea);

        //        ExtractFeatures.ExecuteExtractFeatures(Component2);
        //        RecogniseComponent.Recognise(ShapeNet, Component2);

        //        if (Component1.RecognitionResults.Count > 0 && Component2.RecognitionResults.Count > 0)
        //        {
        //            if (Component1.RecognitionResults[0].Content != "connected" && Component2.RecognitionResults[0].Content != "connected")
        //            {
        //                newProbabilty = Component1.RecognitionResults[0].Probability * Component2.RecognitionResults[0].Probability;

        //                if (RecognitionResults.Count == 0)
        //                {
        //                    RecognitionResult newResult;

        //                    newResult = new RecognitionResult();
        //                    newResult.Content = "";
        //                    newResult.Probability = 0;

        //                    RecognitionResults.Add(newResult);
        //                }

        //                if (RecognitionResults[0].Probability < newProbabilty)
        //                {
        //                    RecognitionResults[0].Content = Component1.Content + Component2.Content;
        //                    RecognitionResults[0].Probability = newProbabilty;
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Determines possible splitting position based on minima at the top and maxima at the bottom.
        /// </summary>
        /// <returns></returns>
        private List<int> SplitDeterminePositions()
        {
            int index;
            int PreviousTop, PreviousBottom, CurrentValue;
            bool ToMinimum, ToMaximum;

            //Step 1: find possible split lines
            List<int> SplitLines = new List<int>(0);

            index = Area.Width;
            PreviousTop = NumberBackgroundPixelsTop(index - 1, 255);
            PreviousBottom = NumberBackgroundPixelsBottom(index - 1, 255);
            ToMinimum = false;
            ToMaximum = false;

            while (index > 0)
            {
                index--;

                //Search for a minimum at the top
                CurrentValue = NumberBackgroundPixelsTop(index, 255);
                if (ToMinimum && CurrentValue < PreviousTop)
                {
                    SplitLines.Insert(0, index + 1);
                    ToMinimum = false;
                    PreviousTop = CurrentValue;
                }
                if (!ToMinimum && CurrentValue > PreviousTop) ToMinimum = true;
                PreviousTop = CurrentValue;

                //Search for a maximum at the bottom
                CurrentValue = NumberBackgroundPixelsBottom(index, 255);
                if (ToMaximum && CurrentValue < PreviousBottom)
                {
                    SplitLines.Insert(0, index + 1);
                    ToMaximum = false;
                    PreviousBottom = CurrentValue;
                }
                if (!ToMaximum && CurrentValue > PreviousBottom) ToMaximum = true;
                PreviousBottom = CurrentValue;
            }

            //step 2: clean up by combining adjacent splitlines
            index = SplitLines.Count;

            while (index > 1)
            {
                index--;

                if (SplitLines[index] - SplitLines[index - 1] < 3)
                {
                    SplitLines[index - 1] = (SplitLines[index] + SplitLines[index - 1]) / 2;
                    SplitLines.RemoveAt(index);
                }
            }
            
            return SplitLines;
        }


        /// <summary>
        /// This function couns the number of pixels in the area on the requested row
        /// </summary>
        /// <param name="Area"></param>
        /// <param name="Row"></param>
        /// <param name="ForegroundColor"></param>
        /// <returns></returns>
        public int NumberPixelsOnRow(byte[,] Source, Rectangle Area, int Row, int ForegroundColor)
        {
            int NumberPixels = 0;
            int Count = 0;
            int y = Area.Top + Row;

            while (Count < Area.Width)
            {
                if (Source[Area.Left + Count, y] != 0xFF) NumberPixels++;
                Count++;
            }

            return NumberPixels;
        }
        /// <summary>
        /// This function couns the number of pixels in the area on the requested column
        /// </summary>
        /// <param name="Area"></param>
        /// <param name="Row"></param>
        /// <param name="ForegroundColor"></param>
        /// <returns></returns>
        public int NumberPixelsOnColumn(byte[,] Source, Rectangle Area, int Column, int ForegroundColor)
        {
            int NumberPixels = 0;
            int x = (Area.Left + Column);
            int Count = 0;

            while (Count < Area.Height)
            {
                if (Source[x, Area.Top + Count] != 0xFF) NumberPixels++;
                Count++;
            }

            return NumberPixels;
        }

        /// <summary>
        /// This function counts the number of background pixels from the top in the requested column
        /// </summary>
        /// <param name="Column"></param>
        /// <returns></returns>
        private int NumberBackgroundPixelsTop(int Column, int BackgroundColor)
        {
            int NumberPixels = 0;
            int y = 0;

            while (y < Height && BinaryBytes[Column, y] == BackgroundColor)
            {
                NumberPixels++;
                y++;
            }

            return NumberPixels;
        }

        /// <summary>
        /// This function counts the number of background pixels from the bottom up in the requested column
        /// </summary>
        /// <param name="Column"></param>
        /// <returns></returns>
        private int NumberBackgroundPixelsBottom(int Column, int BackgroundColor)
        {
            int NumberPixels = 0;
            int y = Area.Height - 1;
            
            while (y >= 0 && BinaryBytes[Column, y] == BackgroundColor)
            {
                NumberPixels++;
                y--;
            }

            return NumberPixels;
        }


        /// <summary>
        /// This function merges this Component with another component.
        /// </summary>
        /// <param name="MergeWith"></param>
        public PageComponent PartialCopy(Rectangle newArea)
        {
            //Create the new component
            PageComponent newComponent;

            newComponent = new PageComponent();
            newComponent.Area = newArea;
            newComponent.Bytes = new Byte[newComponent.Width, newComponent.Height];
            newComponent.BinaryBytes = new Byte[newComponent.Width, newComponent.Height];
            newComponent.Type = ePageComponentType.eSplitCharacter;

            //Clear the memory block
            for (int x = 0; x < newComponent.Width; x++)
            {
                for (int y = 0; y < newComponent.Height; y++)
                {
                    newComponent.Bytes[x, y] = 0xFF;
                    newComponent.BinaryBytes[x, y] = 0xFF;
                }
            }

            //Copy the memory block of this component in the new one
            for (int y = newArea.Top; y < newArea.Bottom; y++)
            {
                for (int x = 0; x < newArea.Width; x++)
                {
                    newComponent.Bytes[x, y - newArea.Top] = Bytes[newArea.X + x, y];
                    newComponent.BinaryBytes[x, y - newArea.Top] = BinaryBytes[newArea.X + x, y];
                    if (BinaryBytes[newArea.X + x, y] !=0xFF) newComponent.PixelCount++;
                }
            }

            return newComponent;
        }



        /// <summary>
        /// This function merges this Component with another component.
        /// </summary>
        /// <param name="MergeWith"></param>
        public void Merge(PageComponent MergeWith) 
        {
            Rectangle newArea;
            Byte[,] newComponentBytes;
            Byte[,] newComponentBinaryBytes;
            int newStride;

            try
            {
                //Calculate the new area of the merged component
                newArea = new Rectangle();

                newArea.X = Math.Min(Area.X, MergeWith.Area.X);
                newArea.Y = Math.Min(Area.Y, MergeWith.Area.Y);
                newArea.Width = Math.Max(Area.Right, MergeWith.Area.Right) - newArea.X;
                newArea.Height = Math.Max(Area.Bottom, MergeWith.Area.Bottom) - newArea.Y;

                //Calculate the size of the memory block which contains the pixels
                newStride = CalculateStride(newArea.Width);

                //Allocate the memory block
                newComponentBytes = new Byte[newArea.Width, newArea.Height];
                newComponentBinaryBytes = new Byte[newArea.Width, newArea.Height];

                //Clear the memory block
                int index = newComponentBytes.Length;
                
                for (int x = 0; x < newArea.Width; x++)
                {
                    for (int y = 0; y < newArea.Height; y++)
                    {
                        newComponentBytes[x, y] = 0xFF;
                        newComponentBinaryBytes[x, y] = 0xFF;
                    }
                }

                //Copy the memory block of this component in the new one
                int PointerFrom;
                int PointerTo;

                for (int y = 0; y < Area.Height; y++)
                {
                    PointerFrom = y * m_Stride;
                    PointerTo = (Area.X - newArea.X) + (y + Area.Y - newArea.Y) * newStride;

                    for (int x = 0; x < Area.Width; x++)
                    {
                        newComponentBytes[(x + Area.X - newArea.X), (y + Area.Y - newArea.Y)] = Bytes[x, y];
                        newComponentBinaryBytes[(x + Area.X - newArea.X), (y + Area.Y - newArea.Y)] = BinaryBytes[x, y];
                    }
                }

                //Copy the memory block of the merge component in the new one
                for (int y = 0; y < MergeWith.Area.Height; y++)
                {
                    for (int x = 0; x < MergeWith.Area.Width; x++)
                    {
                        newComponentBytes[x + MergeWith.Area.X - newArea.X, y + MergeWith.Area.Y - newArea.Y] = MergeWith.Bytes[x, y];
                        newComponentBinaryBytes[x + MergeWith.Area.X - newArea.X, y + MergeWith.Area.Y - newArea.Y] = MergeWith.BinaryBytes[x, y];
                    }
                }

                //Update the properties of this component;
                Area = newArea;
                BinaryBytes = newComponentBinaryBytes;
                Bytes = newComponentBytes;

                PixelCount += MergeWith.PixelCount;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message);
                Console.WriteLine("  In: " + e.StackTrace);
            }
        }

        /// <summary>
        /// This function loads a bitmap from the filesystem into the component
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        public bool LoadBitmap(String Filename)
        {
            bool Result = false;
            Byte[] ImageBuffer;

            try
            {
                System.Drawing.Bitmap Bitmap = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(Filename);
                Area = new System.Drawing.Rectangle(0, 0, Bitmap.Width, Bitmap.Height);
                ImageBuffer = new Byte[Stride * Height];

                System.Drawing.Imaging.BitmapData BitmapData = Bitmap.LockBits(Area,
                                                               System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                               System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                System.Runtime.InteropServices.Marshal.Copy(BitmapData.Scan0, ImageBuffer, 0, Stride * Height);

                Bitmap.UnlockBits(BitmapData);

                //Copy the imagebuffer into the componentbytes
                Bytes = new Byte[Width, Height];
                BinaryBytes = new Byte[Width, Height];

                int Pointer;

                for (int y = 0; y < Height; y++)
                {
                    Pointer = y * Stride;

                    for (int x = 0; x < Width; x++)
                    {
                        Bytes[x, y] = ImageBuffer[Pointer + x];
                        BinaryBytes[x, y] = (ImageBuffer[Pointer + x] == 0xFF ? (Byte)0xFF : (Byte)0x00);
                    }
                }

                //Count the number of pixels
                PixelCount = 0;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (BinaryBytes[x, y] == 0x00) PixelCount++;
                    }
                }

                //Determine a possible threshold
                BuildHistogram();
                int PossibleThreshold;
                PossibleThreshold = 0;

                for (int index = 0; index < 255; index++)
                {
                    if (Histogram[index] > 0) PossibleThreshold = index;
                }

                Threshold = PossibleThreshold;


                //Return the result
                Result = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message);
                Console.WriteLine("  In: " + e.StackTrace);

                Result = false;
            }

            return Result;
        }



       // public Rectangle m_Area;
        public int PixelCount = 0;
        public int  DetectionAreaIndex = 0;
        public int  Gaps = 0;
        public int  Strokes = 0;
        public ePageComponentType Type;
        public Byte[,] CompareMatrix;
        public Byte[,] StrokeMatrix;
        public ePixelType[,] PixelTypeMatrix;
        public int[,] PixelTypeProjectionEndpoint;
        public int[,] PixelTypeProjectionJunction;
        public int[] lPixelProjectionX = new int[32];
        public int[] lPixelProjectionY = new int[32];
        public int[] lStrokeDirectionX = new int[32];
        public int[] lStrokeDirectionY = new int[32];
        public int[,] lStrokeMatrixNW = new int[2, 2];
        public int[,] lStrokeMatrixSW = new int[2, 2];

        private int m_Stride;
        public int ID;

        public List<PageComponent> Components;
        public List<RecognitionResult> RecognitionResults;
    }
}

