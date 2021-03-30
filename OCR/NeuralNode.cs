using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NeuralNetwork
{
    public enum eNeuralNodeType {eInput=0, eOutput=1, eHidden=2};

    public class Node
    {
        public Node(eNeuralNodeType peType, int plLayer)
        {
            m_Type = peType;
            m_Layer = plLayer;
            m_Calculated = false;
            m_Value = 0;
            m_CumulativeErrorDelta = 0;

            Treshold = 0;
            InputMax = 0;
            Id = 0;

            Connections = new List<Connection>(0);
        }

        public Node()
        {
            m_Type = 0;
            m_Layer = 0;
            m_Calculated = false;
            m_Value = 0;
            m_CumulativeErrorDelta = 0;

            Treshold = 0;
            InputMax = 0;
            Id = 0;

            Connections = new List<Connection>(0);
        }

        [XmlAttribute("InputMax")]
        public double InputMax { get; set; }

        [XmlAttribute("Treshold")]
        public double Treshold { get; set; }

        [XmlAttribute("Type")]
        public eNeuralNodeType Type 
        { 
            get {return m_Type;}
            set { m_Type = value; } 
        }

        [XmlAttribute("Layer")]
        public int lLayer { 
            get { return m_Layer;} 
            set { m_Layer = value;}
        }

        public List<Connection> Connections;

        [XmlAttribute("Id")]
        public int Id { get; set; }

        /// <summary>
        /// This function sets the cumulative error of this node. The cumulative error value is used in the learning sequence
        /// </summary>
        /// <param name="Value"></param>
        public void SetCumulativeErrorDelta(double Value)
        {
            m_CumulativeErrorDelta = Value;
        }

        /// <summary>
        /// This function adds the value to the current value of the cumulative error
        /// </summary>
        /// <param name="Value"></param>
        public void AddToCumulativeErrorDelta(double Value)
        {
            m_CumulativeErrorDelta += Value;
        }

        /// <summary>
        /// The current value of the cumulative error. This is used while training the network
        /// </summary>
        public double CumulativeErrorDelta
        {
            get { return m_CumulativeErrorDelta; }
        }

        /// <summary>
        /// This function clears the calculated bit of this node
        /// </summary>
        public void ClearValue()
        {
            m_Calculated = false;
        }

        /// <summary>
        /// This function returns true if this node has been calculated already
        /// </summary>
        /// <returns></returns>
        public bool IsCalculated()
        {
            return m_Calculated;
        }

        /// <summary>
        /// Returns the value of this node
        /// </summary>
        public double Value
        {
            get
            {
                return m_Value;
            }
        }

        /// <summary>
        /// Sets the value of this node
        /// </summary>
        /// <param name="Value"></param>
        public void SetValue(double Value)
        {
            m_Calculated = true;

            if (InputMax == 0)
            {
                m_Value = Value;
            }
            else
            {
                m_Value = Value / InputMax;
            }
        }

        private double m_CumulativeErrorDelta;
        private bool m_Calculated;
        private eNeuralNodeType m_Type;
        private int m_Layer;
        private double m_Value;
    }
}
