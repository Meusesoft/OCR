using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Drawing.Imaging;
using OCR;
using NeuralNetwork;


namespace OCRStub
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private OCR.OCR m_OCR;
        public static UserSettings m_UserSettings;
        private int CurrentComponentId;
        
        /// <summary>
        /// Constructor of the main window
        /// </summary>
        public Window1()
        {
            m_OCR = new OCR.OCR();

            //m_UserSettings = new UserSettings();
            //m_UserSettings.m_filename = "d:\\ocr\\settings.xml";

            m_UserSettings = UserSettings.Load("d:\\ocr\\settings.xml");

            InitializeComponent();

            sldThreadCount.Maximum = System.Environment.ProcessorCount;

            m_UserSettings.AddControl(this, sldThreadCount);
            m_UserSettings.AddControl(this, txtTraceFeatureFolder);
            m_UserSettings.AddControl(this, txtImageFile);
            m_UserSettings.AddControl(this, chkDebug);
            m_UserSettings.AddControl(this, chkDrawComponents);
            m_UserSettings.AddControl(this, chkDrawSentenceBox);
            m_UserSettings.AddControl(this, chkDrawSentenceLine);
            m_UserSettings.AddControl(this, chkTraceFeatures);
            m_UserSettings.AddControl(this, chkApplySplitConnected);
            m_UserSettings.AddControl(this, chkApplyWordList);
        }

        /// <summary>
        /// Open another image to recognize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {

            System.Windows.Forms.OpenFileDialog oFileDialog = new System.Windows.Forms.OpenFileDialog();

            oFileDialog.CheckFileExists = true;
            oFileDialog.AddExtension = true;
            oFileDialog.FileName = System.IO.Path.GetFileName(txtImageFile.Text);
            oFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(txtImageFile.Text);

            if (oFileDialog.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                txtImageFile.Text = oFileDialog.FileName;
            }
            
        }


        static BackgroundWorker bw;
        
        /// <summary>
        /// Execute the recognition process on the selected image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
         private void btnAnalyse_Click(object sender, RoutedEventArgs e)
        {
            OCR.DebugTrace.DebugTrace.Debug = (bool)chkDebug.IsChecked;
            OCR.DebugTrace.DebugTrace.TraceFeatures = (bool)chkTraceFeatures.IsChecked;
            OCR.DebugTrace.DebugTrace.TraceFeatureFolder = txtTraceFeatureFolder.Text;
            OCR.DebugTrace.DebugTrace.ApplyWordList = (bool)chkApplyWordList.IsChecked;
            OCR.DebugTrace.DebugTrace.ApplySplitOnConnectedComponents = (bool)chkApplySplitConnected.IsChecked;

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(txtImageFile.Text);
            bi.EndInit();

            ImgOriginal.Source = bi;

            //System.IO.TextWriter writeFile = new StreamWriter("d:\\ocr\\threshold-components.txt");

            //for (int i = 0; i < 256; i += 2)
            //{
            //    Console.WriteLine("Executing Treshold: " + i);
                
            //    m_OCR = new OCR.OCR();
            //    m_OCR.ImageFile = txtImageFile.Text;
            //    m_OCR.PageImage.Threshold = i;
            //    m_OCR.Execute();

            //    writeFile.WriteLine(i + "\t" + m_OCR.PageImage.Components.Count);

            //    Bitmap SaveImage = OCR.DebugTrace.DebugTrace.CreateBitmapFromByteArray(m_OCR.PageImage.ImageBytes, new System.Drawing.Size(m_OCR.PageImage.Width, m_OCR.PageImage.Height));
            //    SaveImage.Save("d:\\ocr\\threshold-image-" + i + ".bmp");
            //}

            //writeFile.Flush();
            //writeFile.Close();

            //return;


            m_OCR.ImageFile = txtImageFile.Text;

            btnAnalyse.IsEnabled = false;

            bw = new BackgroundWorker();
            bw.DoWork += bw_DoWork;
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_Completed;
            bw.RunWorkerAsync(m_OCR);

        }

        private void bw_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            btnAnalyse.IsEnabled = true;

            IntensityHistogram.Values.Clear();

            for (int i = 0; i < 256; i++)
            {
                IntensityHistogram.AddValue(m_OCR.PageImage.Histogram[i]);
            }
            IntensityHistogram.Highlight = m_OCR.PageImage.Threshold;

            imgTreshold.Source = m_OCR.BuildImage(false, false, false, false);
            UpdateResultImage();

            Statistics.Text = m_OCR.StatisticsString;
            Result.Text = m_OCR.PageImage.Content;
        }

        // This event handler updates the progress bar.
        private void bw_ProgressChanged(object sender,
            ProgressChangedEventArgs e)
        {

        }

        private static void bw_DoWork (object sender, DoWorkEventArgs e) 
        {
            OCR.OCR i_OCR = (OCR.OCR)e.Argument;
            i_OCR.Execute();
            

        }



        private void chkDrawComponents_Click(object sender, RoutedEventArgs e)
        {
            UpdateResultImage();
        }

        private void UpdateResultImage()
        {
            imgResult.Source = m_OCR.BuildImage((bool)chkDrawComponents.IsChecked,
                                                (bool)chkDrawSentenceLine.IsChecked,
                                                (bool)chkDrawSentenceLines.IsChecked,
                                                (bool)chkDrawSentenceBox.IsChecked);
        }

        private void chkDrawSentenceBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateResultImage();
        }

        private void chkDrawSentenceLine_Click(object sender, RoutedEventArgs e)
        {
            UpdateResultImage();
        }

        private void chkDrawSentenceLines_Click(object sender, RoutedEventArgs e)
        {
            UpdateResultImage();
        }
        
        private void chkDebug_Unchecked(object sender, RoutedEventArgs e)
        {
            chkTraceFeatures.IsChecked = false;
        }

        private void imgResult_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            String ResultText;
            
            System.Windows.Point Position = e.GetPosition(imgResult);

            lstResults.Items.Clear();
            
            foreach (PageComponent Component in m_OCR.PageImage.Components)
            {
                if (Component.Area.Contains((int)Position.X, (int)Position.Y))
                {
                    ExtractFeatures.CreateCompareMatrixWithoutPruning(Component, Component.Bytes);

                    imgOriginal.Bytes = Component.CompareMatrix;
                    for (int index = 0; index < 256; index++)
                    {
                        imgOriginal.GridBrushes[index] = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)index, (byte)index, (byte)index));
                    }
                    imgOriginal.InvalidateVisual();


                    //Add thinned and stroked image
                    //Merge strokematrix and pixeltypematrix to show all info in the same image
                    for (int x = 0; x < 32; x++)
                    {
                        for (int y = 0; y < 32; y++)
                        {
                            if (Component.PixelTypeMatrix[x, y] == ePixelType.End)
                            {
                                Component.StrokeMatrix[x, y] = 0xFE;
                            }
                            if (Component.PixelTypeMatrix[x, y] == ePixelType.Junction)
                            {
                                Component.StrokeMatrix[x, y] = 0xFD;
                            }
                            if (Component.StrokeMatrix[x, y] == 0xFF)
                            {
                                if (Component.CompareMatrix[x, y] != 0xFF)
                                {
                                    Component.StrokeMatrix[x, y] = 0x04;
                                }
                            }
                        }
                    }

                    imgProjection.Bytes = Component.StrokeMatrix;
                    imgProjection.GridBrushes[4] = new SolidColorBrush(Colors.LightGray);
                    imgProjection.GridBrushes[11] = new SolidColorBrush(Colors.Brown);
                    imgProjection.GridBrushes[12] = new SolidColorBrush(Colors.Maroon);
                    imgProjection.GridBrushes[13] = new SolidColorBrush(Colors.Magenta);
                    imgProjection.GridBrushes[14] = new SolidColorBrush(Colors.Lime);
                    imgProjection.GridBrushes[15] = new SolidColorBrush(Colors.LightCyan);
                    imgProjection.GridBrushes[16] = new SolidColorBrush(Colors.Purple);
                    imgProjection.GridBrushes[32] = new SolidColorBrush(Colors.Blue);
                    imgProjection.GridBrushes[48] = new SolidColorBrush(Colors.Green);
                    imgProjection.GridBrushes[64] = new SolidColorBrush(Colors.Yellow);
                    imgProjection.GridBrushes[253] = new SolidColorBrush(Colors.Red);
                    imgProjection.GridBrushes[254] = new SolidColorBrush(Colors.Red);

                    imgProjection.InvalidateVisual();

                    //Add result
                    if (Component.RecognitionResults.Count > 0)
                    {
                        ResultText = Component.RecognitionResults[0].Content;
                        ResultText += " (";
                        ResultText += Component.RecognitionResults[0].Probability;
                        ResultText += " )";
                        lstResults.Items.Add(ResultText);
                    }
                    else
                    {
                        lstResults.Items.Add("No results");
                    }

                    //Add ID
                    labelID.Content = "ID: " + Component.ID;
                    CurrentComponentId = Component.ID;
                }
            }    
        }


        private void btnOpenComponent_Click(object sender, RoutedEventArgs e)
        {
            ShapeNet Child = new ShapeNet();
            String FileNumber;

            FileNumber = "000000" + CurrentComponentId;
            FileNumber = FileNumber.Substring(FileNumber.Length - 6);

            Child.txtSample.Text = txtTraceFeatureFolder.Text + "image_" + FileNumber + "_org.bmp";
            Child.txtThinningSample.Text = Child.txtSample.Text;
            Child.tabShapeNetFunctions.SelectedIndex = 3;
            Child.lstNeuralNetworks.SelectedIndex = 2;

            Child.Owner = this;
            Child.ShowDialog();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_UserSettings.Save();
        }

        /// <summary>
        /// Open the OCR Test Center window as a modal dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnuTestcenter_Click(object sender, RoutedEventArgs e)
        {
            ShapeNet Child = new ShapeNet();
            Child.Owner = this;
            Child.ShowDialog();
        }

        private void sldThreadCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OCR.OCR.MaximumThreadCount = Convert.ToInt32(e.NewValue);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            OCR.DebugTrace.DebugTrace.Debug = (bool)chkDebug.IsChecked;
            OCR.DebugTrace.DebugTrace.TraceFeatures = (bool)chkTraceFeatures.IsChecked;
            OCR.DebugTrace.DebugTrace.TraceFeatureFolder = txtTraceFeatureFolder.Text;
            OCR.DebugTrace.DebugTrace.ApplyWordList = (bool)chkApplyWordList.IsChecked;
            OCR.DebugTrace.DebugTrace.ApplySplitOnConnectedComponents = (bool)chkApplySplitConnected.IsChecked;

            //mnuTestcenter_Click(sender, e);
        }
    }
}
