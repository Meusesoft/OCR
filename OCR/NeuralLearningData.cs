using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuralNetwork
{
    public class sNeuralInput
    {
        public sNeuralInput()
        {
            fInputs = new List<double>(0);
        }
        
        public List<double> fInputs;
    }

    public class sNeuralOutput
    {
        public sNeuralOutput()
        {
            fOutputs = new List<double>(0);
        }

        public List<double> fOutputs;
    }
    
    public class LearningData
    {
        public LearningData()
        {
            oInput = new sNeuralInput();
            oOutput = new sNeuralOutput();
        }
        
        public sNeuralInput oInput;
        public sNeuralOutput oOutput;
    }
}
