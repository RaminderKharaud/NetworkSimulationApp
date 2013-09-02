using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.NonThreadSimulation
{
    internal static class NodeActivator
    {
        private static volatile int _NodeNums;
        private static volatile bool _NodeDone;
        private static volatile int _NoChangeCounter;
        private static object _NodeNumsLock = new object();
        private static object _NodeDoneLock = new object();
        private static object _NoChangeCounterLock = new object();
        private static object _SimWindow = null;
        public static decimal TotalWakeUpCalls;
        public static DateTime StartTime, EndTime;
        public static float MaxFlow;
        public static float TotalNumberOfEdges;
        public static AdHocNetSimulation simWin;
        public static Dictionary<int, int> NodeSplitBySourceLog;
        public static Dictionary<int, int> NodeSplitByTargetLog;
        public static HashSet<int> WakeUpNodeList;
        public static double NodeFailureRate;

        public static bool loopWakeUpCall(int i)
        {
            bool NashEquilibrium = false;
            int NodeId = WakeUpNodeList.ElementAt(i);
            NodeActivator.TotalWakeUpCalls++;

            NodeList.Nodes[NodeId].NodeStrategy();
            FlowProcessRoutine(NodeId);

            if (NoChangeCounter > NodeNums)
            {
                NashEquilibrium = LoopRoundCheck();
                if (NashEquilibrium)
                {
                    FinalizeSimulation(false);
                    return true;
                }
            }
            
            return false;
        }
        private static bool LoopRoundCheck()
        {
            for (int i = 0; i < WakeUpNodeList.Count; i++)
            {
                NodeActivator.TotalWakeUpCalls++;
                NodeList.Nodes[WakeUpNodeList.ElementAt(i)].NodeStrategy();
                FlowProcessRoutine(WakeUpNodeList.ElementAt(i));
                if (NoChangeCounter == 0) break;
            }
            if (NoChangeCounter == 0)
            {
                return false;
            }

            return true;
        }

        private static void FlowProcessRoutine(int nodeID)
        {
            int nextID = 0, origin = 0, destination = 0;
            string[] IDs = null;
            try
            {
                foreach (int i in NodeList.Nodes[nodeID].MyOrigins)
                {
                    nextID = i;
                    while (nextID != nodeID)
                    {
                        NodeList.Nodes[nextID].FlowReciever();
                        nextID = NodeList.Nodes[nextID].ForwardingTable[nodeID];
                    }
                }

                NodeList.Nodes[nodeID].FlowReciever();

                foreach (int dest in NodeList.Nodes[nodeID].MyDestinationsAndDemands.Keys)
                {
                    nextID = nodeID;
                    while (nextID != dest)
                    {
                        nextID = NodeList.Nodes[nextID].ForwardingTable[dest];
                        NodeList.Nodes[nextID].FlowReciever();
                    }
                }
                foreach (string commodity in NodeList.Nodes[nodeID].MyEnvolvedments)
                {
                    IDs = commodity.Split(':');
                    origin = int.Parse(IDs[0]);
                    destination = int.Parse(IDs[1]);
                    nextID = origin;
                    while (nextID != destination)
                    {
                        NodeList.Nodes[nextID].FlowReciever();
                        nextID = NodeList.Nodes[nextID].ForwardingTable[destination];
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("\nProblem in Process routine\n");
            }
        }


        public static void RemoveNode(int nodeID)
        {
            Console.WriteLine("Node: " + nodeID + " has failed");
            WakeUpNodeList.Remove(nodeID);
        }

        private static void FinalizeSimulation(bool threads)
        {
            Console.WriteLine("\n\n Equilibrium Reached");
            float TotalCurrFlow = 0;
            float TotalEdgesAlive = 0;
            float EdgePercentage = 0;
            float FlowPercentage = 0;
            int id = 0;
            EndTime = DateTime.Now;

            for (int i = 0; i < WakeUpNodeList.Count; i++)
            {
                float flow = NodeList.Nodes[WakeUpNodeList.ElementAt(i)].getTotalCurrentFlow();
                if (!float.IsNaN(flow)) TotalCurrFlow += flow;
                if (TotalCurrFlow < 0) TotalCurrFlow = 0;
            }

            FlowPercentage = (TotalCurrFlow / MaxFlow) * 100;
            EdgePercentage = (TotalEdgesAlive / TotalNumberOfEdges) * 100;
            TimeSpan time = EndTime.Subtract(StartTime);
            int totalTime = time.Milliseconds;

           /* foreach(KeyValuePair<int,int> pair in NodeSplitBySourceLog)
            {
                id = NodeList.Nodes[pair.Key].GraphID;
                Console.WriteLine("Utility of " + id + "A is: " + NodeList.Nodes[pair.Key].getUtility());
                Console.WriteLine("Utility of " + id + "B is: " + NodeList.Nodes[pair.Value].getUtility());
            }
            foreach (KeyValuePair<int, int> pair in NodeSplitByTargetLog)
            {
                id = NodeList.Nodes[pair.Key].GraphID;
                Console.WriteLine("Utility of " + id + "A is: " + NodeList.Nodes[pair.Key].getUtility());
                Console.WriteLine("Utility of " + id + "B is: " + NodeList.Nodes[pair.Value].getUtility());
            } */

            Console.WriteLine("");
            Console.WriteLine("Time spent to reach Equilibrium: " + totalTime);
            Console.WriteLine("Number of Nodes: " + NodeNums);
            Console.WriteLine("Total Number of Node WakeUp Calls: " + NodeActivator.TotalWakeUpCalls);
            Console.WriteLine("Total Demand Flow: " + MaxFlow);
            Console.WriteLine("Total Current Flow: " + TotalCurrFlow);
            Console.WriteLine("Flow percentage (Total Current Flow / Total Demand Flow): " + FlowPercentage);

        }

        #region Properties
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
