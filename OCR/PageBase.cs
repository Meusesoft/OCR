using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;
using AForge.Math;

namespace OCR
{
    class PageBase
    {

        public PageBase()
        {
            m_Stride = 0;
            m_PeakBlackLevel = 0;
            m_PeakWhiteLevel = 255;
            m_Threshold = 128;
        }

        public int Width
        {
            get { return Area.Width; }
        }

        public int Height
        {
            get { return Area.Height; }
        }
        
        public int Stride
        {
            get
            {

                if (m_Stride == 0)
                {
                    m_Stride = CalculateStride(Width);
                }

                return m_Stride;
            }
        }

        /// <summary>
        /// The area of this component.
        /// </summary>
        public Rectangle Area
        {
            get
            {
                return m_Area;
            }

            set
            {
                m_Area = value;
                m_Stride = 0;
            }
        }

        /// <summary>
        /// This property contains the size of the area of this component 
        /// </summary>
        /// <returns></returns>
        public double AreaSize
        {
            get
            {
                return Area.Width * Area.Height;
            }
        }

        /// <summary>
        /// The gray level in the intensity histogram which is the threshold between the
        /// foreground (black) and the background (white) colour.
        /// </summary>
        public int Threshold
        {
            get
            {
                return m_Threshold;
            }

            set
            {
                m_Threshold = value;

                if (Histogram != null)
                {
                    //calculate the new peaks
                    int PeakBlack = 0;
                    int PeakWhite = 0;
                    m_PeakBlackLevel = 0;
                    m_PeakWhiteLevel = 0;

                    for (int index = 0; index < 256; index++)
                    {
                        if (index < m_Threshold && PeakBlack < Histogram[index])
                        {
                            PeakBlack = Histogram[index];
                            m_PeakBlackLevel = index;
                        }
                        if (index >= m_Threshold && PeakWhite < Histogram[index])
                        {
                            PeakWhite = Histogram[index];
                            m_PeakWhiteLevel = index;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The peak in the intensity histogram for the foreground (black)
        /// </summary>
        public int PeakBlackLevel
        {
            get
            {
                return m_PeakBlackLevel;
            }
        }

        /// <summary>
        /// The peak in the intensity histogram for the background (white)
        /// </summary>
        public int PeakWhiteLevel
        {
            get
            {
                return m_PeakWhiteLevel;
            }
        }

        /// <summary>
        /// This function builds the intensity histogram for the current image
        /// </summary>
        public void BuildHistogram()
        {
            Histogram = new int[256];

            //init the histogram
            for (int i = 0; i < 256; i++)
            {
                Histogram[i] = 0;
            }

            //Build the histogram
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Histogram[Bytes[x, y]]++;
                }
            }

            //ImageStatistics statistics = new ImageStatistics(;
            //Histogram histogram = statistics.Gray;

            //for (int i = 0; i < 256; i++)
            //{
            //    Histogram[i] = histogram.Values[i];
            //}

            Histogram[255] = 0;
        }

        /// <summary>
        /// This function calculates the stride for the give width of a rectangle
        /// </summary>
        /// <param name="Width"></param>
        /// <returns></returns>
        protected static int CalculateStride(int Width)
        {
            int Result;

            Result = Width;
            Result += (Result % 4) == 0 ? 0 : (4 - Result % 4);

            return Result;
        }

        private int m_Stride;
        public Rectangle m_Area;

        public Byte[,] Bytes;
        public Byte[,] BinaryBytes;
        private int m_Threshold;
        private int m_PeakBlackLevel;
        private int m_PeakWhiteLevel;

        public int[] Histogram;
    }
}
