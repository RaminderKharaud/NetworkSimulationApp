using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NetworkSimulationApp.Simulation
{
    static class NodeActivator
    {
        private static volatile int _NodeNums;
        private static volatile bool _NodeDone;
        private static volatile int _NoChangeCounter;
        private static object _NodeNumsLock = new object();
        private static object _NodeDoneLock = new object();
        private static object _NoChangeCounterLock = new object();

        public static void Start(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            NodeDone = true;
            Random random = new Random();
            int randNum = 0;
            bool NashEquilibrium = false;
            Thread.Sleep(100);

            while (true)
            {
                if (NodeDone)
                {
                    NodeDone = false;
                    randNum = random.Next(0, NodeNums);
                    NodeList.Nodes[randNum].WakeUpCall = true;
                }
                if (NoChangeCounter > NodeNums / 2)
                {
                    NashEquilibrium = RoundCheck();
                    if (NashEquilibrium) break;
                  
                }
                if (token.IsCancellationRequested)
                {
                    break;
                }
                Thread.Sleep(10);
            }
            if (NashEquilibrium) FinalizeSimulation();
        }

        private static bool RoundCheck()
        {
            int i = 0;
            NodeDone = true;
            while (true)
            {
                if (NodeDone)
                {
                    NodeDone = false;
                    NodeList.Nodes[i].WakeUpCall = true;
                    i++;
                }
                if (i >= NodeNums || NoChangeCounter == 0)break;
            }

            if (NoChangeCounter == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
           
        }
        private static void FinalizeSimulation()
        {
        }

        public static int NodeNums
        {
            get
            {
                lock (_NodeNumsLock)
                {
                    return _NodeNums;
                }
            }
            set
            {
                lock (_NodeNumsLock)
                {
                    _NodeNums = value;
                }
            }
        }
        public static int NoChangeCounter
        {
            get
            {
                lock (_NoChangeCounterLock)
                {
                    return _NoChangeCounter;
                }
            }
            set
            {
                lock (_NoChangeCounterLock)
                {
                    _NoChangeCounter = value;
                }
            }
        }
        public static bool NodeDone
        {
            get
            {
                lock (_NodeDoneLock)
                {
                    return _NodeDone;
                }
            }
            set
            {
                lock (_NodeDoneLock)
                {
                    _NodeDone = value;
                }
            }
        }
    }
}
