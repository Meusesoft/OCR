using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace NeuralNetwork
{
    public class NeuralNetwork 
    {
        public NeuralNetwork() 
        {
            Nodes = new List<Node>(0);
            InputNodes = new List<Node>(0);
            OutputNodes =new List<Node>(0);
            oLearningData =new List<LearningData>(0);
        }
    
        public void ClearNetwork() 
        {
            //clear the vector of pointers to the input nodes
            InputNodes.Clear();
            OutputNodes.Clear();

            //delete all nodes from the network and clear the
            //vector
            Nodes.Clear();
            }

        /// <summary>
        /// Builds the neural network from a XML file.
        /// </summary>
        public NeuralNetwork LoadXML(String Filename) 
        {
            NeuralNetwork newNeuralNetwork;

            XmlSerializer s = new XmlSerializer(this.GetType());
            TextReader r = new StreamReader(Filename);
            newNeuralNetwork = (NeuralNetwork)s.Deserialize(r);
            r.Close();

            return newNeuralNetwork;
            }

        public void SaveXML(String Filename) 
        {
            StreamWriter w = new StreamWriter(Filename);
            XmlSerializer s = new XmlSerializer(this.GetType());
            s.Serialize(w, this);
            w.Close();
            }

        public void ClearSituations() 
        {
            oLearningData.Clear();
        }

        public void AddSituation(LearningData poLearningData) 
        {
            oLearningData.Add(poLearningData);
        }

        public void ComputeInputRatios() 
        {
            List<double>fMax;

            fMax = new List<double>(0);

            //init the values of the vector, set them all to 1.
            for (int lIndex=0; lIndex<(int)InputNodes.Count; lIndex++) {

                fMax.Add(1);
                }

            //determine the maximum values in the learning data per input node
            //to do: we assume the oInputs vector in the learning data is as
            //large as the number of input nodes.
            for (int lIndex=0; lIndex<(int)oLearningData.Count; lIndex++) {

                for (int lIndex2=0; lIndex2<(int)InputNodes.Count; lIndex2++) {

                    if (oLearningData[lIndex].oInput.fInputs[lIndex2] > fMax[lIndex2]) {

                        fMax[lIndex2] = oLearningData[lIndex].oInput.fInputs[lIndex2];
                        }
                    }
                }

            //set the maximum allowed value per input node
            for (int lIndex=0; lIndex<(int)InputNodes.Count; lIndex++) {

                InputNodes[lIndex].InputMax = fMax[lIndex];
                }
            }

        public void AddNode(int plLayer, eNeuralNodeType peType) 
        {

            Node oNode;
            Connection oConnection;
            int lNodeIndex;

            //create node
            oNode = new Node(peType, plLayer);
            oNode.Treshold = 0;
            oNode.Id = Nodes.Count;
            Nodes.Add(oNode);

            switch (peType) 
            {
                case eNeuralNodeType.eInput: 
                    {
                    InputNodes.Add(oNode);
                    break;
                    }

                case eNeuralNodeType.eOutput:
                    {
                    OutputNodes.Add(oNode);
                    break;
                    }
                }

            lNodeIndex = Nodes.Count-1;

            //create connections to all nodes in previous layer
            for (int lIndex=0; lIndex<(int)Nodes.Count; lIndex++) {

                if (Nodes[lIndex].lLayer == plLayer-1) {

                    oConnection = new Connection(lIndex, lNodeIndex);

                    oConnection.fWeight = 0;

                    oNode.Connections.Add(oConnection);
                    }
                }

            //create connections to all nodes in next layer
            foreach (Node Node in Nodes)

                if (Node.lLayer == plLayer+1) 
                {
                    oConnection = new Connection(lNodeIndex, Node.Id);

                    oConnection.fWeight = 0;

                    Node.Connections.Add(oConnection);
                }
            }

        public void InitNetworkForLearning() 
        {

            //fill the network with random values as a starting
            //point for 'learning'
            foreach (Node Node in Nodes)
            {
                Node.Treshold = 0.5 - (((double)_r.Next(1000)) / 1000);

                foreach (Connection Connection in Node.Connections) 
                {
                    Connection.fWeight = 0.5 - (((double)_r.Next(1000)) / 1000);
                }
            }
        }

        public void Train(int piRepetitions)
        {
            StartTraining();

            TrainingExecuteRepetition(piRepetitions);
        }
        
        public void StartTraining()
        {
            InitNetworkForLearning();
        }
            
        public double TrainingExecuteRepetition(int piRepetitions) 
        {
            int lRepetition;
            int lLayer;
            int lCounter;
            List<double> oTotalErrors = new List<double>(0);
            sNeuralOutput oOutput;
            int lExample;
            //double fError;
            //double fTotalError;

            //int iFileHandle;
            //int iFileHandle2;

            lCounter = 0;
            oOutput = new sNeuralOutput();

            //start learning by back-propagation
            lRepetition = 0;

            double fGoodError = 0;
            int  lGood = 0;
            double fFaultError = 0;
            int  lFault = 0;

            if (oLearningData.Count>0) {

                do {

                    fGoodError = 0;
                    lGood = 0;
                    fFaultError = 0;
                    lFault = 0;

                    //fTotalError = 0;

                    //Randomize();
                    for (int lIndex=0; lIndex<(int)oLearningData.Count; lIndex++) {

                        //get an example from the learning vector
                        lExample = _r.Next(oLearningData.Count-1);

                        //compute the value suggested by the network
                        ComputeOutput(oLearningData[lExample].oInput, oOutput);

                        //make sure the outputs (desired and computed) are the same size
                        if (oOutput.fOutputs.Count != oLearningData[lExample].oOutput.fOutputs.Count) {
                            throw new ApplicationException("Desired and computed output differ in size. Cannot compare.");
                            }

                        //compute error
                        for (int lIndex2=0; lIndex2<(int)oOutput.fOutputs.Count; lIndex2++) {

                            if (oLearningData[lExample].oOutput.fOutputs[lIndex2] > 0.5) {

                                fGoodError += System.Math.Abs(oOutput.fOutputs[lIndex2] - 1);
                                lGood++;
                                }
                            else {

                                fFaultError += System.Math.Abs(oOutput.fOutputs[lIndex2]);
                                lFault++;
                                }

                            //fError += fabs(oOutput.fOutputs[lIndex2] - oLearningData[lIndex].oOutput.fOutputs[lIndex2]);
                            }
                        
                        //fTotalError += fError;

                        //determine the number of the output layer in the network
                        lLayer =  OutputNodes[0].lLayer;

                        //set cumulative error to 0 in all nodes
                        for (int lIndex2=0; lIndex2<(int)Nodes.Count; lIndex2++) {

                            Nodes[lIndex2].SetCumulativeErrorDelta(0);
                            }

                        //do the back propagation
                        //start with output layer first
                        for (int lIndex2=0; lIndex2<(int)OutputNodes.Count; lIndex2++) {

                            LearnUpdateWeights(OutputNodes[lIndex2].Id, oLearningData[lExample].oOutput.fOutputs[lIndex2], oOutput.fOutputs[lIndex2], (double)0.50);
                            }

                        //followed by the other nodes
                        lLayer--;

                        do {

                            for (int lIndex2=0; lIndex2<(int)Nodes.Count; lIndex2++) 
                            {
                                if (Nodes[lIndex2].lLayer==lLayer/* && oOutputNodes[0].lLayer*/) 
                                {
                                    LearnUpdateWeights(lIndex2, 0, 0, (double)0.50); //the outputs don't matter for the hidden layers, therefor they are set to 0
                                }
                            }

                       lLayer--;

                       } while (lLayer>0);
                   }

                    /*oTotalErrors.Add(fTotalError);

                    fAverage = 0.6;

                    if (oTotalErrors.Count > 11) {

                        //compute average difference of last 10 repetitions
                        fAverage = 0;
                        for (int lIndex = oTotalErrors.Count-11; lIndex < oTotalErrors.Count-1; lIndex++) {

                            fAverage += fabs(oTotalErrors[lIndex] - oTotalErrors[lIndex+1]);
                            }

                        fAverage = fAverage / 10;
                        }  */

                     lRepetition++;

                     fFaultError = fFaultError / (double)lFault;
                     fGoodError = fGoodError / (double)lGood;

                     lCounter++;

                    //} while (/*fAverage > 0.005 && */lRepetition<450);
                    } while (lRepetition<piRepetitions); // || (fabs(fFaultError-fGoodError)>0.3 && lRepetition<2000));
                }

            return ComputeSuccessPercentage(0.2);
            }

        public double ComputeSuccessPercentage(double pfTreshold) 
        {

        //    double fOutput;
            double fReturn;
	        bool bGood;
            int lGood;
            int lGoodExamples;
            sNeuralOutput oOutput;

            lGood = lGoodExamples = 0;
            fReturn = 0;
            oOutput = new sNeuralOutput();

            //loop through the examples
            for (int lIndex=0; lIndex<(int)oLearningData.Count; lIndex++) {

                ComputeOutput(oLearningData[lIndex].oInput, oOutput);

		        if (oOutput.fOutputs.Count == oLearningData[lIndex].oOutput.fOutputs.Count) {

                    lGoodExamples++;
			        bGood = true; 

			        for (int lOutputIndex=0; bGood && lOutputIndex<(int)oLearningData[lIndex].oOutput.fOutputs.Count; lOutputIndex++) {
        			
				        if (System.Math.Abs(oLearningData[lIndex].oOutput.fOutputs[lOutputIndex] - oOutput.fOutputs[lOutputIndex])>pfTreshold) {
					        bGood = false;
					        }
                        }

			        if (bGood) lGood++;
                    }
                }

            if (lGoodExamples>0) {

                fReturn = (double)((lGood * 100) / lGoodExamples);
                }

            return fReturn;
            }

        public void InitialiseNetwork()
        {
            //Clear all the nodes
            foreach (Node Node in Nodes)
            {
                Node.ClearValue();
            }


            //Check if we have 'shortcuts' to the input and output nodes
            if (InputNodes.Count == 0 || OutputNodes.Count == 0)
            {
                InputNodes.Clear();
                OutputNodes.Clear();

                foreach (Node Node in Nodes)
                {
                    switch (Node.Type)
                    {
                        case eNeuralNodeType.eInput:
                            InputNodes.Add(Node);
                            break;

                        case eNeuralNodeType.eOutput:
                            OutputNodes.Add(Node);
                            break;
                    }
                }
            }


        }

        public void ComputeOutput(sNeuralInput oInput, sNeuralOutput oOutput) 
        {

            //clear the output structure
            oOutput.fOutputs.Clear();

            //init network
            InitialiseNetwork();

            //fill input nodes
            for (int lIndex=0; lIndex<(int)oInput.fInputs.Count && lIndex<(int)InputNodes.Count; lIndex++) {

                InputNodes[lIndex].SetValue(oInput.fInputs[lIndex]);
                }

            //compute output nodes
            foreach (Node Node in OutputNodes)
            {
                oOutput.fOutputs.Add(ComputeNode(Node.Id));
            }
         }

        public double ComputeNode(int plNode) 
        {

            double fValue;
	        Node oNode;

            if (!Nodes[plNode].IsCalculated()) {

		        oNode = Nodes[plNode];

                fValue = (double)(oNode.Treshold * -1);

                //loop through the connection list and get the values of all nodes before
                //the current one.
                foreach (Connection Connection in oNode.Connections)
                {
                    if (Connection.lEndNode == oNode.Id)
                    {

                        //Calculate the input value of this node by summing all output values of previous
                        //nodes multiplied by the weight of the connection.
                        fValue = fValue + Connection.fWeight * ComputeNode(Connection.lStartNode);
                    }
                }

                oNode.SetValue(fValue);

                //On other nodes then the input and output nodes apply an
                //activation function. In this case it is a step function, meaning
                //that if a certain treshold is crossed it will output 1, else
                //it will output zero.
                if (oNode.Type == eNeuralNodeType.eHidden) {
                    oNode.SetValue(Sigmoid(0, fValue));
                    }

                if (oNode.Type == eNeuralNodeType.eOutput)
                {
                    oNode.SetValue(Sigmoid(0, fValue));
                    }
                }

            //return the output of the node
            return (double)Nodes[plNode].Value;
            }

        public double Sigmoid(double pfTreshold, double pfInput) 
        {

            double fValue;

            fValue = 1 / (1 + Math.Exp(-(pfInput - pfTreshold)));

            return fValue;
            }

        public void LearnUpdateWeights(int plNode, double pfDesired, double pfOutput, double pfLearnRate) 
        {

            double fWeightDelta;
            double fErrorDelta;
	        Node oNode;

            fWeightDelta = 0;
            fErrorDelta = 1;
	        oNode = Nodes[plNode];

            foreach (Connection Connection in oNode.Connections)
            {
                if (Connection.lEndNode == oNode.Id)
                {

                    if (oNode.Type == eNeuralNodeType.eHidden)
                    {
                        fErrorDelta = oNode.Value * (1 - oNode.Value) * oNode.CumulativeErrorDelta;
                        Nodes[Connection.lStartNode].AddToCumulativeErrorDelta(fErrorDelta * Connection.fWeight);
                    }

                    if (oNode.Type == eNeuralNodeType.eOutput)
                    {
                        fErrorDelta = pfOutput * (1 - pfOutput) * (pfDesired - pfOutput);
                        Nodes[Connection.lStartNode].AddToCumulativeErrorDelta(fErrorDelta * Connection.fWeight);
                    }

                    fWeightDelta = pfLearnRate * Nodes[Connection.lStartNode].Value * fErrorDelta;

                    Connection.fWeight += fWeightDelta;
                }
            }

            //adjust treshold, the treshold is seen as another connection with a constant input of -1. The weight
            //of the connection will be the treshold value.

            if (oNode.Type == eNeuralNodeType.eHidden)
            {
                fErrorDelta = oNode.Value * (1- oNode.Value) * oNode.CumulativeErrorDelta;
            }

            if (oNode.Type == eNeuralNodeType.eOutput)
            {
                fErrorDelta = pfOutput*(1-pfOutput)*(pfDesired-pfOutput);
            }

            fWeightDelta = pfLearnRate * -1 * fErrorDelta;
            oNode.Treshold += fWeightDelta;
            }

        private List<LearningData> oLearningData;
        
        public List<Node> Nodes;

        private List<Node> InputNodes;
        private List<Node> OutputNodes;

        static Random _r = new Random();

        public int Repetitions {get; set;}
    }
}
