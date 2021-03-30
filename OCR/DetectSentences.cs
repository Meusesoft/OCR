using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;


namespace OCR
{
    class DetectSentences : WorkThread
    {
        public class DetectionArea 
        {
            /// <summary>
            /// The box of this detection area
            /// </summary>
            public Rectangle Area;
            
            /// <summary>
            /// The components in this detection area
            /// </summary>
            public List<PageComponent> Components;

            public DetectionArea()
            {
                Area = new Rectangle();
                Components = new List<PageComponent>(0);
            }
        }
        
        /// <summary>
        /// The image to process
        /// </summary>
        public PageImage Image;

        /// <summary>
        /// The number of detection areas on the x-axis
        /// </summary>
        static int NumberAreasX;

        /// <summary>
        /// A detection area helps to minimize the search for neighbour components
        /// </summary>
        private List<DetectionArea> m_Areas;

        /// <summary>This function launches multiple threads to execute the algorithm
        /// for detecting sentences
        /// </summary>
        public void Execute()
        {
            //int[] Assignment;
            
            Console.WriteLine("Execute " + this.GetType().ToString());

            CreateAreas();

            for (int i = 0; i < 1/*ThreadCount*/ ; i++) 
            {
                //At the moment we only start one thread for this part
                //The algorithm doesn't multithread very easily.
                
                WorkPackage WorkPackage = new WorkPackage();
                WorkPackage.Image = Image;
                WorkPackage.Method = ExecuteActivity;
                WorkPackage.Parameter = (object)m_Areas;
                RunPackage(WorkPackage);
            }
            
            WaitForWorkDone(this.GetType().ToString());

            //Clean up sentences
            CleanUpSentences(Image);

            //Add Statistics
            OCR.Statistics.AddCounter("Detected sentences", Image.Sentences.Count);
        }

        /// <summary>
        /// This function is the execution of a single thread of detecting sentences
        /// </summary>
        /// <param name="Parameter"></param>
        public static void ExecuteActivity(object Parameter)
        {
            WorkPackage WorkPackage;

            WorkPackage = (WorkPackage)Parameter;

            //Find sentences with areas
            FindSentences(WorkPackage.Image, WorkPackage.Parameter);

            //Uniform the height;
            //CalculateBoundingBox(WorkPackage.Image);

            //Add spaces to sentences
            AddSpacesToSentences(WorkPackage.Image);

           
            //Signal we are done
            SignalWorkDone();        
        }

        /// <summary>
        /// This function creates detection areas. It will creates as many areas as needed to have an 
        /// average of 20 components per area.
        /// </summary>
        public void CreateAreas() {

            //this function divides the image into areas so that each area contains
            //on average 20 rectangles. This helps to speed up the process of finding
            //the closest rectangle.

            m_Areas = new List<DetectionArea>(10);

            DetectionArea Area;
            Point Center;

            int      nDividerLinesX, nDividerLinesY;
            int      nAreas;
            int      AreaWidth, AreaHeight;
            int      AreaNumber;

            //calculate the number of dividerlines necessary to make up the desired
            //number of 20 rectangles per area
            nAreas =  Image.Components.Count / 20;

            if (nAreas<1) nAreas = 1;

            //determine the desired size of the areas.
            nDividerLinesX = 0;
            nDividerLinesY = 0;

            while (((nDividerLinesX+1)*(nDividerLinesY+1)) < nAreas) {

                nDividerLinesX++;
                nDividerLinesY = (nDividerLinesX * Image.Height) / Image.Width;
                }

            NumberAreasX = nDividerLinesX+1;
            int NumberAreasY = nDividerLinesY+1;

            AreaWidth = Image.Width / NumberAreasX;
            AreaHeight = Image.Height / NumberAreasY;

            if (AreaWidth<=0) throw new ApplicationException("DetectSentences: AreaWidth<=0");
            if (AreaHeight<=0) throw new ApplicationException("DetectSentences: AreaHeight<=0");

            NumberAreasX++;
            NumberAreasY++;

            //create the areas
            for (int Y=0; Y<NumberAreasY; Y++) {

                for (int X=0; X<NumberAreasX; X++) {

                    Area = new DetectionArea();

                    Area.Area.X = X * AreaWidth;
                    Area.Area.Y = Y * AreaHeight;

                    if (X == nDividerLinesX) 
                    {
                        Area.Area.Width = Image.Width - X * AreaWidth;
                    }
                    else 
                    {
                        Area.Area.Width = AreaWidth;
                     }

                    if (X == nDividerLinesX) 
                    {
                        Area.Area.Height = Image.Height - Y * AreaHeight;
                    }
                    else 
                    {
                        Area.Area.Height = AreaHeight;
                    }

                    m_Areas.Add(Area);
                }
            }

            nAreas = NumberAreasX * NumberAreasY;

            //assign an area to every rectangle
            foreach (PageComponent Component in Image.Components)
            {
                Center = Component.CenterPoint;

                AreaNumber = (Center.X / AreaWidth);
                AreaNumber = AreaNumber + (NumberAreasX * (Center.Y / AreaHeight));

                if (AreaNumber >= m_Areas.Count) throw new ApplicationException("DetectSentences: Invalid Area number");
                Component.DetectionAreaIndex = AreaNumber;

                //add the component to the area
                m_Areas[AreaNumber].Components.Add(Component);
                }
            }

        /// <summary>
        /// This function starts the search for a sentence. It takes the
        /// first free rectangle (not belonging to a sentence) and takes
        /// it as the first step in a growth process in which more rectangles
        /// are added to it
        /// </summary>
        /// <param name="Image">The image to process</param>
        /// <param name="DetectionAreas">The list of detection areas</param>
        public static void FindSentences(PageImage Image, Object DetectionAreas) 
        {

            Sentence      newSentence;
            PageComponent Component;
            List<DetectionArea> Areas = (List<DetectionArea>)DetectionAreas;

            foreach (DetectionArea Area in Areas)
            {

                while (Area.Components.Count > 0)
                {
                    Component = Area.Components.First();
                    Area.Components.Remove(Component);

                    if (Component.Type == ePageComponentType.eCharacter)
                    {

                        //start a new sentence
                        newSentence = new Sentence();

                        newSentence.Add(Component);

                        GrowSentence(newSentence, (List<DetectionArea>)Areas);

                        Image.Sentences.Add(newSentence);
                    }
                }
            }
        }

        /// <summary>
        ///This function adds components the the current rectangle. There are
        ///two growth paths, to the left and to the right.
        /// </summary>
        /// <param name="Sentence"></param>
        /// <param name="m_Areas"></param>
        public static void GrowSentence(Sentence Sentence, List<DetectionArea> m_Areas) 
        {
            bool Growing;

            Growing = true;

            while (Growing) 
            {
                Growing = false;

                //Grow at the end
                while (AddNearestComponentAtTheEnd(Sentence, m_Areas, 0)) 
                {
                    Growing = true;
                }

                //Grow at the beginning
                while (AddNearestComponentAtTheBeginning(Sentence, m_Areas, 0)) 
                {
                    Growing = true;
                }
            }
        }

        /// <summary>
        /// This function searches for and adds the nearest component to the beginning of the sentence
        /// </summary>
        /// <param name="Sentence"></param>
        /// <param name="m_Areas"></param>
        /// <param name="Angle"></param>
        /// <returns></returns>
        public static bool AddNearestComponentAtTheBeginning(Sentence Sentence, List<DetectionArea> m_Areas, float Angle) 
        {
            PageComponent newNeighbour;

            newNeighbour = FindNearestComponent(Sentence, Sentence.Components.First(), m_Areas, Angle, true);
            
            if (newNeighbour!=null) Sentence.Insert(0, newNeighbour);

            return newNeighbour!=null;
        }

        /// <summary>
        /// This function searches for and adds the nearest component add the end of the sentence.
        /// </summary>
        /// <param name="Sentence"></param>
        /// <param name="m_Areas"></param>
        /// <param name="Angle"></param>
        /// <returns></returns>

        public static bool AddNearestComponentAtTheEnd(Sentence Sentence, List<DetectionArea> m_Areas, float Angle) 
        {
            PageComponent newNeighbour;

            newNeighbour = FindNearestComponent(Sentence, Sentence.Components.Last(), m_Areas, Angle, false);

            if (newNeighbour!=null) Sentence.Add(newNeighbour);

            return newNeighbour!=null;
        }


        /// <summary>
        ///This function searches the distance matrix for the closest component
        ///to the given rectangle. The closest component must comply to
        ///the following rules:
        /// * Different from the given component
        /// * Component must not be a part of another sentence
        /// * Must be on the left side of given rectangle (if bLeft is true)
        /// * Must be on the right side of given rectangle (if bLeft is false)
        /// * Angle between rectangle and given rectangle must be less than 5 degrees
        /// </summary>
        /// <param name="Sentence"></param>
        /// <param name="CurrentComponent"></param>
        /// <param name="m_Areas"></param>
        /// <param name="Angle"></param>
        /// <param name="Left"></param>
        /// <returns></returns>
        
        public static PageComponent FindNearestComponent(Sentence Sentence, PageComponent CurrentComponent, List<DetectionArea> m_Areas, float Angle, bool Left) 
        {

            int MinDistance;
            int Distance;

            MinDistance = 2000;

            PageComponent BestCandidate = null;

            int CurrentAreaIndex;
            DetectionArea Area;


            double AverageAngle = Sentence.AverageAngle();
            int MaxAllowedDistance = (int)(Sentence.AverageHeight()*1.5);
            int AreaIndex = CurrentComponent.DetectionAreaIndex;

            for (int Index=0; Index<6; Index++) 
            {
                //the Index is converted to an area number according to the
                //matrix below. The start point is the center area (lIndex = 2).
                //lIndex 3 is on the left when bleft = true or on the right when bleft = false

                //  *************
                //  * 1 * 0 * 1 *
                //  *************
                //  * 3 * 2 * 3 *
                //  *************
                //  * 5 * 4 * 5 *
                //  *************

                CurrentAreaIndex = AreaIndex;

                //translate the lIndex to the x axis of the areas
                if ((Index & 1) != 0) 
                {
                    if (Left) 
                    {
                        CurrentAreaIndex--;
                    }
                    else 
                    {
                        CurrentAreaIndex++;
                    }
                }

            //translate the Index to the y axis of the areas
            CurrentAreaIndex += ((Index/2)-1) * NumberAreasX;   

            //the current area number must ly within boundaries (0, number areas)
            //no throw here if it is outside the boundaries because when the area
            //lies on the edge of the image this algorithm will try to go further
            if (CurrentAreaIndex>=0 && CurrentAreaIndex<m_Areas.Count) {

                Area = m_Areas[CurrentAreaIndex];

                //loop through all the rectangles in the area
                foreach (PageComponent Candidate in Area.Components)
                {
                    if (Candidate.Type == ePageComponentType.eCharacter)
                    {
                        //Candidate neighbour must be on left/right side
                        if ((Left && Candidate.Area.X < CurrentComponent.Area.X) ||
                            (!Left && Candidate.Area.X > CurrentComponent.Area.X)) 
                        {
                            Distance = PageComponent.DistanceBetweenComponents(CurrentComponent, Candidate);

                            //Distance must be between current minimum and Maximum allowed
                            if (MinDistance > Distance && MaxAllowedDistance > Distance) 
                            {
                                //Angle between rectangle and sentence must be less than 5 degrees
                                if (PageComponent.AngleBetweenComponents(CurrentComponent, Candidate) < 5 ||
                                    (Sentence.Area.Bottom > Candidate.Area.Top && Sentence.Area.Top < Candidate.Area.Top) ||
                                    (Sentence.Area.Bottom > Candidate.Area.Bottom && Sentence.Area.Top > Candidate.Area.Bottom))
                                {
                                    BestCandidate = Candidate;
                                    MinDistance = Distance;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (BestCandidate != null)
        {
            m_Areas[BestCandidate.DetectionAreaIndex].Components.Remove(BestCandidate);
        }


        return BestCandidate;
        }

    /// <summary>
    ///this function add spaces to sentences. Space are added between two
    ///characters when the space between the two is larger than
    ///60 percent of the average width of all the characters in the sentence
    /// </summary>
    /// <param name="Image"></param>
        
    public static void AddSpacesToSentences(PageImage Image) 
    {
        int Pointer;
        int AverageWidthCharacters;
        int WidthBetweenCharacters;
        int nSpaces;
        PageComponent newSpace;
        Rectangle newArea;
        Point Component1, Component2;


        foreach (Sentence Sentence in Image.Sentences)
        {
            Pointer = Sentence.Count -1;
            AverageWidthCharacters = (int)(Sentence.AverageWidth() * 0.60);

            if (Sentence.Components.Count > 1)
            {
                while (Pointer > 0)
                {

                    WidthBetweenCharacters = (int)PageComponent.DistanceBetweenComponents(Sentence.Components[Pointer], Sentence.Components[Pointer - 1]);
                    if (WidthBetweenCharacters >= AverageWidthCharacters && AverageWidthCharacters > 0)
                    {

                        nSpaces = WidthBetweenCharacters / AverageWidthCharacters;
                        Component1 = Sentence.Components[Pointer - 1].CenterPoint;
                        Component2 = Sentence.Components[Pointer].CenterPoint;

                        for (int j = 0; j < nSpaces; j++)
                        {
                            //to do: the area part is mostly for visualising the space. Doesn't really needs to
                            //be there. This is an option to speed it up.
                            newSpace = new PageComponent();
                            newSpace.Type = ePageComponentType.eSpace;


                            newArea = new Rectangle((Component2.X - Component1.X) / nSpaces + Component1.X, 
                                (Component2.Y - Component1.Y) / nSpaces + Component1.Y, 
                                2, 
                                2);
                            newSpace.Area = newArea;

                            Sentence.Insert(Pointer, newSpace);
                        }
                    }
                    Pointer--;
                }
            }
        }
    }


    ///// <summary>
    ///// This function calculates the bounding box of all sentences.
    ///// </summary>
    ///// <param name="Image"></param>
    //public static void CalculateBoundingBox(PageImage Image)
    //{

    //    int Top;
    //    int Bottom;
    //    PageComponent Component;

    //    foreach (Sentence Sentence in Image.Sentences)
    //    {
    //        if (Sentence.Components.Count > 0)
    //        {
    //            Component = Sentence.Get(0);

    //            Sentence.Area = Component.Area;

    //            Top = Component.Area.Top;
    //            Bottom = Component.Area.Bottom;

    //            //find the top and bottom of the sentence
    //            foreach (PageComponent SentenceComponent in Sentence.Components)
    //            {
    //                Sentence.Area.Width = SentenceComponent.Area.Right - Sentence.Area.X;

    //                if (Top > SentenceComponent.Area.Y) Top = SentenceComponent.Area.Y;
    //                if (Bottom < SentenceComponent.Area.Y + SentenceComponent.Area.Height) Bottom = SentenceComponent.Area.Y + SentenceComponent.Area.Height;
    //            }

    //            Sentence.Area.Y = Top;
    //            Sentence.Area.Height = Bottom - Top;
    //        }
    //    }
    //}

    /// <summary>
    ///this function cleans up the collection of sentences. With cleaning up
    ///we mean that it removes excessively large sentences/rectangles and that
    ///it removes sentences which are 90% bounded by other sentences. Most of
    ///the time these bounded sentences are a collection of points belong to
    ///characters in the bounding sentence.    
    ///</summary>
    /// <param name="Image"></param>
    public static void CleanUpSentences(PageImage Image)
    {

        //step 1: remove excessively large sentences

        //step 2: merge sentences
        foreach (Sentence OuterSentence in Image.Sentences)
        {
            foreach (Sentence InnerSentence in Image.Sentences)
            {
                if (!InnerSentence.Equals(OuterSentence))
                {
                    //Sentence lies within other sentence, merge the components
                    if (SentenceWithinSentence(OuterSentence, InnerSentence, 2))
                    {
                        //clear the sentence, but don't delete it
                        while (InnerSentence.Components.Count > 0 && OuterSentence.Components.Count > 0)
                        {
                            MergeRectangles(InnerSentence, OuterSentence);

                            Image.Components.Remove(InnerSentence.Get(0));

                            InnerSentence.Delete(0);
                        }
                    }
                    else
                    {
                        //Sentence touches other sentence, combine the two into one
                        if (OuterSentence.Area.IntersectsWith(InnerSentence.Area))
                        {
                            MergeSentences(InnerSentence, OuterSentence);
                        }
                    }
                }
            }

            OuterSentence.SortComponents();
        }

        //step 3: remove empty sentences or small sentences
        int index = Image.Sentences.Count;
        Sentence Sentence;

        while (index > 0)
        {
            index--;
            Image.Sentences[index].ID = index;

            Sentence = Image.Sentences[index];

            if (Sentence.Count < 3)
            {
                //Remove the components of the sentence
                foreach (PageComponent Component in Sentence.Components)
                {
                    Image.Components.Remove(Component);
                }

                //Remove the sentence
                Image.Sentences.RemoveAt(index);
            }
        }

        //step 4: merge components in the sentence if the angle between them is around 90.
        MergeComponentsInSentences(Image);

        //step 5: set a reference to the sentence in all components
        SetSentenceReference(Image);
    }

    /// <summary>
    /// This function iterates through all the sentence
    /// and add a reference to the sentence to all its components
    /// </summary>
    /// <param name="Image"></param>
    public static void SetSentenceReference(PageImage Image)
    {
        foreach (Sentence Sentence in Image.Sentences)
        {
            foreach (PageComponent Component in Sentence.Components)
            {
                Component.Sentence = Sentence;
            }
        }
    }

    /// <summary>
    /// This function checks all sentences if there are components which have an angle
    /// of 90 degrees, if so they will be merged
    /// </summary>
    public static void MergeComponentsInSentences(PageImage Image)
    {
        int index;
        double Angle;
        PageComponent Left, Right;

        foreach (Sentence Sentence in Image.Sentences)
        {
            for (index = 0; index < Sentence.Count - 1; index++)
            {
                Left = Sentence.Components[index];
                Right = Sentence.Components[index + 1];

                if (Left.Type == ePageComponentType.eCharacter &&
                    Right.Type == ePageComponentType.eCharacter)
                {
                    Angle = PageComponent.AngleBetweenComponents(Left, Right);

                    if (Angle > 88 && Angle < 92)
                    {
                        Left.Merge(Right);
                        Sentence.Delete(index + 1);
                    }
                }
            }
        }





    }


    /// <summary>
    /// This function merges both sentences into one. It copies the components to the sentence on the left
    /// and adjusts the bounding box so it contains all components again
    /// </summary>
    /// <param name="Left"></param>
    /// <param name="Right"></param>
    public static void MergeSentences(Sentence Left, Sentence Right)
    {
        Sentence Swap;
        if (Left.Area.Left > Right.Area.Left)
        {
            Swap = Left;
            Left = Right;
            Right = Swap;
        }

        foreach (PageComponent Component in Right.Components)
        {
            Left.Add(Component);
        }

        while (Right.Count > 0)
        {
            Right.Delete(0);
        }
    }


    /// <summary>
    ///This function takes a to be deleted component and sees whether it could
    ///be a part of another component in the outer sentence. It could be a
    ///dot for example. If so, it will merged.
    /// </summary>
    /// <param name="InnerSentence"></param>
    /// <param name="OuterSentence"></param>
    public static void MergeRectangles(Sentence InnerSentence, Sentence OuterSentence) {

        int MinDistance = 1000;
        int Distance;
        double BestAngle = 90;
        double Angle;
        double AverageAngle;
        
        PageComponent InnerComponent;
        PageComponent MergeComponent = null;

        InnerComponent = InnerSentence.Components[0];

        if (InnerComponent.Type != ePageComponentType.eCharacter) return;

        AverageAngle = OuterSentence.AverageAngle();

        //we merge two rectangles if:
        //1. the distance between them is the minimum in comparing to other pairs in the sentence
        //2. if the angle between them is the best 90 degress angle in comparision to the average angle

        foreach (PageComponent OuterComponent in OuterSentence.Components)
        {
            Distance = PageComponent.DistanceBetweenComponents(OuterComponent, InnerComponent);

            if (Distance < MinDistance && OuterComponent.Type == ePageComponentType.eCharacter)
            {

                Angle = PageComponent.AngleBetweenComponents(OuterComponent, InnerComponent);
                Angle = (Angle % 180) - 90 + AverageAngle;

                if (Math.Abs(Angle) <= 10)
                {
                    if (Math.Abs(Angle) <= BestAngle)
                    {
                        //Check if the distance isn't to great, but if the angle is exact 90 degrees always
                        //do a merge.                        
                        if (Distance <= 3 * Math.Max(OuterComponent.Width, OuterComponent.Height) || 
                            Angle==0)
                        {

                            BestAngle = Math.Abs(Angle);
                            MergeComponent = OuterComponent;
                            MinDistance = Distance;
                        }
                    }
                }
            }
        }

        if (MergeComponent != null)
        {
            //we found a rectangle with which we can merge
            MergeComponent.Merge(InnerComponent);
        }
    }

    public static bool SentenceWithinSentence(Sentence OuterSentence, Sentence InnerSentence, int plPixelMargin)
    {
        //this function tests if the inner sentence really lies inside the
        //outer sentence by comparing their bounding boxes. A margin can be used
        //so that this function still return true if the box lies with one row or two columns
        //outside the outer sentence
        Rectangle InnerRectWithMargin = new Rectangle(InnerSentence.Area.Location, InnerSentence.Area.Size);
        InnerRectWithMargin.Inflate(-plPixelMargin, -plPixelMargin);

        return OuterSentence.Area.Contains(InnerRectWithMargin);

    }
    }
}
