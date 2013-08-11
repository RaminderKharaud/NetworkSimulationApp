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
        private static object _SimWindow = null;
        private static decimal _TotalWakeUpCalls;
        private static DateTime StartTime,EndTime;
        public static double MaxFlow;
        public static double TotalNumberOfEdges;
        public static void Start(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            NodeDone = true;
            Random random = new Random();
            int randNum = 0;
            bool NashEquilibrium = false;
            Thread.Sleep(100);
            _TotalWakeUpCalls = 0;
            StartTime = DateTime.Now;
            while (true)
            {
                if (NodeDone)
                {
                    NodeDone = false;
                    randNum = random.Next(0, NodeNums);
                    NodeList.Nodes[randNum].WakeUpCall = true;
                    _TotalWakeUpCalls++;
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
            Console.WriteLine("Equilibrium Reached");
            double TotalCurrFlow = 0;
            double TotalEdgesAlive = 0;
            double EdgePercentage = 0;
            double FlowPercentage = 0;
            EndTime = DateTime.Now;
            TimeSpan time = EndTime.Subtract(StartTime);
            int totalTime = time.Milliseconds;
            for (int i = 0; i < NodeNums; i++)
            {
              //  Console.WriteLine(i + " : Final Utility is: " + NodeList.Nodes[i].CurrUtility);
                TotalCurrFlow += NodeList.Nodes[i].getTotalCurrentFlow();
                TotalEdgesAlive += NodeList.Nodes[i].getNumberOfEdgesAlive();
            }

            FlowPercentage = (TotalCurrFlow / MaxFlow) * 100;
            EdgePercentage = (TotalEdgesAlive / TotalNumberOfEdges) * 100;

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Time spent to reach Equilibrium: " + totalTime);
            Console.WriteLine("Number of Nodes: " + NodeNums);
            Console.WriteLine("Total Number of Node WakeUp Calls: " + _TotalWakeUpCalls);
            Console.WriteLine("Total Demand Flow: " + MaxFlow);
            Console.WriteLine("Total Current Flow: " + TotalCurrFlow);
            Console.WriteLine("Flow percentage (Total Current Flow / Total Demand Flow): " + FlowPercentage);
            Console.WriteLine();
            Console.WriteLine();

         //   Console.WriteLine("Edge percentage: " + EdgePercentage);
            ((SimulationWin)_SimWindow).CancleThreads();
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
        public static object SimWindow
        {
            set
            {
                _SimWindow = value;
            }
        }
    }
}
