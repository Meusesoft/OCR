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
    /// Interaction logic for Chart.xaml
    /// </summary>
    public partial class Chart : UserControl
    {
        public Chart()
        {
            InitializeComponent();
            Clear();
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
            drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.Transparent, 0), new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            
            Pen LinePen = new Pen(Brushes.Black, 1);
            Point From = new Point(0,this.Height);
            Point To = new Point(0, this.Height);
            
            foreach (int Value in Values)
            {
                To = new Point(this.ActualWidth / Values.Capacity + From.X, this.ActualHeight - Value * this.ActualHeight / 100);
                drawingContext.DrawLine(LinePen, From, To);
                From = To;
            }
        }

        public List<int> Values;
         

    }
}
