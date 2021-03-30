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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ProjectionBars : UserControl
    {
        public ProjectionBars()
        {
            InitializeComponent();
            Clear();
            Vertical = false;
            BarBrush = Brushes.Black;
            HighlightBrush = Brushes.Red;
            Highlight = -1;
        }

       public void Clear()
        {
            Values = new List<int>(25);
            InvalidateVisual();
        }

        public void AddValue(int Value)
        {
            Values.Add(Value);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect ControlRect = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
            Rect HighlightRectangle = new Rect(ControlRect.X, ControlRect.Y, 0, 0);

            ControlRect.Inflate(-2, -2);
           ControlRect.Offset(-1,1);

            drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.Transparent, 0), ControlRect);
            
            Pen LinePen = new Pen(BarBrush, 1);
            Pen HighlightPen = new Pen(HighlightBrush, 1);
            Point From = new Point(0,this.Height);
            Point To = new Point(0, this.Height);
            int MaxValue = 1;
            int index = 0;
            if (Values.Count>0) MaxValue = Values.Max();
            if (MaxValue == 0) MaxValue = 1;

            if (Vertical)
            {
                Rect BarRectangle = new Rect(ControlRect.X, ControlRect.Y, 0, 0);

                foreach (int Value in Values)
                {
                    BarRectangle.Y = BarRectangle.Bottom;
                    BarRectangle.Height = ControlRect.Y + ((index + 1) * ControlRect.Height / Values.Count) - BarRectangle.Y;
                    BarRectangle.X = ControlRect.X;
                    BarRectangle.Width = Value * ControlRect.Width / MaxValue;

                    drawingContext.DrawRectangle(BarBrush, LinePen, BarRectangle);

                    if (index == Highlight)
                    {
                        HighlightRectangle.Y = BarRectangle.Bottom;
                        HighlightRectangle.Height = ControlRect.Bottom - ((index + 1) * ControlRect.Height / Values.Count) - BarRectangle.Y;
                        HighlightRectangle.Y += HighlightRectangle.Height / 2;
                        HighlightRectangle.X = ControlRect.X;
                        HighlightRectangle.Width = ControlRect.Width;

                        drawingContext.DrawRectangle(HighlightBrush, LinePen, HighlightRectangle);
                    }
                    index++;
                }
            }
            else
            {

                Rect BarRectangle = new Rect(ControlRect.X, ControlRect.Height, 0, ControlRect.Height);

                foreach (int Value in Values)
                {
                    BarRectangle.X = BarRectangle.Right;
                    BarRectangle.Width = ControlRect.X + ((index + 1) * ControlRect.Width / Values.Count) - BarRectangle.X;
                    BarRectangle.Y = ControlRect.Bottom - Value * ControlRect.Height / MaxValue;
                    BarRectangle.Height = Value * ControlRect.Height / MaxValue;

                    drawingContext.DrawRectangle(BarBrush, LinePen, BarRectangle);

                    if (index == Highlight)
                        {
                        HighlightRectangle.X = BarRectangle.Right;
                        HighlightRectangle.Width = ControlRect.X + ((index + 1) * ControlRect.Width / Values.Count) - BarRectangle.X;
                        HighlightRectangle.X += BarRectangle.Width / 2;
                        HighlightRectangle.Width = 1;
                        HighlightRectangle.Y = ControlRect.Y;
                        HighlightRectangle.Height = ControlRect.Height; ;

                        drawingContext.DrawRectangle(HighlightBrush, HighlightPen, HighlightRectangle);
                    }
                index++;
                }
            }
        }

        public List<int> Values;

        public bool Vertical { get; set; }
        public Brush BarBrush { get; set; }
        public int Highlight { get; set; }
        public Brush HighlightBrush { get; set; }
    }
}
