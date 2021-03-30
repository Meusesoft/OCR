using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;

namespace OCR
{
    class WorkThread
    {
        protected class WorkPackage
        {
            public PageImage Image;
            public object Assignment;
            public object Parameter;
            public WaitCallback Method;
        }

        static object WorkPackageLocker = new object();
        static int WorkPackagesActive = 0;

        private int StartTime = 0;
        private int m_ThreadCount;

        public WorkThread()
        {

            m_ThreadCount = ProcessorCount;
            //m_ThreadCount = 16;
            if (m_ThreadCount > 32) m_ThreadCount = 32; //maximum thread count at the moment, see detect components
        }
        
        public int ThreadCount
        {
            get
            {
                return m_ThreadCount;
            }
        }


        public int ProcessorCount
        {
            get
            {
                int m_ProcessorCount;

                m_ProcessorCount = System.Environment.ProcessorCount;
                if (m_ProcessorCount > OCR.MaximumThreadCount)
                {
                    m_ProcessorCount = OCR.MaximumThreadCount;
                }

                return m_ProcessorCount;
            }
        }

        protected void RunPackage(object Package)
        {
            
            lock (WorkPackageLocker) 
            {

                if (WorkPackagesActive == 0) StartTimer();
                WorkPackagesActive++; 
            }

            ThreadPool.QueueUserWorkItem(((WorkPackage)Package).Method, Package);
        }


        protected void WaitForWorkDone(String WhoIsWaiting)
        {
            lock (WorkPackageLocker)
            {

                while (WorkPackagesActive > 0) Monitor.Wait(WorkPackageLocker);
            };

            MeasureTime(WhoIsWaiting);
        }

        protected static void SignalWorkDone()
        {
            lock (WorkPackageLocker)
            {
                WorkPackagesActive--;
                Monitor.Pulse(WorkPackageLocker);
            }
        }

        protected void StartTimer()
        {
            StartTime = System.Environment.TickCount;
        }

        protected void MeasureTime(String Measure)
        {
            OCR.Statistics.AddDuration(Measure, System.Environment.TickCount - StartTime);
        }
    }
}
