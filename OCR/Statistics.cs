using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCR
{
    class Statistics
    {
        public void Clear() 
        {
            m_Duration = "";
            m_Counters = "";
        }
        
        public void AddDuration(String Description, int Duration)
        {
            m_Duration += Description + ": " + Duration + " ms\n";
        }

        public void AddCounter(String Description, int Number)
        {
            m_Counters += Description + ": " + Number + "\n";
        }

        public String Durations
        {
            get { return m_Duration; }

        }

        public String Counters
        {
            get { return m_Counters;}
        }

        String m_Duration;
        String m_Counters;
    }
}
