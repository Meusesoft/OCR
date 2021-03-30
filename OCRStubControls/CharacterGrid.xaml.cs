using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OCRStubControls
{
    /// <summary>
    /// Interaction logic for CharacterGrid.xaml
    /// </summary>
    public partial class CharacterGrid : UserControl
    {
        public CharacterGrid()
        {
            InitializeComponent();

            GridBrushes = new SolidColorBrush[256];
            Bytes = new byte[1, 1];

            for (int i=0; i<256; i++)
            {
                GridBrushes[i] = new SolidColorBrush(Color.FromArgb((byte)255, (byte)i, (byte)i, (byte)i));
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect GridRect = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
            GridRect.Inflate(-2, -2);
            GridRect.Offset(-1,1);

            drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.Transparent, 0), GridRect);

            int MaxX = Bytes.GetUpperBound(0);
            int MaxY = Bytes.GetUpperBound(1);
            Size PixelSize = new Size(GridRect.Width / MaxX, GridRect.Height / MaxY);
            Rect Pixel = new Rect();
            Pixel.Width = PixelSize.Width;
            Pixel.Height = PixelSize.Height;

            for (int x = 0; x <= MaxX; x++)
            {
                for (int y = 0; y <= MaxY; y++)
                {
                    Pixel.X = x * Pixel.Width;
                    Pixel.Y = y * Pixel.Height;
                    drawingContext.DrawRectangle(GridBrushes[Bytes[x, y]], new Pen(GridBrushes[Bytes[x, y]], 1), Pixel);
                }
            }            
        }

        public byte[,] Bytes { get; set; }
        public SolidColorBrush[] GridBrushes { get; set; }
    }
}
