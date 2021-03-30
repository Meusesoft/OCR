using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NeuralNetwork;

namespace OCR
{
    /// <summary>
    /// This class contains the position of the shape. It describes it as follows
    /// Ascent  --- xxxxx -----------
    ///             x
    /// Height  --- x ------  xxx  --
    ///             xxxxx    x   x
    /// Center  --- x ------ x   x --
    ///             x        x   x
    /// Base    --- xxxxx --  xxxx --
    ///                          x
    /// Descent ------------ xxxx ---
    /// </summary>
    public class ShapePosition
    {
        [XmlAttribute("Ascent")]
        public bool Ascent { get; set; }

        [XmlAttribute("Height")]
        public bool Height { get; set; }

        [XmlAttribute("Center")]
        public bool Center { get; set; }

        [XmlAttribute("Base")]
        public bool Base { get; set; }

        [XmlAttribute("Descent")]
        public bool Descent { get; set; }
    }
    
    public class ShapeListEntry
    {
        public ShapeListEntry()
        {
            Shape = "";
            SampleFolder = "";
            Position = new ShapePosition();
        }
        
        [XmlAttribute("Shape")]
        public String Shape { get; set; }

        [XmlAttribute("SampleFolder")]
        public String SampleFolder { get; set; }
        
        [XmlElement("Position")]
        public ShapePosition Position { get; set; }

        public int NumberItems
        {
            get
            {
                if (m_NumberItems == 0) CountNumberItems();
                
                return m_NumberItems;
            }
        }

        public void CountNumberItems()
        {
            try
            {
                string[] fileEntries = System.IO.Directory.GetFiles(SampleFolder, "*.bmp");

                m_NumberItems = fileEntries.Length;
            }
            catch (Exception)
            { }
        }

        private int m_NumberItems = 0;

       // [XmlAttribute("ShapeId")]
       // public int ShapeId { get; set; }
    }    
    
    public class ShapeNet
    {
        public ShapeNet()
        {
            Name = "";
            lNumberExamples = 0;
            SearchFolders = true;
            ShapeList = new List<ShapeListEntry>(0);
            NeuralNetwork = new NeuralNetwork.NeuralNetwork();
        }

        [XmlAttribute("Name")]
        public String Name { get; set; }

        [XmlAttribute("lNumberExamples")]
        public int lNumberExamples { get; set; }

        [XmlAttribute("bSearchFolders")]
        public bool SearchFolders { get; set; }

        public List<ShapeListEntry> ShapeList;

        public NeuralNetwork.NeuralNetwork NeuralNetwork;
    }

    public class ShapeNeuralNetworkCollection
    {
        public ShapeNeuralNetworkCollection()
        {
            ShapeNets = new List<ShapeNet>(0);

            //ShapeNet SN = new ShapeNet();
            //ShapeNets.Add(SN);
        }

        public List<ShapeNet> ShapeNets;

        public ShapeNeuralNetworkCollection Load(string Filename)
        {
            ShapeNeuralNetworkCollection newShapeNets;

            XmlSerializer s = new XmlSerializer(this.GetType());
            TextReader r = new StreamReader(Filename);
            newShapeNets = (ShapeNeuralNetworkCollection)s.Deserialize(r);
            r.Close();

            return newShapeNets;
        }

        public void Save(string Filename)
        {
            StreamWriter w = new StreamWriter(Filename);
            XmlSerializer s = new XmlSerializer(this.GetType());
            s.Serialize(w, this);
            w.Close();
        }
    }
}
