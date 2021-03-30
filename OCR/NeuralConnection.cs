using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NeuralNetwork
{
    public class Connection
    {
        public Connection()
        {
            lStartNode = 0;
            lEndNode = 0;

            fWeight = 0;
            fLastError = 0;
        }

        public Connection(int plStartNode, int plEndNode)
        {
            lStartNode = plStartNode;
            lEndNode = plEndNode;

            fWeight = 0;
            fLastError = 0;
        }

        [XmlAttribute("Start")]
        public int lStartNode { get; set; }

        [XmlAttribute("End")]
        public int lEndNode { get; set; }

        [XmlAttribute("Weight")]
        public double fWeight { get; set; }

        private double fLastError { get; set; }
    }
}
