using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCR;
using NeuralNetwork;

namespace OCR
{
    
    class RecogniseComponent : WorkThread
    {
        public PageImage Image;

        public void Execute()
        {
            WorkPackage WorkPackage;
            int[] Assignment;

            Console.WriteLine("Execute " + this.GetType().ToString());

            NumberConnectedComponents = 0;

            for (int i = 0; i < ThreadCount; i++)
            {
                WorkPackage = new WorkPackage();
                WorkPackage.Method = ExecuteActivity;
                WorkPackage.Image = Image;

                Assignment = new int[2];
                Assignment[0] = i; //start index
                Assignment[1] = ThreadCount; //step size through list

                WorkPackage.Assignment = (object)Assignment;
                WorkPackage.Parameter = (object)0;
                RunPackage(WorkPackage);
            }

            WaitForWorkDone(this.GetType().ToString());
        
            //Add Statistics
            OCR.Statistics.AddCounter("Number connected components", NumberConnectedComponents);
        }

        public static int NumberConnectedComponents;
        
        /// <summary>
        /// This function process the workpackage for recognising the characters
        /// </summary>
        /// <param name="Parameter"></param>
        public static void ExecuteActivity(object Parameter)
        {
            //Load the neural network
            ShapeNeuralNetworkCollection ShapeNet;

            ShapeNeuralNetworkCollection SC = new ShapeNeuralNetworkCollection();
            ShapeNet = SC.Load("D:\\OCR\\Shapenet.xml");
                        
            //Initialize the workpackage
            WorkPackage WorkPackage = (WorkPackage)Parameter;
            int[] Assignment = (int[])WorkPackage.Assignment;

            //Run the package
            for (int index = Assignment[0];
                 index < WorkPackage.Image.Components.Count;
                 index += Assignment[1])
            {
                Recognise(ShapeNet.ShapeNets[2], WorkPackage.Image.Components[index]);
            }

            SignalWorkDone();
        }

        /// <summary>
        /// This functions 'recognises' the given compontent by running its features through
        /// a neural network.
        /// </summary>
        /// <param name="Component"></param>
        public static void Recognise(ShapeNet ShapeNet, PageComponent Component)
        {
            RecogniseWithoutConnectedRepair(ShapeNet, Component);

            Component.CheckAndRepairConnected(ShapeNet);
        }

        /// <summary>
        /// This functions 'recognises' the given compontent by running its features through
        /// a neural network.
        /// </summary>
        /// <param name="Component"></param>
        public static void RecogniseWithoutConnectedRepair(ShapeNet ShapeNet, PageComponent Component)
        {
            sNeuralOutput Result = new sNeuralOutput();
            ShapeNet.NeuralNetwork.ComputeOutput(RecogniseComponent.CalculateNetworkInput(Component), Result);

            for (int i = 0; i < Result.fOutputs.Count; i++)
            {
                if (Result.fOutputs[i] >= 0.01)
                {
                    Component.AddRecognitionResult(Result.fOutputs[i], ShapeNet.ShapeList[i].Shape);
                }
            }
        }

        /// <summary>
        /// This function creates/calculates the input for a neural network
        /// </summary>
        /// <param name="Component"></param>
        /// <returns></returns>
        public static sNeuralInput CalculateNetworkInput(PageComponent Component)
        {
            sNeuralInput Input;

            Input = new sNeuralInput();

            //Initialize input
            for (int index = 0; index < 48; index++)
            {
                Input.fInputs.Add(0);
            }

            //Add stroke direction
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Input.fInputs[i + 0] += Component.lStrokeDirectionX[i * 4 + j];
                    Input.fInputs[i + 8] += Component.lStrokeDirectionY[i * 4 + j];
                }
            }

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Input.fInputs[16 + i + j * 2] = Component.lStrokeMatrixNW[i, j];
                    Input.fInputs[20 + i + j * 2] = Component.lStrokeMatrixSW[i, j];
                }
            }

            int Pointer;
            //Add endpoints and junctions
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                Pointer = 24 + x + y * 3;
                Input.fInputs[Pointer] = Component.PixelTypeProjectionEndpoint[x, y];

                Pointer = 33 + x + y * 3;
                Input.fInputs[Pointer] = Component.PixelTypeProjectionJunction[x, y];
                }
            }

            //Add width/height ratio
            Input.fInputs[42] = ((double)Component.Width / (double)Component.Height);

            //Add position
            Input.fInputs[43] = Component.Position.Ascent  ? 1.0 : 0.0;
            Input.fInputs[44] = Component.Position.Height  ? 1.0 : 0.0;
            Input.fInputs[45] = Component.Position.Center  ? 1.0 : 0.0;
            Input.fInputs[46] = Component.Position.Base    ? 1.0 : 0.0;
            Input.fInputs[47] = Component.Position.Descent ? 1.0 : 0.0;

            return Input;
        }
    }
}
