using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// File:                   NodeActivator.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       June 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                NodeActivator activates nodes randomly to update their strategy. It also
    ///                         has finalize method that gets the result of simulation
    /// </summary>
    internal static class NodeActivator
    {
        /// <summary>
        /// wake up node list contains all the nodes that are involved in commodities
        /// this list gets updeted when a node fails
        /// </summary>
        public static HashSet<int> WakeUpNodeList;
        private static volatile int _NodeNums; //total number of nodes in the network
        private static volatile bool _NodeDone; 
        private static volatile int _NoChangeCounter;
        private static object _NodeNumsLock = new object();
        private static object _NodeDoneLock = new object();
        private static object _NoChangeCounterLock = new object();
        private static object _NodeRemoveLock = new object();
        private static object _FailNumLock = new object();
        private static object _SimWindow = null;
        private static decimal _TotalWakeUpCalls;
        private static int _FailNum;
        private static DateTime StartTime,EndTime;
        public static Dictionary<int, int> NodeSplitBySourceLog; //list of nodes splitted by source edges
        public static Dictionary<int, int> NodeSplitByTargetLog; //list of nodes splitted by target edges
        public static int EdgeNums;
        public static int CommNums;
        public static float MaxFlow;
        public static float SingleEdgeFlow;
        public static float TotalNumberOfEdges;
        private static object _CancelLock = new object();
        private static bool _Cancel = false;
        /// <summary>
        /// This method runs until equilibrium is not reached and wake up nodes 
        /// randomly. when more than 50% node that it wakes up dont change their strategy
        /// it runs a roundcheck to see if equilibiurm has reached or not
        /// </summary>
        /// <param name="obj"></param>
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
                /*  if (NodeDone)
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
              } */
                NashEquilibrium = RoundCheck();
                if (NashEquilibrium) break;
            }

            if (NashEquilibrium) FinalizeSimulation();
        }
        /// <summary>
        /// check all the nodes turn by turn to see if no node changes
        /// its strategy. If any one node changes its strategy, the check fails
        /// and returns false;
        /// </summary>
        /// <returns></returns>
        private static bool RoundCheck()
        {
            NodeDone = true;
            NoChangeCounter = 1;
           /* while (true)
            {
                if (NodeDone)
                {
                    NodeDone = false;
                    NodeList.Nodes[WakeUpNodeList.ElementAt(i)].WakeUpCall = true;
                    i++;
                }
                if (i == WakeUpNodeList.Count || NoChangeCounter == 0)break;
            } */
            _TotalWakeUpCalls++;
            for (int i = 0; i < NodeList.Nodes.Count; i++)
            {
                if (!NodeList.Nodes[i].NodeStrategy())
                {
                    NoChangeCounter = 0;
                    break;
                }
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
        /// <summary>
        /// remove the node from list of wake up nodes. 
        /// this method is called by the node which is going to fail
        /// </summary>
        /// <param name="nodeID"></param>
        public static void RemoveNode(int nodeID)
        {
            lock (_NodeRemoveLock)
            {
                AdHocNetSimulation.SimWindow.WriteOutput("Node: " + nodeID + " has failed");
                if(WakeUpNodeList.Count > 1) WakeUpNodeList.Remove(nodeID);
            }
        }
        /// <summary>
        /// This method calculates all the final values of simulation and send information
        /// to simulation window before it stop all the threads
        /// </summary>
        private static void FinalizeSimulation()
        {
            AdHocNetSimulation.SimWindow.WriteOutput("\n\n Equilibrium Reached");
            float TotalCurrFlow = 0;
            float FlowPercentage = 0;
            float NonTrivialFlowPercentage = 0;
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
            if (TotalCurrFlow - SingleEdgeFlow > 0)
            {
                NonTrivialFlowPercentage = ((TotalCurrFlow - SingleEdgeFlow) / MaxFlow) * 100;
            }
            else
            {
                NonTrivialFlowPercentage = 0;
            }

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
            AdHocNetSimulation.SimWindow.WriteOutput("Number of Edges: " + EdgeNums);
            AdHocNetSimulation.SimWindow.WriteOutput("Number of Commodities: " + CommNums);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Number of Round Checks: " + _TotalWakeUpCalls);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Demand Flow: " + MaxFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Single Edge Flow: " + SingleEdgeFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Current Flow: " + TotalCurrFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Final Non-Trivial Flow Percentage: " + NonTrivialFlowPercentage);
            AdHocNetSimulation.SimWindow.WriteOutput("Final Flow Percentage: " + FlowPercentage);
            Cancel = true;
          //  ((SimulationWin)_SimWindow).CancleThreads();
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
        public static int FailNum
        {
            get
            {
                lock (_FailNumLock)
                {
                    return _FailNum;
                }
            }
            set
            {
                lock (_FailNumLock)
                {
                    _FailNum = value;
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
        public static bool Cancel
        {
            get
            {
                lock (_CancelLock)
                {
                    return _Cancel;
                }
            }
            set
            {
                lock (_CancelLock)
                {
                    _Cancel = value;
                }
            }
        }
        #endregion
    }
}
