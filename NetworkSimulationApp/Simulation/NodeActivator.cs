using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NetworkSimulationApp.Simulation
{
    internal static class NodeActivator
    {
        public static HashSet<int> WakeUpNodeList;
        private static volatile int _NodeNums;
        private static volatile bool _NodeDone;
        private static volatile int _NoChangeCounter;
        private static object _NodeNumsLock = new object();
        private static object _NodeDoneLock = new object();
        private static object _NoChangeCounterLock = new object();
        private static object _NodeRemoveLock = new object();
        private static object _SimWindow = null;
        private static decimal _TotalWakeUpCalls;
        private static DateTime StartTime,EndTime;
        public static Dictionary<int, int> NodeSplitBySourceLog;
        public static Dictionary<int, int> NodeSplitByTargetLog;
        public static float MaxFlow;
        public static float TotalNumberOfEdges;

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
            int length = 0;

            while (true)
            {
                if (NodeDone)
                {
                    NodeDone = false;
                    length = WakeUpNodeList.Count;
                    randNum = random.Next(0, length);
                    if (WakeUpNodeList.Count > 1)
                    {
                        NodeList.Nodes[WakeUpNodeList.ElementAt(randNum)].WakeUpCall = true;
                        _TotalWakeUpCalls++;
                    }
                    else
                    {
                        AdHocNetSimulation.SimWindow.WriteOutput("All nodes have been failed");
                        break;
                    }
                }
             /*   if (WakeUpNodeList.Count < 2)
                {
                    AdHocNetSimulation.SimWindow.WriteOutput("All nodes have been failed");
                    break;
                } */
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
                    NodeList.Nodes[WakeUpNodeList.ElementAt(i)].WakeUpCall = true;
                    i++;
                }
                if (i == WakeUpNodeList.Count || NoChangeCounter == 0)break;
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

        public static void RemoveNode(int nodeID)
        {
            lock (_NodeRemoveLock)
            {
                AdHocNetSimulation.SimWindow.WriteOutput("Node: " + nodeID + " has failed");
                if(WakeUpNodeList.Count > 1) WakeUpNodeList.Remove(nodeID);
            }
        }

        private static void FinalizeSimulation()
        {
            AdHocNetSimulation.SimWindow.WriteOutput("\n\n Equilibrium Reached");
            float TotalCurrFlow = 0;
            float FlowPercentage = 0;
            EndTime = DateTime.Now;
            TimeSpan time = EndTime.Subtract(StartTime);
            int totalTime = time.Milliseconds;
            int length = WakeUpNodeList.Count;
            int id = 0;

            for (int i = 0; i < length; i++)
            {
                float flow = NodeList.Nodes[WakeUpNodeList.ElementAt(i)].getTotalCurrentFlow();
                if(!float.IsNaN(flow)) TotalCurrFlow += flow;
                if (TotalCurrFlow < 0) TotalCurrFlow = 0;
            }

            FlowPercentage = (TotalCurrFlow / MaxFlow) * 100;

            foreach (KeyValuePair<int, int> pair in NodeSplitBySourceLog)
            {
                id = NodeList.Nodes[pair.Key].GraphID;
                AdHocNetSimulation.SimWindow.WriteOutput("Utility of " + id + "A is: " + NodeList.Nodes[pair.Key].getUtility());
                AdHocNetSimulation.SimWindow.WriteOutput("Utility of " + id + "B is: " + NodeList.Nodes[pair.Value].getUtility());
            }
            foreach (KeyValuePair<int, int> pair in NodeSplitByTargetLog)
            {
                id = NodeList.Nodes[pair.Key].GraphID;
                AdHocNetSimulation.SimWindow.WriteOutput("Utility of " + id + "A is: " + NodeList.Nodes[pair.Key].getUtility());
                AdHocNetSimulation.SimWindow.WriteOutput("Utility of " + id + "B is: " + NodeList.Nodes[pair.Value].getUtility());
            }
            
            AdHocNetSimulation.SimWindow.WriteOutput("");
            AdHocNetSimulation.SimWindow.WriteOutput("Time spent to reach Equilibrium: " + totalTime);
            AdHocNetSimulation.SimWindow.WriteOutput("Number of Nodes: " + NodeNums);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Number of Node WakeUp Calls: " + _TotalWakeUpCalls);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Demand Flow: " + MaxFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Current Flow: " + TotalCurrFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Flow percentage (Total Current Flow / Total Demand Flow): " + FlowPercentage);

            ((SimulationWin)_SimWindow).CancleThreads();
        }
        #region properties
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
        #endregion
    }
}
