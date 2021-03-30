using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;

namespace OCR
{
    class OCR
    {

        public OCR()
        {
            Console.WriteLine("Creating OCR instance");

            Statistics = new Statistics();
        }

        public Bitmap Image
        {
            set
            {
                m_Image = new PageImage();
                m_Image.Image = value;
            }
        }

        public String ImageFile
        {
            set
            {
                ReadFile(value);
            }
        }

        public PageImage PageImage
        {
            get
            {
                return m_Image;
            }
        }

        public void Execute() 
        {
            Console.WriteLine("Execute OCR");

            OCR.Statistics.Clear();
            OCR.Statistics.AddCounter("Processors", System.Environment.ProcessorCount);

            SISThreshold filter = new SISThreshold();
            int Threshold = filter.CalculateThreshold(m_Image.Image, m_Image.Area);

            OtsuThreshold filterOtsu = new OtsuThreshold();
            Threshold = filterOtsu.CalculateThreshold(m_Image.Image, m_Image.Area);

            PreprocessPage Processor = new PreprocessPage();
            Processor.Image = m_Image;
            Processor.Treshold = m_Image.CalculateOtsuThreshold();
            m_Image.Threshold = 170;
            Processor.Execute();

            resultBitmap = PageImage.CreateBitmapFromByteArray(m_Image.BinaryBytes, new Size(m_Image.Width, m_Image.Height));

            DetectComponents Step2 = new DetectComponents();
            Step2.Image = m_Image;
            Step2.Execute();

            AnalyseComponents Step3 = new AnalyseComponents();
            Step3.Image = m_Image;
            Step3.Execute();

            DetectSentences Step4 = new DetectSentences();
            Step4.Image = m_Image;
            Step4.Execute();

            ExtractFeatures Step5 = new ExtractFeatures();
            Step5.Image = m_Image;
            Step5.Execute();

            RecogniseComponent Step6 = new RecogniseComponent();
            Step6.Image = m_Image;
            Step6.Execute();

            ProcessSentences Step7 = new ProcessSentences();
            Step7.Image = m_Image;
            Step7.Execute();
        }

        public String StatisticsString 
        {
            get {return Statistics.Counters + Statistics.Durations;}
        }

        public System.Windows.Media.Imaging.BitmapImage BuildImage(bool Components, 
                                                                   bool SentencesLine, 
                                                                   bool SentencesLines, 
                                                                   bool SentencesBox)
        {
            //Here create the Bitmap to the know height, width and format
            System.Drawing.Rectangle cloneRect = new System.Drawing.Rectangle(0, 0, m_Image.Width, m_Image.Height);
            System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            Bitmap cloneBitmap = resultBitmap.Clone(cloneRect, format);

            Graphics Graphics = Graphics.FromImage(cloneBitmap);

            System.Drawing.Pen PenBlue = new System.Drawing.Pen(System.Drawing.Color.Blue);
            System.Drawing.Pen PenRed = new System.Drawing.Pen(System.Drawing.Color.Red);
            System.Drawing.Pen PenGray = new System.Drawing.Pen(System.Drawing.Color.Gray);

            if (Components)
            {
                foreach (PageComponent Component in PageImage.Components)
                {
                    if (Component.Type == ePageComponentType.eCharacter) Graphics.DrawRectangle(PenBlue, Component.Area);
                }
            }

            System.Drawing.Point PointFrom, PointTo;

            foreach (Sentence Sentence in PageImage.Sentences)
            {
                if (SentencesBox) Graphics.DrawRectangle(PenRed, Sentence.Area);

                if (SentencesLines)
                {
                    int Delta;

                    Delta = (int)(Math.Sin(Sentence.Slope) * Sentence.Area.Width / 2);

                    PointFrom = new System.Drawing.Point(Sentence.Area.Left, Sentence.PositionCenter.Y - (int)((Sentence.PositionCenter.X - Sentence.Area.Left) * Math.Sin(Sentence.Slope)));
                    PointTo = new System.Drawing.Point(Sentence.Area.Right, Sentence.PositionCenter.Y + (int)((Sentence.PositionCenter.X - Sentence.Area.Left) * Math.Sin(Sentence.Slope)));

                    Graphics.DrawLine(PenRed, PointFrom, PointTo);

                    PointFrom = new System.Drawing.Point(Sentence.Area.Left, Sentence.PositionBase.Y - (int)((Sentence.PositionBase.X - Sentence.Area.Left) * Math.Sin(Sentence.Slope)));
                    PointTo = new System.Drawing.Point(Sentence.Area.Right, Sentence.PositionBase.Y + (int)((Sentence.PositionBase.X - Sentence.Area.Left) * Math.Sin(Sentence.Slope)));

                    Graphics.DrawLine(PenRed, PointFrom, PointTo);

                    PointFrom = new System.Drawing.Point(Sentence.Area.Left, Sentence.PositionDescent.Y - (int)((Sentence.PositionDescent.X - Sentence.Area.Left) * Math.Sin(Sentence.Slope)));
                    PointTo = new System.Drawing.Point(Sentence.Area.Right, Sentence.PositionDescent.Y + (int)((Sentence.PositionDescent.X - Sentence.Area.Left) * Math.Sin(Sentence.Slope)));

                    Graphics.DrawLine(PenRed, PointFrom, PointTo);
                }
                
                
                
                
                
                
                
                
                
                PointFrom = PointTo = new System.Drawing.Point(0, 0);

                foreach (PageComponent Component in Sentence.Components)
                {
                    if (Component.Type == ePageComponentType.eCharacter)
                    {
                        PointFrom = PointTo;
                        PointTo = Component.CenterPoint;

                        if (!(PointFrom.X==0 && PointFrom.Y==0) && PointFrom != PointTo && SentencesLine)
                        {
                            Graphics.DrawLine(PenGray, PointFrom, PointTo);
                        }
                    }
                    if (Component.Type == ePageComponentType.eSpace && SentencesLine)
                    {
                        Graphics.DrawRectangle(PenGray, Component.Area);
                    }
                }


                //PointTo = Sentence.BaseLine[0];

                //foreach (Point Point in Sentence.BaseLine)
                //{
                //    PointFrom = PointTo;
                //    PointTo = Point;

                //    Graphics.DrawLine(PenGray, PointFrom, PointTo);
                //}

                //PointTo = Sentence.DescentLine[0];

                //foreach (Point Point in Sentence.DescentLine)
                //{
                //    PointFrom = PointTo;
                //    PointTo = Point;

                //    Graphics.DrawLine(PenGray, PointFrom, PointTo);
                //}



            }

            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            cloneBitmap.Save(ms, ImageFormat.Bmp);

            System.Windows.Media.Imaging.BitmapImage returnImage = new System.Windows.Media.Imaging.BitmapImage();

            returnImage.BeginInit();
            returnImage.StreamSource = new System.IO.MemoryStream(ms.ToArray());
            returnImage.EndInit();

            return returnImage;
        }

        /// <summary>
        /// This function reads the file from the disc and converts it (if possible) to a
        /// 8bpp grayscale image.
        /// </summary>
        /// <param name="Filename"></param>
        private void ReadFile(string Filename)
        {
            Bitmap Bitmap = (Bitmap)Bitmap.FromFile(Filename);

            if (Bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                Console.WriteLine(" Converting image to grayscale");
                
                Bitmap = Grayscale.CommonAlgorithms.BT709.Apply(Bitmap);
            }

            //Set the bitmap as the image to OCR.
            Image = Bitmap;
        }

        private PageImage m_Image;
        public Bitmap resultBitmap;
        public static int MaximumThreadCount = 32;
        public static Statistics Statistics;
    }
}

namespace OCR.DebugTrace
{

    class DebugTrace
    {
        public static bool Debug = false;
        public static bool TraceFeatures = false;
        public static string TraceFeatureFolder = "d:\\test\\";
        public static bool ApplyWordList = true;
        public static bool ApplySplitOnConnectedComponents = true;

        /// <summary>
        /// This function creates an image from the given Byte array.
        /// </summary>
        /// <param name="ImageBytes"></param>
        /// <param name="ImageSize"></param>
        /// <returns></returns>
        public static Bitmap CreateBitmapFromByteArray(Byte[] ImageBytes, Size ImageSize)
        {
            Bitmap bitmapResult = new Bitmap(ImageSize.Width, ImageSize.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            
            BitmapData bitmapData = bitmapResult.LockBits(new Rectangle(0, 0, ImageSize.Width, ImageSize.Height),
                ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            // Copy the RGB values back to the bitmap
            IntPtr ptr = bitmapData.Scan0;
            int bytes = bitmapData.Stride * ImageSize.Height;

            System.Runtime.InteropServices.Marshal.Copy(ImageBytes, 0, ptr, bytes);

            bitmapResult.UnlockBits(bitmapData);

            return bitmapResult;
            }

        public static Bitmap CreateBitmapFromByteArray(Byte[,] ImageBytes, Size ImageSize)
        {
            byte[] Bytes;
            
            Bitmap bitmapResult = new Bitmap(ImageSize.Width, ImageSize.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            ColorPalette Palette = bitmapResult.Palette;

            for (int i = 0; i < 256; i++)
            {
                Palette.Entries[i] = System.Drawing.Color.FromArgb(255, i, i, i);
            }
    
            bitmapResult.Palette = Palette;

            BitmapData bitmapData = bitmapResult.LockBits(new Rectangle(0, 0, ImageSize.Width, ImageSize.Height),
                ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            // Copy the RGB values back to the bitmap
            IntPtr ptr = bitmapData.Scan0;
            int bytes = bitmapData.Stride * ImageSize.Height;
            Bytes = new byte[bytes];

            for (int y = 0; y < ImageSize.Height; y++)
            {
                for (int x = 0; x < ImageSize.Width; x++)
                {
                    Bytes[y * bitmapData.Stride + x] = ImageBytes[x, y];
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(Bytes, 0, ptr, bytes);

            bitmapResult.UnlockBits(bitmapData);

            return bitmapResult;
        }
    }







}
