using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using NeuralNetwork;
using OCR;
using System.Drawing;
using System.Drawing.Imaging;

namespace OCRStub
{
    /// <summary>
    /// Interaction logic for ShapeNet.xaml
    /// </summary>
    public partial class ShapeNet : Window
    {
        public OCR.ShapeNeuralNetworkCollection ShapeNetwork;

        static Random _r = new Random();
        private UserSettings m_UserSettings;
        private WordList m_WordList;

        public ShapeNet()
        {
            InitializeComponent();

            pbVertical.Vertical = true;
            pbStrokeVertical.Vertical = true;

            m_OCR = new OCR.OCR();
            m_UserSettings = OCRStub.Window1.m_UserSettings;

            m_UserSettings.AddControl(this, txtSample);
            m_UserSettings.AddControl(this, txtThinningSample);
            m_UserSettings.AddControl(this, txtNodesLayer1);
            m_UserSettings.AddControl(this, txtNodesLayer2);
            m_UserSettings.AddControl(this, txtRepetitions);
            m_UserSettings.AddControl(this, tabShapeNetFunctions);
            m_UserSettings.AddControl(this, ThinningAlgorithm);
            m_UserSettings.AddControl(this, lstNeuralNetworks);
            m_UserSettings.AddControl(this, txtWordListSource);
            m_UserSettings.AddControl(this, txtWord);

        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadShapeNet();
        }

        private void FillListBox()
        {
            //lstNeuralNetworks.Items.Clear();
            //lvShapes.Items.Clear();
           // lstNeuralNetworks.Items.
            
            lstNeuralNetworks.ItemsSource = ShapeNetwork.ShapeNets;
            lstNeuralNetworks.Items.Refresh();

            //foreach (OCR.ShapeNet Member in ShapeNetwork.ShapeNets)
            //{
            //    lstNeuralNetworks.Items.Add(Member);
            //}
        }

        private void lstNeuralNetworks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OCR.ShapeNet Member;

            Member = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

            if (Member != null)
            {
                txtNameNeuralNetwork.Text = Member.Name;
                txtExamplesPerFolder.Text = Member.lNumberExamples.ToString();
                chkSubFolders.IsChecked = Member.SearchFolders;
                FillListView(Member);
                FillShapeList(Member);
            }
        }

        private void FillListView(OCR.ShapeNet SelectedShapeNet)
        {
            lvShapes.SelectedItems.Clear();
            lvShapes.ItemsSource = SelectedShapeNet.ShapeList;
            lvShapes.Items.Refresh();

            

            //foreach (OCR.ShapeListEntry Shape in SelectedShapeNet.ShapeList)
            //{
            //    lvShapes.Items.Add(Shape);
            //}
        }


        private void lvShapes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OCR.ShapeListEntry Shape;

            Shape = (OCR.ShapeListEntry)lvShapes.SelectedItem;

            if (Shape != null)
            {
                txtShapeName.Text = Shape.Shape;
                txtShapeFolder.Text = Shape.SampleFolder;
                chkShapeAscent.IsChecked = Shape.Position.Ascent;
                chkShapeBaseline.IsChecked = Shape.Position.Base;
                chkShapeHeightline.IsChecked = Shape.Position.Height;
                chkShapeDecent.IsChecked = Shape.Position.Descent;
                chkShapeCenterLine.IsChecked = Shape.Position.Center;
            }
            else
            {
                txtShapeName.Text = "";
                txtShapeFolder.Text = "";
                chkShapeAscent.IsChecked = false;
                chkShapeBaseline.IsChecked = false;
                chkShapeHeightline.IsChecked = false;
                chkShapeDecent.IsChecked = false;
                chkShapeCenterLine.IsChecked = false;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            OCR.ShapeListEntry Shape;
            OCR.ShapeNet SelectedShapeNet;

            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;
            Shape = (OCR.ShapeListEntry)lvShapes.SelectedItem;

            if (SelectedShapeNet != null && Shape != null)
            {
                SelectedShapeNet.ShapeList.Remove(Shape);
            }

            FillListView(SelectedShapeNet);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            OCR.ShapeListEntry Shape;
            OCR.ShapeNet SelectedShapeNet;

            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;
            Shape = (OCR.ShapeListEntry)lvShapes.SelectedItem;

            Shape.Shape = txtShapeName.Text;
            Shape.SampleFolder = txtShapeFolder.Text;
            Shape.Position.Ascent = (bool)chkShapeAscent.IsChecked;
            Shape.Position.Base = (bool)chkShapeBaseline.IsChecked;
            Shape.Position.Height = (bool)chkShapeHeightline.IsChecked;
            Shape.Position.Descent = (bool)chkShapeDecent.IsChecked;
            Shape.Position.Center = (bool)chkShapeCenterLine.IsChecked;

            FillListView(SelectedShapeNet);
        }

        private void btnAddShape_Click(object sender, RoutedEventArgs e)
        {
            OCR.ShapeListEntry Shape;
            OCR.ShapeNet SelectedShapeNet;

            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;
            Shape = new OCR.ShapeListEntry();

            Shape.Shape = txtShapeName.Text;
            Shape.SampleFolder = txtShapeFolder.Text;

            SelectedShapeNet.ShapeList.Add(Shape);

            FillListView(SelectedShapeNet);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            ShapeNetwork.Save("D:\\OCR\\Shapenet2.xml");
        }

        private void btnUpdateNetwork_Click(object sender, RoutedEventArgs e)
        {
            OCR.ShapeNet SelectedShapeNet;

            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

            SelectedShapeNet.Name = txtNameNeuralNetwork.Text;
            SelectedShapeNet.lNumberExamples = Convert.ToInt32(txtExamplesPerFolder.Text);
            SelectedShapeNet.SearchFolders = (bool)chkSubFolders.IsChecked;
            
            lstNeuralNetworks.Items.Refresh();
        }

        public class Example
        {
            public String Filename { get; set; }
            public int ShapeId { get; set; }
        }

        static BackgroundWorker bw;

        private void btnCreateAndTrain_Click(object sender, RoutedEventArgs e)
        {
            OCR.ShapeNet SelectedShapeNet;
            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

            CreateNetwork(SelectedShapeNet);
            SelectedShapeNet.NeuralNetwork.StartTraining();
            chart1.Clear();

            ContinueTraining();
        }

        private void ContinueTraining()
        {
            OCR.ShapeNet SelectedShapeNet;
            List<Example> Examples = new List<Example>(0);

            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

            btnCreateAndTrain.IsEnabled = false;
            btnCancelTraining.IsEnabled = true;
            btnTrainMore.IsEnabled = false;

            lblProgress.Content = "";
            CreateNetwork(SelectedShapeNet);
            SelectedShapeNet.NeuralNetwork.Repetitions = Convert.ToInt32(txtRepetitions.Text);

            bw = new BackgroundWorker();
            bw.DoWork += bw_DoWork;
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_Completed;
            bw.RunWorkerAsync(SelectedShapeNet);
        
        }

        private void bw_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCreateAndTrain.IsEnabled = true;
            btnTrainMore.IsEnabled = true;
            btnCancelTraining.IsEnabled = false;
        }

        // This event handler updates the progress bar.
        private void bw_ProgressChanged(object sender,
            ProgressChangedEventArgs e)
        {
            this.lblProgress.Content = "Repetition " + Convert.ToString(e.ProgressPercentage + 1) + " completed (" + Convert.ToString((double)e.UserState) + " %)";
            chart1.AddValue(Convert.ToInt32((double)e.UserState));
        }

        private static void bw_DoWork (object sender, DoWorkEventArgs e) 
        {
            OCR.ShapeNet SelectedShapeNet;
            List<Example> Examples = new List<Example>(0);
            double TrainingResult;

            SelectedShapeNet = (OCR.ShapeNet)e.Argument;

            if (!bw.CancellationPending) CollectTrainingExamples(SelectedShapeNet, Examples);

            if (!bw.CancellationPending) CreateTrainingData(SelectedShapeNet, Examples);


            //bw.ReportProgress(0);

            for (int i = 0; i < SelectedShapeNet.NeuralNetwork.Repetitions && !bw.CancellationPending; i++)
            {
                TrainingResult = SelectedShapeNet.NeuralNetwork.TrainingExecuteRepetition(1);
                bw.ReportProgress(i, (object)TrainingResult);
            }
        }

        private void CreateNetwork(OCR.ShapeNet ShapeNet)
        {
            ShapeNet.NeuralNetwork.ClearNetwork();

            int OutputNodes = ShapeNet.ShapeList.Count;
            int Layer1Nodes = Convert.ToInt32(txtNodesLayer1.Text);
            int Layer2Nodes = Convert.ToInt32(txtNodesLayer2.Text);

            //fill the network with nodes
            for (int lIndex = 0; lIndex < 48; lIndex++)
            {
                ShapeNet.NeuralNetwork.AddNode(0, eNeuralNodeType.eInput);
            }

            for (int lIndex = 0; lIndex < Layer1Nodes; lIndex++)
            {
                ShapeNet.NeuralNetwork.AddNode(1, eNeuralNodeType.eHidden);
            }

            for (int lIndex = 0; lIndex < Layer2Nodes; lIndex++)
            {
                ShapeNet.NeuralNetwork.AddNode(2, eNeuralNodeType.eHidden);
            }

            for (int lIndex = 0; lIndex < OutputNodes; lIndex++)
            {
                ShapeNet.NeuralNetwork.AddNode(3, eNeuralNodeType.eOutput);
            }

            ShapeNet.NeuralNetwork.InitNetworkForLearning();
        }

        private static void MakeExampleList(List<String> Filenames, String SampleFolder)
        {
            string[] fileEntries = System.IO.Directory.GetFiles(SampleFolder, "*.bmp");
            foreach (string fileName in fileEntries)
            {
                Filenames.Add(fileName);
            }

            string[] folderEntries = System.IO.Directory.GetDirectories(SampleFolder);
            foreach (string folder in folderEntries)
            {
                MakeExampleList(Filenames, folder);
            }

        }

        private static void CollectTrainingExamples(OCR.ShapeNet ShapeNet, List<Example> Examples)
        {
            List<String> Filenames = new List<string>(0);
            Example newExample;
            int NumberExamples = ShapeNet.lNumberExamples;
             

            //init learning data
            //collect all available examples
            for (int lIndex = 0; lIndex < ShapeNet.ShapeList.Count; lIndex++)
            {
                Filenames.Clear();

                MakeExampleList(Filenames, ShapeNet.ShapeList[lIndex].SampleFolder);

                if (Filenames.Count > 0)
                {
                    for (long lIndex2 = 0; lIndex2 < NumberExamples; lIndex2++)
                    {
                        newExample = new Example();
                        newExample.Filename = Filenames[_r.Next(Filenames.Count)];
                        newExample.ShapeId = lIndex;

                        Examples.Add(newExample);
                    }
                }
            }
        }

        private static void CreateTrainingData(OCR.ShapeNet ShapeNet, List<Example> Examples)
        {
            NeuralNetwork.LearningData oLearningData;
            OCR.PageComponent PageComponent;
            sNeuralInput newInput;

            foreach (Example Example in Examples)
            {
                PageComponent = new OCR.PageComponent();
                PageComponent.LoadBitmap(Example.Filename);

                ExtractFeatures.ExecuteExtractFeatures(PageComponent, true);

                PageComponent.Position.Ascent  = ShapeNet.ShapeList[Example.ShapeId].Position.Ascent;
                PageComponent.Position.Height  = ShapeNet.ShapeList[Example.ShapeId].Position.Height;
                PageComponent.Position.Center  = ShapeNet.ShapeList[Example.ShapeId].Position.Center;
                PageComponent.Position.Base    = ShapeNet.ShapeList[Example.ShapeId].Position.Base;
                PageComponent.Position.Descent = ShapeNet.ShapeList[Example.ShapeId].Position.Descent;

                //Fill the
                oLearningData = new NeuralNetwork.LearningData();

                oLearningData.oInput.fInputs.Clear();
                oLearningData.oOutput.fOutputs.Clear();

                newInput = RecogniseComponent.CalculateNetworkInput(PageComponent);
                oLearningData.oInput = newInput;

                for (long lIndex2 = 0; lIndex2 < ShapeNet.ShapeList.Count; lIndex2++)
                {
                    if (Example.ShapeId == lIndex2)
                    {
                        oLearningData.oOutput.fOutputs.Add(1);
                    }
                    else
                    {
                        oLearningData.oOutput.fOutputs.Add(0);
                    }
                }

                ShapeNet.NeuralNetwork.AddSituation(oLearningData);
            }

            ShapeNet.NeuralNetwork.ComputeInputRatios();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog oFileDialog = new System.Windows.Forms.OpenFileDialog();

            oFileDialog.CheckFileExists = true;
            oFileDialog.AddExtension = true;
            if (txtSample.Text.Length > 0)
            {
                oFileDialog.FileName = System.IO.Path.GetFileName(txtSample.Text);
                oFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(txtSample.Text);
            }

            if (oFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSample.Text = oFileDialog.FileName;
                //imgSample.Source = new BitmapImage(new Uri(txtSample.Text));
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            OCR.ShapeNet SelectedShapeNet;
            OCR.PageComponent PageComponent;

            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

            PageComponent.newID = 0;
            PageComponent = new OCR.PageComponent();
            PageComponent.LoadBitmap(txtSample.Text);

            ExtractFeatures.ExecuteExtractFeatures(PageComponent, PageComponent, true);
            RecogniseComponent.Recognise(SelectedShapeNet, PageComponent);

            //Recognise the component
            lstResults.Items.Clear();
            String ResultText = "";

            foreach (RecognitionResult Result in PageComponent.RecognitionResults)
            {
                ResultText = Result.Content + " (" + Result.Probability + ")";
                lstResults.Items.Add(ResultText);

            }

            //Build the original image
            PageComponent Original = new PageComponent();
            Original.LoadBitmap(txtSample.Text);

            ExtractFeatures.CreateCompareMatrixWithoutPruning(Original, Original.Bytes);

            imgSample.Bytes = Original.CompareMatrix;
            for (int index=0; index<256; index++)
            {
                imgSample.GridBrushes[index] = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)index, (byte)index, (byte)index));
            }
            imgSample.InvalidateVisual();

            ExtractFeatures.CreateCompareMatrixWithoutPruning(PageComponent);

            //Build the thinned and stroked image
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (PageComponent.StrokeMatrix[x, y] == 0xFF && PageComponent.CompareMatrix[x, y] != 0xFF)
                    {
                        PageComponent.StrokeMatrix[x, y] = 0x04;
                    }
                    if (PageComponent.PixelTypeMatrix[x, y] == ePixelType.End)
                    {
                        PageComponent.StrokeMatrix[x, y] = 0xFE;
                    }
                    if (PageComponent.PixelTypeMatrix[x, y] == ePixelType.Junction)
                    {
                        PageComponent.StrokeMatrix[x, y] = 0xFD;
                    }
                }
            }

            imgProjection.Bytes = PageComponent.StrokeMatrix;
            imgProjection.GridBrushes[0] = new SolidColorBrush(Colors.Black);
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

            lblStrokes.Content = "#Strokes: " + Convert.ToString(PageComponent.Strokes);
            
            //Bitmap StrokeMatrixBitmap = OCR.DebugTrace.DebugTrace.CreateBitmapFromByteArray(PageComponent.StrokeMatrix, new System.Drawing.Size(32, 32));
            //StrokeMatrixBitmap.Save("d:\\ocr\\temp.bmp");
            //imgProjection.Source = new BitmapImage(new Uri("d:\\ocr\\temp.bmp"));

            pbHorizontal.Clear();

            int ProjectionValue;
            for (int x = 0; x < 3; x++)
            {
                ProjectionValue = 0;
                for (int y = 0; y < 3; y++)
                {
                    ProjectionValue += PageComponent.PixelTypeProjectionJunction[x, y];
                    ProjectionValue += PageComponent.PixelTypeProjectionEndpoint[x, y];
                }
                pbHorizontal.AddValue(ProjectionValue);
            }

            pbVertical.Clear();
            for (int y = 0; y < 3; y++)
            {
                ProjectionValue = 0;
                for (int x = 0; x < 3; x++)
                {
                    ProjectionValue += PageComponent.PixelTypeProjectionJunction[x, y];
                    ProjectionValue += PageComponent.PixelTypeProjectionEndpoint[x, y];
                }
                pbVertical.AddValue(ProjectionValue);
            }

            pbStrokeHorizontal.Clear();
            foreach (int Value in PageComponent.lStrokeDirectionX)
            {
                pbStrokeHorizontal.AddValue(Value);
            }

            pbStrokeVertical.Clear();
            foreach (int Value in PageComponent.lStrokeDirectionY)
            {
                pbStrokeVertical.AddValue(Value);
            }
            
        }

        private void btnCancelTraining_Click(object sender, RoutedEventArgs e)
        {
            bw.CancelAsync();
            btnCancelTraining.IsEnabled = false;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            LoadShapeNet();
            lstNeuralNetworks.SelectedIndex = 0;

            //if (txtSample.Text.Length>0) imgSample.Source = new BitmapImage(new Uri(txtSample.Text));
        }

        private void LoadShapeNet()
        {
            OCR.ShapeNeuralNetworkCollection SC = new OCR.ShapeNeuralNetworkCollection();
            //SC.Save("d:\\ocr\\test.xml");

            ShapeNetwork = SC.Load("D:\\OCR\\Shapenet.xml");

            FillListBox();
        }

        private void btnTrainMore_Click(object sender, RoutedEventArgs e)
        {
            ContinueTraining();
        }

        private void RunThinning_Click(object sender, RoutedEventArgs e)
        {
            OCRStubControls.CharacterGrid[] CharacterGrids = new OCRStubControls.CharacterGrid[7];

            OCR.PageImage PageImage;
            OCR.PageComponent PageComponent;

            //Initialize charactergrids
            CharacterGrids[0] = ThinningStep1;
            CharacterGrids[1] = ThinningStep2;
            CharacterGrids[2] = ThinningStep3;
            CharacterGrids[3] = ThinningStep4;
            CharacterGrids[4] = ThinningStep5;
            CharacterGrids[5] = ThinningStep6;
            CharacterGrids[6] = ThinningStep7;

            PageImage = new OCR.PageImage();

            PageComponent = new OCR.PageComponent();
            PageComponent.LoadBitmap(txtThinningSample.Text);

            for (int x = 0; x < PageComponent.Width; x++)
            {
                for (int y = 0; y < PageComponent.Height; y++)
                {
                    PageComponent.BinaryBytes[x, y] = PageComponent.Bytes[x, y];
                }
            }

            ExtractFeatures.CreateCompareMatrixWithoutPruning(PageComponent);

            for (int x = 0; x < PageComponent.Width; x++)
            {
                for (int y = 0; y < PageComponent.Height; y++)
                {
                    PageComponent.BinaryBytes[x, y] = (PageComponent.Bytes[x, y]==0xFF ? (Byte)0xFF : (Byte)0x00);
                }
            }

            CharacterGrids[0].Bytes = new byte[32, 32];
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    CharacterGrids[0].Bytes[x, y] = PageComponent.CompareMatrix[x, y];
                }
            }

            CharacterGrids[0].InvalidateVisual();

            for (int i = 0; i < 6; i++)
            {
                CharacterGrids[i+1].Bytes = new byte[32, 32];

                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        CharacterGrids[i+1].Bytes[x, y] = PageComponent.CompareMatrix[x, y];
                    }
                }

                switch (ThinningAlgorithm.SelectedIndex)
                {
                    case 0: //Standard

                        ExtractFeatures.Thinning(CharacterGrids[i + 1].Bytes, 0, 0xFF, i + 1);
                        break;

                    case 1: //Erode

                        ExtractFeatures.ErodeThinning(CharacterGrids[i + 1].Bytes, 0, 0xFF, i + 1);
                        break;

                    case 2: //Middle

                        ExtractFeatures.MiddleThinning(CharacterGrids[i + 1].Bytes, 0, 0xFF);
                        break;

                    case 3: //Condensed

                        PageComponent = new OCR.PageComponent();
                        PageComponent.LoadBitmap(txtThinningSample.Text);
                        ExtractFeatures.CondensedThinning(PageImage, PageComponent, 0, 0xFF, i + 1);
                        for (int x = 0; x < 32; x++)
                        {
                            for (int y = 0; y < 32; y++)
                            {
                                CharacterGrids[i + 1].Bytes[x, y] = PageComponent.CompareMatrix[x, y];
                            }
                        }
                        break;

                    case 4: //Pruning

                        PageComponent = new OCR.PageComponent();
                        PageComponent.LoadBitmap(txtThinningSample.Text);

                        for (int x = 0; x < PageComponent.Width; x++)
                        {
                            for (int y = 0; y < PageComponent.Height; y++)
                            {
                                PageComponent.BinaryBytes[x, y] = PageComponent.Bytes[x, y];
                            }
                        }

                        ExtractFeatures.ThinningPruningOnOriginalImage(PageComponent, PageComponent, 0, i+1);

                        ExtractFeatures.CreateCompareMatrixWithoutPruning(PageComponent);

                        for (int x = 0; x < 32; x++)
                        {
                            for (int y = 0; y < 32; y++)
                            {
                                CharacterGrids[i + 1].Bytes[x, y] = PageComponent.CompareMatrix[x, y];
                            }
                        }

                        break;

                    case 5: //Pruning  original

                        PageComponent = new OCR.PageComponent();
                        PageComponent.LoadBitmap(txtThinningSample.Text);
                        ExtractFeatures.ThinningPruningOnOriginalImage(PageImage, PageComponent, 2, i+1);
                        ExtractFeatures.CreateCompareMatrixWithoutPruning(PageComponent);
                        for (int x = 0; x < 32; x++)
                        {
                            for (int y = 0; y < 32; y++)
                            {
                                CharacterGrids[i + 1].Bytes[x, y] = PageComponent.CompareMatrix[x, y];
                            }
                        }
                        break;
                }

                CharacterGrids[i+1].InvalidateVisual();
            }
        }

        private void BrowseThinning_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog oFileDialog = new System.Windows.Forms.OpenFileDialog();

            oFileDialog.CheckFileExists = true;
            oFileDialog.AddExtension = true;
            if (txtThinningSample.Text.Length > 0)
            {
                oFileDialog.FileName = System.IO.Path.GetFileName(txtThinningSample.Text);
                oFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(txtThinningSample.Text);
            }

            if (oFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtThinningSample.Text = oFileDialog.FileName;
            }
        }

        private void txtThinningSample_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            FillFileList("");
        }

        static BackgroundWorker bwFileList;

        struct ParameterFileListRefresh
        {
            public string Filter;
            public OCR.ShapeNet ShapeNet;
        }

        struct ShapeFileList
        {
            public string File;
            public string Shape;
        }

        static List<ShapeFileList> ShapeFiles;

        private void FillFileList(String Filter)
        {
            btnRefresh.IsEnabled = false;
            btnFilterOnShape.IsEnabled = false;

            lstFiles.Items.Clear();

            ParameterFileListRefresh Parameters;

            Parameters.ShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;
            Parameters.Filter = Filter;

            bwFileList = new BackgroundWorker();
            bwFileList.DoWork += bw_DoWorkFileList;
            bwFileList.WorkerReportsProgress = true;
            bwFileList.WorkerSupportsCancellation = true;
            bwFileList.ProgressChanged += bw_ProgressFileList;
            bwFileList.RunWorkerCompleted += bw_CompletedFileList;

            bwFileList.RunWorkerAsync(Parameters);
        }

        private void bw_CompletedFileList(object sender, RunWorkerCompletedEventArgs e)
        {
            btnRefresh.IsEnabled = true;
            btnFilterOnShape.IsEnabled = true;
        }

        private void bw_ProgressFileList(object sender, ProgressChangedEventArgs e)
        {
            lstFiles.Items.Add(e.UserState);
        }

        private void bw_DoWorkFileList (object sender, DoWorkEventArgs e) 
        {
            ParameterFileListRefresh pFR;

            pFR = (ParameterFileListRefresh)e.Argument;

            DoFillFileList(pFR.Filter, pFR.ShapeNet);
        }
        
        public void DoFillFileList(string Filter, OCR.ShapeNet ShapeNet)
        {
            try
            {

                if (ShapeFiles == null) ShapeFiles = new List<ShapeFileList>(0);

                if (ShapeFiles.Count == 0)
                {
                    DirectoryInfo di = new DirectoryInfo("D:\\OCR\\Images\\");
                    FileInfo[] Files = di.GetFiles("*_org.bmp");

                    foreach (FileInfo fi in Files)
                    {
                        if (Filter.Length > 0)
                        {
                            String Filename;
                            RecognitionResult MostLikelyShape;

                            // OCR.ShapeNet Shapenet = new OCR.ShapeNet();
                            PageComponent Component = new PageComponent();

                            //Get the selected item
                            Filename = fi.Name.ToString();

                            //Load the image
                            Component.LoadBitmap(fi.FullName.ToString());

                            //Extract features and recognize
                            ExtractFeatures.ExecuteExtractFeatures(Component, true);
                            RecogniseComponent.Recognise(ShapeNet, Component);

                            //Fill the recognition list
                            MostLikelyShape = new RecognitionResult();
                            MostLikelyShape.Probability = 0;

                            foreach (RecognitionResult Result in Component.RecognitionResults)
                            {
                                if (Result.Probability > MostLikelyShape.Probability)
                                {
                                    MostLikelyShape = Result;
                                }
                            }

                            //Add ShapeFile to cache
                            ShapeFileList ShapeFile;

                            ShapeFile.Shape = MostLikelyShape.Content;
                            ShapeFile.File = fi.Name.ToString();

                            ShapeFiles.Add(ShapeFile);

                            //Add file to filelist
                            if (MostLikelyShape.Content == Filter)
                            {
                                bwFileList.ReportProgress(0, fi.Name.ToString());
                            }
                        }
                        else
                        {
                            bwFileList.ReportProgress(0, fi.Name.ToString());
                        }
                    }
                }
                else
                {
                    foreach (ShapeFileList ShapeFile in ShapeFiles)
                    {
                        if (Filter.Length == 0)
                        {
                            bwFileList.ReportProgress(0, ShapeFile.File);
                        }
                        else
                        {
                            if (Filter == ShapeFile.Shape)
                            {
                                bwFileList.ReportProgress(0, ShapeFile.File);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message);
                Console.WriteLine("  In: " + e.StackTrace);
            }
        }

        private void FillShapeList(OCR.ShapeNet SelectedShapeNet)
        {
            lstShapes.UnselectAll();
            lstShapes.ItemsSource = SelectedShapeNet.ShapeList;
            lstShapes.Items.Refresh();
        }


        private OCR.OCR m_OCR;

        private void lstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String Filename;
            String ResultString;
            RecognitionResult MostLikelyShape;

            try
            {

                // OCR.ShapeNet Shapenet = new OCR.ShapeNet();
                PageComponent Component = new PageComponent();

                //Get the selected item
                Filename = "D:\\OCR\\Images\\" + (String)lstFiles.SelectedItem;

                if (lstFiles.SelectedItem == null) return;

                //Load the image
                Component.LoadBitmap(Filename);

                //Extract features and recognize
                ExtractFeatures.ExecuteExtractFeatures(Component, true);
                RecogniseComponent.Recognise((OCR.ShapeNet)lstNeuralNetworks.SelectedItem, Component);

                //Fill the imgContainer
                ExtractFeatures.CreateCompareMatrixWithoutPruning(Component, Component.Bytes);
                imgTrainingDataOriginal.Bytes = Component.CompareMatrix;
                for (int index = 0; index < 256; index++)
                {
                    imgTrainingDataOriginal.GridBrushes[index] = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)index, (byte)index, (byte)index));
                }
                imgTrainingDataOriginal.InvalidateVisual();

                //Fill the recognition list
                lstRecognitionResult.Items.Clear();

                MostLikelyShape = new RecognitionResult();
                MostLikelyShape.Probability = 0;

                foreach (RecognitionResult Result in Component.RecognitionResults)
                {
                    if (Result.Probability > MostLikelyShape.Probability)
                    {
                        MostLikelyShape = Result;
                    }
                }

                ShapeListEntry Shape = new ShapeListEntry(); ;

                //Select the result with the highest probability in the shape list.
                if (MostLikelyShape.Probability > 0)
                {
                    ResultString = MostLikelyShape.Content + " (" + MostLikelyShape.Probability + ")";
                    lstRecognitionResult.Items.Add(ResultString);

                    foreach (Object Item in lstShapes.Items)
                    {
                        Shape = (ShapeListEntry)Item;
                        if (Shape.Shape == MostLikelyShape.Content)
                        {
                            lstShapes.SelectedItem = Item;
                            lstShapes.ScrollIntoView(Item);
                            lstShapes.Focus();
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception caught: " + exp.Message);
                Console.WriteLine("  In: " + exp.StackTrace);
            }
        }


        /// <summary>
        /// This function copies the selected file in the filelist to the target folder
        /// of the shape.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCopyFileToShape_Click(object sender, RoutedEventArgs e)
        {
            CopyShape();
        }


        private void lstShapes_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CopyShape();    
            }

            if (e.Key == Key.Right)
            {
                lstFiles.SelectedIndex++;
            }
        }

        private void CopyShape()
        {
            ShapeListEntry Shape = new ShapeListEntry(); ;
            String ImageFile;
            String DestinationFile;
            int index;
            bool Continue;

            //Check if a file and a shape are selected
            if (lstFiles.SelectedIndex == -1 && lstShapes.SelectedIndex == -1)
            {
                MessageBox.Show("A file and a shape must be selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Get the selected items;
            ImageFile = "D:\\OCR\\Images\\" + lstFiles.SelectedItem;
            Shape = (ShapeListEntry)lstShapes.SelectedItem;

            //Determine a new file name for the target file
            DirectoryInfo di = new DirectoryInfo(Shape.SampleFolder + "\\");
            FileInfo[] Files = di.GetFiles("*.bmp");

            index = 1;

            do
            {
                Continue = false;
                DestinationFile = "00000000" + Convert.ToString(index);
                DestinationFile = DestinationFile.Substring(DestinationFile.Length - 8);
                DestinationFile += ".bmp";

                foreach (FileInfo File in Files)
                {
                    if (File.Name == DestinationFile) Continue = true;
                }

                index++;

            } while (Continue);

            //Copy the file
            DestinationFile = Shape.SampleFolder + "\\" + DestinationFile;
            System.IO.File.Copy(ImageFile, DestinationFile, true);

            //Remove the file from the filelist.
            object SelectedItem;
            SelectedItem = lstFiles.SelectedItem;
            lstFiles.SelectedIndex++;

            lstFiles.Items.Remove(SelectedItem);

            //Delete the original file
            try
            {
                System.IO.File.Delete(ImageFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message);
                Console.WriteLine("  In: " + e.StackTrace);
            }
        }

        private void btnDeleteExamples_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete ALL examples?", 
                                "Warning", MessageBoxButton.YesNo, 
                                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                OCR.ShapeNet Member;

                Member = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

                if (Member != null)
                {
                    foreach (ShapeListEntry Shape in Member.ShapeList)
                    {
                        DirectoryInfo di = new DirectoryInfo(Shape.SampleFolder + "\\");
                        FileInfo[] Files = di.GetFiles("*.*");

                        foreach (FileInfo File in Files)
                        {
                            System.IO.File.Delete(File.FullName);
                        }
                    }
                }
            }
        }

        private void WindowShapenet_Closing(object sender, CancelEventArgs e)
        {
            m_UserSettings.SaveControlsValue(this);
            m_UserSettings.RemoveControls(this);
        }

        private void btnBrowseWordList_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog oFileDialog = new System.Windows.Forms.OpenFileDialog();

            oFileDialog.CheckFileExists = true;
            oFileDialog.AddExtension = true;
            if (txtWordListSource.Text.Length > 0)
            {
                oFileDialog.FileName = System.IO.Path.GetFileName(txtWordListSource.Text);
                oFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(txtWordListSource.Text);
            }

            if (oFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtWordListSource.Text = oFileDialog.FileName;
            }
        }

        private void btnCreateWordList_Click(object sender, RoutedEventArgs e)
        {
            m_WordList = new WordList();

            m_WordList.Build(txtWordListSource.Text);
            m_WordList.SaveXML("d:\\ocr\\wordlist.xml");
        }

        private void btnTestWord_Click(object sender, RoutedEventArgs e)
        {
            String Result;

            if (m_WordList == null)
            {
                WordList temp = new WordList();
                m_WordList = temp.LoadXML("d:\\ocr\\wordlist.xml");
            }

            lstWordResult.Items.Clear();

            Result = m_WordList.Contains(txtWord.Text, false);

            if (Result.ToLower() == txtWord.Text.ToLower())
            {
                lstWordResult.Items.Add("Found: " + Result);
            }
            else
            {
                lstWordResult.Items.Add("Not found");

                List<SuggestionEntry> Suggestions = m_WordList.Suggestions(txtWord.Text);

                foreach (SuggestionEntry Suggestion in Suggestions)
                {
                    lstWordResult.Items.Add(Suggestion.Suggestion + " (" + Suggestion.EditDistance + ")" ) ;
                }
            }
        }

        private void tabWordList_Loaded(object sender, RoutedEventArgs e)
        {
            btnTestWord_Click(sender, e);
        }

        private void btnTestNetwork_Click(object sender, RoutedEventArgs e)
        {
            OCR.ShapeNet SelectedShapeNet;
            PageComponent PageComponent;
            List<Example> Examples = new List<Example>(0);
            bool Result;

            //Collect the examples
            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

            List<String> Filenames = new List<string>(0);
            Example newExample;

            //collect all available examples
            for (int lIndex = 0; lIndex < SelectedShapeNet.ShapeList.Count; lIndex++)
            {
                Filenames.Clear();

                MakeExampleList(Filenames, SelectedShapeNet.ShapeList[lIndex].SampleFolder);

                for (int lIndex2 = 0; lIndex2 < Filenames.Count; lIndex2++)
                {
                    newExample = new Example();
                    newExample.Filename = Filenames[lIndex2];
                    newExample.ShapeId = lIndex;

                    Examples.Add(newExample);
                }
            }   
            
            //Test all examples against the network
            foreach (Example Example in Examples)
            {
                PageComponent = new PageComponent();
                PageComponent.LoadBitmap(Example.Filename);
                Result = false;

                ExtractFeatures.ExecuteExtractFeatures(PageComponent, true);
                RecogniseComponent.RecogniseWithoutConnectedRepair(SelectedShapeNet, PageComponent);

                if (PageComponent.RecognitionResults.Count > 0 &&
                    Example.ShapeId <SelectedShapeNet.ShapeList.Count)
                {
                    if (SelectedShapeNet.ShapeList[Example.ShapeId].Shape.ToLower() == PageComponent.RecognitionResults[0].Content.ToLower())
                    {
                        Result = true;

                    }
                }

                if (!Result)
                {
                    lstFailedExamples.Items.Add(Example.Filename);
                }
            }
        }

        private void lstFailedExamples_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PageComponent PageComponent;
            String item;
            OCR.ShapeNet SelectedShapeNet;


            //Load the image
            item = (String)lstFailedExamples.SelectedItem;
            PageComponent = new PageComponent();
            PageComponent.LoadBitmap(item);

            //Recognise the image and fill the result list
            SelectedShapeNet = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;

            ExtractFeatures.ExecuteExtractFeatures(PageComponent, true);
            RecogniseComponent.RecogniseWithoutConnectedRepair(SelectedShapeNet, PageComponent);

            lstFailedResult.Items.Clear();
            String ResultText = "";

            foreach (RecognitionResult Result in PageComponent.RecognitionResults)
            {
                ResultText = Result.Content + " (" + Result.Probability + ")";
                lstFailedResult.Items.Add(ResultText);

            }

            //Build the image
            PageComponent Original = new PageComponent();
            Original.LoadBitmap(item);

            PageComponent OriginalWithPruning = new PageComponent();
            OriginalWithPruning.LoadBitmap(item);

            ExtractFeatures.CreateCompareMatrixWithoutPruning(Original);
            ExtractFeatures.CreateCompareMatrix(OriginalWithPruning);

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    PageComponent.CompareMatrix[x, y] = Original.CompareMatrix[x, y];
                    
                    if (Original.CompareMatrix[x, y] != 0xFF && PageComponent.CompareMatrix[x, y] == 0xFF)
                    {
                        PageComponent.CompareMatrix[x, y] = 4;

                    }   
                        
                    if (PageComponent.StrokeMatrix[x, y] != 0xFF)
                    {
                        PageComponent.CompareMatrix[x, y] = PageComponent.StrokeMatrix[x, y];
                    }
                }
            }

            imgFailed.Bytes = PageComponent.CompareMatrix;
            imgFailed.GridBrushes[0] = new SolidColorBrush(Colors.Gray);
            imgFailed.GridBrushes[4] = new SolidColorBrush(Colors.LightGray);
            imgFailed.GridBrushes[11] = new SolidColorBrush(Colors.Brown);
            imgFailed.GridBrushes[12] = new SolidColorBrush(Colors.Maroon);
            imgFailed.GridBrushes[13] = new SolidColorBrush(Colors.Magenta);
            imgFailed.GridBrushes[14] = new SolidColorBrush(Colors.Lime);
            imgFailed.GridBrushes[15] = new SolidColorBrush(Colors.LightCyan);
            imgFailed.GridBrushes[16] = new SolidColorBrush(Colors.Purple);
            imgFailed.GridBrushes[32] = new SolidColorBrush(Colors.Blue);
            imgFailed.GridBrushes[48] = new SolidColorBrush(Colors.Green);
            imgFailed.GridBrushes[64] = new SolidColorBrush(Colors.Yellow);
            imgFailed.GridBrushes[253] = new SolidColorBrush(Colors.Red);
            imgFailed.GridBrushes[254] = new SolidColorBrush(Colors.Red);
            imgFailed.GridBrushes[255] = new SolidColorBrush(Colors.White);
            imgFailed.InvalidateVisual();

            //imgFailed.Bytes = PageComponent.StrokeMatrix;
            //imgFailed.InvalidateVisual();
        }

        private void btnCreateFolders_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to create the folders?",
                                "Warning", MessageBoxButton.YesNo,
                                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                OCR.ShapeNet Member;
                int CreatedFolders;

                Member = (OCR.ShapeNet)lstNeuralNetworks.SelectedItem;
                CreatedFolders = 0;

                if (Member != null)
                {
                    foreach (ShapeListEntry Shape in Member.ShapeList)
                    {
                        if (!Directory.Exists(Shape.SampleFolder))
                        {
                            Directory.CreateDirectory(Shape.SampleFolder);
                            CreatedFolders++;
                        }
                    }
                }

                String Message;

                Message = CreatedFolders.ToString();
                Message += " folder(s) created.";

                MessageBox.Show(Message, "Message", MessageBoxButton.OK);
            }
        }

        private void btnBrowseShapeFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog oFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            oFolderDialog.Description = "Select a folder.";
            oFolderDialog.SelectedPath = txtShapeFolder.Text;

            oFolderDialog.ShowDialog();

            txtShapeFolder.Text = oFolderDialog.SelectedPath;
        }

        private void btnFilterOnShape_Click(object sender, RoutedEventArgs e)
        {
            if (lstShapes.SelectedIndex != -1)
            {
                ShapeListEntry Shape = (ShapeListEntry)lstShapes.SelectedItem;

                FillFileList(Shape.Shape);
            }
            else
            {
                MessageBox.Show("No shape is selected, filter cannot be applied.", "Error", MessageBoxButton.OK);
            }
        }

    }
}
