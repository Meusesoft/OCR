using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;


namespace OCR
{
    class PageImage : PageBase
    {

        public PageImage() 
        {
        }

        public Bitmap Image
        {
            get
            {
                return m_Image;
            }
            set
            {
                //A new image is set. Initialise all variables
                m_Image = value;
                Area = new System.Drawing.Rectangle(0, 0, value.Width, value.Height);
                Components = new List<PageComponent>(1);
                Sentences = new List<Sentence>(1);
                GetImageBytes(m_Image);
                BuildHistogram();
            }
        }





        /// <summary>
        /// This property returns the content of the page, which is the result of the recognition process
        /// </summary>
        public String Content
        {
            get
            {
                return BuildContent();
            }
        }


        public List<Sentence> Sentences;
        public List<PageComponent> Components;

        public double AverageAreaComponents()
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



        /// <summary>
        /// This function fills the imagebytes array with the contents of the image.
        /// </summary>
        /// <param name="Image"></param>
        private void GetImageBytes(Bitmap Image)
        {
            if (Image.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                throw new ApplicationException("Image isn't a grayscale bitmap (8 bpp)");
            }

            //Read the bitmap data
            int ImageSize;

            ImageSize = Stride * (int)Image.Height;

            Byte[] ImageBuffer = new Byte[ImageSize];
            
            BitmapData bitmapData = Image.LockBits(Area, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            Marshal.Copy(bitmapData.Scan0, ImageBuffer, 0, ImageSize);

            Image.UnlockBits(bitmapData);

            //Convert the byte[] to byte[,]
            Bytes = new Byte[Width, Height];
            BinaryBytes = new Byte[Width, Height];

            int Pointer;

            for (int y = 0; y < Image.Height; y++)
            {
                Pointer = y * Stride;
                
                for (int x = 0; x < Image.Width; x++)
                {
                    Bytes[x, y] = ImageBuffer[Pointer];
                    Pointer++;
                }
            }            
        }

        /// <summary>
        /// This function builds the content of the image, out of the pagecomponents and sentences
        /// </summary>
        /// <returns></returns>
        private String BuildContent()
        {
            String Result = "";

            foreach (Sentence Sentence in Sentences)
            {
                Result += Sentence.ID + " ";
                Result += Sentence.Content;
                

                //Add linefeed and carriage return
                Result += "\n\r";
            }

            return Result;
        }

        /// <summary>
        /// This function calculates the optimal Threshold according to the Otsu Algorithm
        /// </summary>
        /// <returns></returns>

        public int CalculateOtsuThreshold()
        {
            int Result = 0;

            double Wb, Wf, Ub, Uf, Vb, Vf;
            double WCV, minimalWCV;
            int Total;
            int Divider;

            Total = Width * Height;
            minimalWCV = 0;


            for (int t = 1; t < 255; t++)
            {
                //calculate variance for foreground pixels
                Wf = Uf = Vf = 0;
                for (int i = 0; i < t; i++)
                {
                    Wf += (int)Histogram[i];
                    Uf += (int)Histogram[i] * i;
                }
                Divider = (int)Wf;
                Wf /= Total;
                if (Divider == 0) Uf = 0;
                if (Divider !=0) Uf /= Divider;
                for (int i = 0; i < t; i++)
                {
                    Vf = (int)Histogram[i] * Math.Pow(i-Uf, 2);
                }
                if (Divider == 0) Vf = 0;
                if (Divider != 0) Vf /= Divider;

                //calculate variance for background pixels
                Wb = Ub = Vb = 0;
                for (int i = t; i < 256; i++)
                {
                    Wb += (int)Histogram[i];
                    Ub += (int)Histogram[i] * i;
                }
                Divider = (int)Wb;
                Wb /= Total;
                if (Divider == 0) Ub= 0;
                if (Divider != 0) Ub /= Divider;
                for (int i = t; i < 256; i++)
                {
                    Vb = (int)Histogram[i] * Math.Pow(i - Ub, 2);
                }
                if (Divider == 0) Vb = 0;
                if (Divider != 0) Vb /= Divider;

                //Calculate Within Class Variance
                WCV = Wf * Vf + Wb * Vb;

                if (minimalWCV == 0 || minimalWCV > WCV)
                {
                    minimalWCV = WCV;
                    Result = t;
                }
            }

            return Result;
        }

        public static Bitmap CreateBitmapFromByteArray(Byte[,] ImageBytes, Size ImageSize)
        {
            byte[] Bytes;

            Bitmap bitmapResult = new Bitmap(ImageSize.Width, ImageSize.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

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

        
        private Bitmap m_Image;

    }
}
