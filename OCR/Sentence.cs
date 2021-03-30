using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OCR
{
    class Sentence
    {

        public List<PageComponent> Components;
        public int ID;

        private double m_AverageHeight = -1;
        private double m_AverageWidth = -1;
        private double m_AverageAngle = -1;
        private double m_Slope = -1;

        private int m_ThresholdAscentHeight = -1;
        private int m_Center = -1;
        private int m_ThresholdBaseDescent = -1;

        private Rectangle m_Area = new Rectangle(0,0,0,0);

        public Sentence()
        {
            ClearCache();
            Components = new List<PageComponent>(0);
        }

        public List<Point> BaseLine = new List<Point>();
        public List<Point> DescentLine = new List<Point>();

        public Point PositionAscent { get; set; }
        public Point PositionHeight { get; set; }
        public Point PositionCenter { get; set; }
        public Point PositionBase { get; set; }
        public Point PositionDescent { get; set; }


        /// <summary>
        /// This property contains the contents of the sentence 
        /// </summary>
        public String Content { get; set; }

        /// <summary>
        /// Adds a component to the sentence
        /// </summary>
        /// <param name="Component"></param>
        public void Add(PageComponent Component) 
        {
            ClearCache();

            Components.Add(Component);
        }

        /// <summary>
        /// Updates/replaces the given position in the sentence with a new component
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="Component"></param>
        public void Update(int Position, PageComponent Component) 
        {
            ClearCache();

            Components[Position] = Component;
        }
        
        /// <summary>
        /// Removes the component at the given position
        /// </summary>
        /// <param name="Position"></param>
        public void Delete(int Position) 
        {
            ClearCache();
  
            Components.RemoveAt(Position);
        }

        /// <summary>
        /// Inserts a component at the given position
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="Component"></param>
        public void Insert(int Position, PageComponent Component)
        {
            ClearCache();

            Components.Insert(Position, Component);           
        }
        
        /// <summary>
        /// Clear the private variables so that they will be recalculated when requested.
        /// </summary>
        private void ClearCache()
        {
            m_AverageHeight = -1;
            m_AverageWidth = -1;
            m_AverageAngle = -1;
            m_Slope = -1;
            m_Area = new Rectangle(0, 0, 0, 0);
            m_ThresholdAscentHeight = -1;
            m_ThresholdBaseDescent = -1;
            m_Center = -1;
        }

        /// <summary>
        /// The number of components in this sentence
        /// </summary>
        public int Count
        {
            get { return Components.Count; }
        }

        /// <summary>
        /// Get the component at the given position
        /// </summary>
        /// <param name="Position"></param>
        /// <returns></returns>
        public PageComponent Get(int Position)
        {
            return Components[Position];
        }

        /// <summary>
        /// This property contains the bounding box of the sentence
        /// </summary>
        public Rectangle Area
        {
            get
            {
                if (m_Area.Height == 0)
                {
                    foreach (PageComponent Component in Components)
                    {
                        if (m_Area.Height == 0)
                        {
                            m_Area = new Rectangle(Component.Area.Location, Component.Area.Size);
                        }
                        else
                        {
                            if (m_Area.X > Component.Area.X)
                            {
                                m_Area.Width = m_Area.Width + Math.Abs(m_Area.X - Component.Area.X);
                                m_Area.X = Component.Area.X;
                            }
                            if (m_Area.Y > Component.Area.Y)
                            {
                                m_Area.Height = m_Area.Height + Math.Abs(m_Area.Y - Component.Area.Y);
                                m_Area.Y = Component.Area.Y;
                            }
                            m_Area.Width = Math.Max(m_Area.Right, Component.Area.Right) - m_Area.X;
                            m_Area.Height = Math.Max(m_Area.Bottom, Component.Area.Bottom) - m_Area.Y;
                        }
                    }
                }

                return m_Area;
            }
        }

        /// <summary>
        /// This property contains the slope of this sentence
        /// </summary>
        public double Slope
        {
            get
            {
                if (m_Slope == -1)
                {
                    m_Slope = 0;
                    
                    if (Components.Count > 1)
                    {
                        Point PointFrom = Components.First().CenterPoint;
                        Point PointTo = Components.Last().CenterPoint;

                        double dx = (PointTo.X - PointFrom.X);
                        double dy = (PointTo.Y - PointFrom.Y);

                        if (dx != 0)
                        {
                            m_Slope = Math.Atan(dy / dx);
                        }
                        else
                        {
                            m_Slope = Math.PI / 2;
                        }
                    }

                    PositionCenter = new Point(Area.Left + Area.Width / 2, Area.Top + Area.Height / 2);
                   
                    List<Point> CenterPoints = new List<Point>(0);
                    foreach (PageComponent Component in Components)
                    {
                        CenterPoints.Add(Component.CenterPoint);
                    }
                    CenterPoints.Sort(CompareByPointY);

                    //remove the excesses (the first and last 10% and at least 10 characters
                    //remaining
                    int Removal = CenterPoints.Count / 10;

                    while (CenterPoints.Count > 10 && Removal > 0)
                    {
                        Removal--;

                        CenterPoints.RemoveAt(0);
                        CenterPoints.RemoveAt(CenterPoints.Count - 1);
                    }
                    
                    m_Slope = LineBestFitAngle(PositionCenter, m_Slope, CenterPoints);
                    PositionCenter = LineBestFitPoint(PositionCenter, m_Slope, CenterPoints);
                    PositionBaseDescentLine();
                }

                return m_Slope;
            }
        }

        public void PositionBaseDescentLine()
        {
            List<Point> BottomPoints = new List<Point>(0);

            //Place all bottom points in the list
            foreach (PageComponent Component in Components)
            {
                BottomPoints.Add(new Point(Component.CenterPoint.X, Component.Area.Bottom));
            }
            BottomPoints.Sort(CompareByPointY);
            Point newPoint;
            //Adjust the bottom with the predicted slope of the line
            for (int index = 0; index < BottomPoints.Count; index++)
            {
                newPoint = new Point(BottomPoints[index].X, BottomPoints[index].Y + (int)(Math.Sin(m_Slope) * (PositionCenter.X - BottomPoints[index].X)));
                BottomPoints[index] = newPoint;
            }

            BottomPoints.Sort(CompareByPointY);

            //Find the best points to position the base and descent line
            PositionBase = BottomPoints[BottomPoints.Count / 2];
            PositionDescent = BottomPoints[BottomPoints.Count / 2];

            int BestFit = 1000000000;
            int Position = BottomPoints.Count;
            int Fit;
            List<Point> BasePoints;
            List<Point> DescentPoints;

            while (Position > 1)
            {
                Position --;

                BasePoints = new List<Point>(0);
                DescentPoints = new List<Point>(0);

                for (int index = 0; index < Position; index++)
                {
                    BasePoints.Add(BottomPoints[index]);
                }
                for (int index = Position; index < BottomPoints.Count; index++)
                {
                    DescentPoints.Add(BottomPoints[index]);
                }

                Fit = LineBestFitQuadraticDistance(BasePoints[BasePoints.Count / 2], 0, BasePoints);
                Fit += LineBestFitQuadraticDistance(DescentPoints[DescentPoints.Count / 2], 0, DescentPoints);

                if (Fit < BestFit)
                {
                    BestFit = Fit;
                    PositionBase = BasePoints[BasePoints.Count / 2];
                    PositionDescent = DescentPoints[DescentPoints.Count / 2];
                }
            }
        }





        private static int CompareByPointY(Point Left, Point Right)
        {
            try
            {
                if (Left.Y > Right.Y) return 1;
                if (Left.Y < Right.Y) return -1;
            }
            catch (Exception)
            {
                return 0;
            }

            return 0;
        }

        /// <summary>
        /// Fit the points to the line and try to find the best fit by
        /// adjusting the line its position
        /// </summary>
        /// <param name="Point"></param>
        /// <param name="Angle"></param>
        /// <param name="Points"></param>
        /// <returns></returns>
        private Point LineBestFitPoint(Point Point, double Angle, List<Point> Points)
        {
            int BestFit;
            Point BestFitPoint;

            BestFit = LineBestFitQuadraticDistance(Point, Angle, Points);
            BestFitPoint = Point;

            //Continue to shift the line up and down to see if there is a better match
            //until there are no better matches anymore
            int Delta = 1;
            int Fit;
            bool Continue;
            Point FitPoint;

            do
            {
                Continue = false;

                FitPoint = new Point(Point.X, Point.Y + Delta);
                Fit = LineBestFitQuadraticDistance(Point, Angle, Points);

                if (Fit < BestFit)
                {
                    BestFit = Fit;
                    BestFitPoint = FitPoint;
                    Continue = true;
                }

                FitPoint = new Point(Point.X, Point.Y - Delta);
                Fit = LineBestFitQuadraticDistance(Point, Angle, Points);

                if (Fit < BestFit)
                {
                    BestFit = Fit;
                    BestFitPoint = FitPoint;
                    Continue = true;
                }

                Delta++;

            } while (Continue);

            return BestFitPoint;
        }

        /// <summary>
        /// Fit the points to the line and try to find the best fit by
        /// adjusting the line its position
        /// </summary>
        /// <param name="Point"></param>
        /// <param name="Angle"></param>
        /// <param name="Points"></param>
        /// <returns></returns>
        private double LineBestFitAngle(Point Point, double Angle, List<Point> Points)
        {
            int BestFit;
            double BestFitAngle;

            BestFit = LineBestFitQuadraticDistance(Point, Angle, Points);
            BestFitAngle = Angle;

            //Continue to rotate the line up and down to see if there is a better match
            //until there are no better matches anymore
            int Delta = 1;
            int Fit;
            bool Continue;
            double FitAngle;

            do
            {
                Continue = false;

                FitAngle = Angle + Delta * Math.PI / 180;
                Fit = LineBestFitQuadraticDistance(Point, FitAngle, Points);

                if (Fit <= BestFit)
                {
                    BestFit = Fit;
                    BestFitAngle = FitAngle;
                    Continue = true;
                }

                FitAngle = Angle - Delta * Math.PI / 180;
                Fit = LineBestFitQuadraticDistance(Point, FitAngle, Points);

                if (Fit <= BestFit)
                {
                    BestFit = Fit;
                    BestFitAngle = FitAngle;
                    Continue = true;
                }

                Delta++;

            } while (Continue);

            return BestFitAngle;
        }

        /// <summary>
        /// This function calculates the quadratic distance of all the points
        /// to the line
        /// </summary>
        /// <param name="Point"></param>
        /// <param name="Angle"></param>
        /// <param name="Points"></param>
        /// <returns></returns>
        private int LineBestFitQuadraticDistance(Point Point, double Angle, List<Point> Points)
        {
            int Result = 0;

            foreach (Point DistancePoint in Points)
            {
                Result += LineBestFitDistance(Point, Angle, DistancePoint) ^ 2;
            }
            
            return Result;
        }

        /// <summary>
        /// This function calculates the distance from the point to the line
        /// </summary>
        /// <param name="Point"></param>
        /// <param name="Angle"></param>
        /// <param name="DistanceTo"></param>
        /// <returns></returns>
        private int LineBestFitDistance(Point Point, double Angle, Point DistanceTo)
        {   
            int A = DistanceTo.X - Point.X;
            int B = DistanceTo.Y - Point.Y;
            int C = Point.X + 1000 - Point.X;
            int D = Point.Y + (int)(1000 * Math.Sin(Angle)) - Point.Y;

            int Result = (int)(Math.Abs(A * D - C * B) / Math.Sqrt(C * C + D * D));

            return Result;
        }
        
        /// <summary>
        /// Returns the average height of the components in the sentence
        /// </summary>
        /// <returns></returns>
        public double AverageHeight() 
        {
            if (m_AverageHeight == -1)
            {

                m_AverageHeight = 0;

                foreach (PageComponent Component in Components)
                {
                    m_AverageHeight += Component.Height;
                }

                if (Components.Count > 0)
                {
                    m_AverageHeight = m_AverageHeight / (double)Components.Count;
                }
                else 
                {
                    m_AverageHeight = 0;
                }
            }

        return m_AverageHeight;
        }

        /// <summary>
        /// Returns the average width of the components in the sentence
        /// </summary>
        /// <returns></returns>
        public double AverageWidth() 
        {
            if (m_AverageWidth == -1) {

                m_AverageWidth = 0;

                foreach (PageComponent Component in Components)
                {
                    m_AverageWidth += Component.Width;
                }

                if (Components.Count > 0)
                {
                    m_AverageWidth = m_AverageWidth / (double)Components.Count;
                }
                else 
                {
                    m_AverageWidth = 0;
                }
            }

            return m_AverageWidth;
        }

        /// <summary>
        /// Returns the average angle between the components in the sentence
        /// </summary>
        /// <returns></returns>
        public double AverageAngle() 
        {
            if (m_AverageAngle == -1)
            {
                if (Components.Count > 0) 
                {
                   m_AverageAngle = PageComponent.AngleBetweenComponents(Components.First(), Components.Last());
                }
            }

            //int PointerFrom, PointerTo, ComponentCount;

            //if (m_AverageAngle == -1) {

            //    ComponentCount = 0;
            //    m_AverageAngle = 0;

            //    PointerFrom = 0;
            //    PointerTo = 0;

            //    do
            //    {
            //        while (Components[PointerTo].Type == eRectangleType.eSpace && PointerTo < Components.Count)
            //        {
            //            PointerTo++;
            //        }

            //        if (PointerTo < Components.Count)
            //        {
            //            m_AverageAngle += PageComponent.AngleBetweenComponents(Components[PointerFrom], Components[PointerTo]);
            //            ComponentCount++;
            //        }

            //        PointerFrom = PointerTo;
            //        PointerTo++; 
                    
            //    } while (PointerTo < Components.Count);

            //    if (ComponentCount > 0) 
            //    {
            //        m_AverageAngle = m_AverageAngle / ComponentCount;
            //    }
            //}

            return m_AverageAngle;
        }

        /// <summary>
        /// This property contains the center position (y coordinate) of the sentence
        /// </summary>
        public int Center
        {
            get 
            {
                if (m_Center == -1) ComputeLineTresholds();

                return m_Center;
            }
        }

        /// <summary>
        /// This property contains the treshold (y-coordinate) between
        /// the ascent and height line of the sentence
        /// </summary>
        public int TresholdAscentHeight
        {
            get
            {
                if (m_ThresholdAscentHeight == -1) ComputeLineTresholds();

                return m_ThresholdAscentHeight;
            }
        }

        /// <summary>
        /// This property contains the treshold (y-coordinate) between
        /// the base and the descent line of the sentence
        /// </summary>
        public int TresholdBaseDescent
        {
            get
            {
                if (m_ThresholdBaseDescent == -1) ComputeLineTresholds();

                return m_ThresholdBaseDescent;
            }
        }

        /// <summary>
        /// This function orders the component so they are in a perfect sequence from left to right
        /// </summary>
        public void SortComponents()
        {
            Components.Sort(delegate(PageComponent p1, PageComponent p2) { return p1.Area.Left.CompareTo(p2.Area.Left); });

            ClearCache();
        }

        /// <summary>
        /// This function computes the line thresholds to be used
        /// in filling the Position property of a PageComponent.
        /// </summary>
        private void ComputeLineTresholds()
        {
            //The center of the sentence
            m_Center = Area.Bottom - Area.Top;

            //The tresholds between the lines of a sentence. So is the
            //distance between the Descent and the Baseline in the arial
            //font about 22% of the maximum height. The treshold is chosen
            //to be in the middel (11% of the height of the sentence).
            m_ThresholdAscentHeight = Area.Top + (int)(0.11 * Area.Height);
            m_ThresholdBaseDescent = Area.Bottom - (int)(0.11 * Area.Height);


            DescentLine.Add(new Point(Area.X, Area.Bottom));
            BaseLine.Add(new Point(Area.X, Area.Bottom - Area.Height / 10));

            int DistanceBase, DistanceDescent;

            foreach (PageComponent Component in Components)
            {
                if (Component.Type == ePageComponentType.eCharacter)
                {
                    //baseline or descent, to which is it closest?
                    DistanceBase = Math.Abs(BaseLine.Last().Y - Component.Area.Bottom);
                    DistanceDescent = Math.Abs(DescentLine.Last().Y - Component.Area.Bottom);

                    if (DistanceDescent > DistanceBase)
                    {
                        BaseLine.Add(new Point(Component.Area.X, Component.Area.Bottom));
                    }
                    else
                    {
                        DescentLine.Add(new Point(Component.Area.X, Component.Area.Bottom));
                    }
                }
            }
        }
    }
}
