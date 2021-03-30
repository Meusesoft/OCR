using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

enum eRectangleType {eUnknownRect = -1, eCharacter = 0, eImageRect = 1, eInvertedCharacter = 2, eSplitCharacter = 3};

namespace OCR
{
    class PageComponent
    {

        public PageComponent() 
        {
            Points = new List<Point>(0);
            Components = new List<PageComponent>(0);
            Content = "";
            Type = eRectangleType.eUnknownRect;
            CharacterSuggestions = new List<string>(0);
            VectorPoints = new List<Point>(0);

        }
            
        public Boolean Add(PageComponent Child) {

            Components.Add(Child);
            return true;
            }

        public Boolean Delete(int index) 
        {

            Components.RemoveAt(index);
            return true;
        }

        public int Count() 
        {

            return Components.Count;
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

        public Point GetCenterPoint()
        {
            Point Result;

            Result = Area.Location;
            Result.X += Area.Width / 2;
            Result.Y += Area.Height / 2;

            return Result;
        }

        public List<Point> VectorPoints;
        public List<String> CharacterSuggestions;


        public long DistanceChildren(int iIndex1, int iIndex2) 
        {
        
            long lRetValue;
            PageComponent Child;
            Point Pos1;
            Point Pos2;

            double root1, root2;

            Child = Components[iIndex1];
            Pos1 = Child.GetCenterPoint();

            Child = Components[iIndex2];
            Pos2 = Child.GetCenterPoint();

            root1 = System.Math.Pow(System.Math.Abs(Pos1.X - Pos2.X), 2);
            root2 = System.Math.Pow(System.Math.Abs(Pos1.Y - Pos2.Y), 2);
            lRetValue = (long)System.Math.Sqrt(root1 + root2);

            return lRetValue;
        }

        public long DistanceBetweenChildren(int iIndex1, int iIndex2) 
        {
        
            long lRetValue;
            PageComponent ChildFrom;
            PageComponent ChildTo;

            int xDelta, yDelta;
            int xMoved, yMoved;
            double Direction;
            Point PointFrom, PointTo, Position, Origin;

            lRetValue = 0;
            Direction = 0;

            ChildFrom = Components[iIndex1];
            ChildTo = Components[iIndex2];

            PointFrom = ChildFrom.GetCenterPoint();
            PointTo   = ChildTo.GetCenterPoint();
            Origin = PointFrom;

            lRetValue = DistanceChildren(iIndex1, iIndex2) + 1;

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

                while (ChildFrom.CoordinateInMe(Position)) {

                    lRetValue--;

                    xMoved += xDelta;
                    Position.X += xDelta;
                    Position.Y = (Origin.Y + (int)(xMoved * Direction));
                    }

                Position = PointTo;
                xMoved = 0;
                xDelta = xDelta * -1; //we walk to the different side

                while (ChildTo.CoordinateInMe(Position)) 
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

                while (ChildFrom.CoordinateInMe(Position)) 
                {

                    lRetValue--;

                    yMoved += yDelta;
                    Position.Y += yDelta;
                    Position.X = Origin.X + (int)(yMoved * Direction);
                }

                Position = PointTo;
                yMoved = 0;
                yDelta = yDelta * -1; //we walk to the different side

                while (ChildTo.CoordinateInMe(Position))
                {
                    lRetValue--;

                    yMoved += yDelta;
                    Position.Y += yDelta;
                    Position.X = Origin.X + (int)(yMoved * Direction);
                }
            }

        return lRetValue;        
        }

        public double AverageAreaChildren()
        {
            double Result;

            Result = 0;
            
            foreach (PageComponent Child in Components) 
            {
                
                Result += Child.Area.Width * Child.Area.Height;
            }
            
            Result = Result / Components.Count;

            return Result;
        }

        public double AngleBetweenChildren(int index1, int index2)
        {
            PageComponent oFromRectangle;
            PageComponent oToRectangle;
            double dx, dy, dInput, dAngle;
            int xTo,xFrom,yTo,yFrom;

            oFromRectangle = GetChild(index1);
            oToRectangle = GetChild(index2);

            xTo = oToRectangle.Area.X;
            xFrom = oFromRectangle.Area.X;
            yTo = oToRectangle.Area.Y;
            yFrom = oFromRectangle.Area.Y;

                if (xTo <= xFrom && (xTo+oToRectangle.Area.Width) > xFrom) {
                    xTo = xFrom;
                    }
                if (xFrom <= xTo && (xFrom + oFromRectangle.Area.Width) > xTo)
                {
                    xFrom = xTo;
                    }
                if (yTo <= yFrom && (yTo + oToRectangle.Area.Height) > yFrom)
                {
                    yTo = yFrom;
                    }
                if (yFrom <= yTo && (yFrom + oFromRectangle.Area.Height) > yTo)
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




        public long MergeRectangles(long index1, long index2) {

            return 0;
        }

        public Rectangle Area;
        public int  NumberPixels = 0;
        public int  SentenceNumber = 0;
        public int  AreaNumber = 0;
        public int  MergeWithRectangle = 0;
        public int  Gaps = 0;
        public bool TouchingCharacters = false;
        public float AnglePrevious = 0;
        public float AngleNext = 0;

        public List<Point> Points;

        public eRectangleType Type;
        public String Content;
        public Byte[] ComponentBytes;

      //  public HGLOBAL hBitmap;

        private List<PageComponent> Components;
    }
}
