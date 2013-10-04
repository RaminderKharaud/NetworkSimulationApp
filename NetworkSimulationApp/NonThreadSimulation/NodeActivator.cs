using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.NonThreadSimulation
{
    /// <summary>
    /// File:                   NodeActivator.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       August 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                NodeActivator is given a random node by simulation loop to update strategy and process nodes
    ///                         accordingly. It also has finalize method that gets the result of simulation
    /// </summary>
    internal static class NodeActivator
    {
        private static volatile int _NodeNums; //total number of nodes in the network
        public static int EdgeNums;
        public static int CommNums;
        private static volatile bool _NodeDone;
        private static volatile int _NoChangeCounter;
        public static decimal TotalWakeUpCalls;
        public static DateTime StartTime, EndTime;
        public static float MaxFlow;
        public static float SingleEdgeFlow;
        public static int FailNum;
        public static float TotalNumberOfEdges;
        public static Dictionary<int, int> NodeSplitBySourceLog; //list of nodes splitted by source edges
        public static Dictionary<int, int> NodeSplitByTargetLog; //list of nodes splitted by target edges
        public static NonThreadedSimulationWin SimWin;
        private static object _CancelLock = new object();
        private static bool _Cancel = false;
        /// <summary>
        /// wake up node list contains all the nodes that are involved in commodities
        /// this list gets updeted when a node fails
        /// </summary>
        public static HashSet<int> WakeUpNodeList;
        public static double NodeFailureRate;

        /// <summary>
        /// This method updates the node strategy for given node and if 
        /// no changer counter is equal to the total number of nodes, it runs the 
        /// round check and if round check returns true, it calls the finalize method
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static bool loopWakeUpCall(int i)
        {
            bool NashEquilibrium = false;
            int NodeId = WakeUpNodeList.ElementAt(i);
            NodeActivator.TotalWakeUpCalls++;

            NodeList.Nodes[NodeId].NodeStrategy();
            FlowProcessRoutine(NodeId);

            if (NoChangeCounter > WakeUpNodeList.Count)
            {
                NashEquilibrium = LoopRoundCheck();
                if (NashEquilibrium)
                {
                    FinalizeSimulation();
                    _Cancel = true;
                    return true;
                }
            }
            if (_Cancel) return true;
            
            return false;
        }
        /// <summary>
        /// Check all nodes and if no node changes its strategy
        /// returns true
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// This method process data for all the nodes in the commodities
        /// which the given node is involved in.
        /// </summary>
        /// <param name="nodeID"></param>
        private static void FlowProcessRoutine(int nodeID)
        {
            int nextID = 0, origin = 0, destination = 0;
            string[] IDs = null;
            try
            {
                //process data for all commodities for which this node is destination
                for (int i = 0; i < NodeList.Nodes[nodeID].MyOrigins.Count; i++)
                {
                    nextID = NodeList.Nodes[nodeID].MyOrigins.ElementAt(i);
                    while (nextID != nodeID)
                    {
                        NodeList.Nodes[nextID].FlowReciever();
                        nextID = NodeList.Nodes[nextID].ForwardingTable[nodeID];
                    }
                }
            
                NodeList.Nodes[nodeID].FlowReciever();

                //process data for all commodities for which this node is origin
                for (int i = 0; i < NodeList.Nodes[nodeID].MyDestinationsAndDemands.Count; i++)
                {
                    nextID = nodeID;
                    destination = NodeList.Nodes[nodeID].MyDestinationsAndDemands.ElementAt(i).Key;
                    while (nextID != destination)
                    {
                        nextID = NodeList.Nodes[nextID].ForwardingTable[destination];
                        NodeList.Nodes[nextID].FlowReciever();
                    }
                }
             
                //process data for all commodities that goes through this node
                for (int i = 0; i < NodeList.Nodes[nodeID].MyEnvolvedments.Count; i++)
                {
                    IDs = NodeList.Nodes[nodeID].MyEnvolvedments.ElementAt(i).Split(':');
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
            catch (Exception ex)
            {
                AdHocNetSimulation.SimWindow.WriteOutput("\nProblem in Process routine\n" + ex.ToString());
            }
        }

        /// <summary>
        /// remove the node from list of wake up nodes. 
        /// this method is called by the node which is going to fail
        /// </summary>
        /// <param name="nodeID"></param>
        public static void RemoveNode(int nodeID)
        {
            AdHocNetSimulation.SimWindow.WriteOutput("Node: " + nodeID + " has failed");
            WakeUpNodeList.Remove(nodeID);
        }
        /// <summary>
        /// This method calculates all the final values of simulation and send information
        /// to simulation window before it stop all the threads
        /// </summary>
        private static void FinalizeSimulation()
        {
            AdHocNetSimulation.SimWindow.WriteOutput("\nEquilibrium Reached\n");
            float TotalCurrFlow = 0;
            float TotalEdgesAlive = 0;
            float EdgePercentage = 0;
            float FlowPercentage = 0;
            float NonTrivialFlowPercentage = 0;
            int id = 0;
            EndTime = DateTime.Now;

            for (int i = 0; i < WakeUpNodeList.Count; i++)
            {
                float flow = NodeList.Nodes[WakeUpNodeList.ElementAt(i)].getTotalCurrentFlow();
                if (!float.IsNaN(flow)) TotalCurrFlow += flow;
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
            EdgePercentage = (TotalEdgesAlive / TotalNumberOfEdges) * 100;
            TimeSpan time = EndTime.Subtract(StartTime);
            int totalTime = time.Milliseconds;

            foreach(KeyValuePair<int,int> pair in NodeSplitBySourceLog)
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
            AdHocNetSimulation.SimWindow.WriteOutput("Total Number of Node WakeUp Calls: " + NodeActivator.TotalWakeUpCalls);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Demand Flow: " + MaxFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Single Edge Flow: " + SingleEdgeFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Total Current Flow: " + TotalCurrFlow);
            AdHocNetSimulation.SimWindow.WriteOutput("Final Non-Trivial Flow Percentage: " + NonTrivialFlowPercentage);
            AdHocNetSimulation.SimWindow.WriteOutput("Final Flow Percentage: " + FlowPercentage);
        }

        #region Properties
        public static int NodeNums
        {
            get
            {
                return _NodeNums;
            }
            set
            {
                _NodeNums = value;
            }
        }
        public static int NoChangeCounter
        {
            get
            {
                return _NoChangeCounter;
            }
            set
            {
                _NoChangeCounter = value;
            }
        }
        public static bool NodeDone
        {
            get
            {
                return _NodeDone;
                
            }
            set
            {
              
                _NodeDone = value;
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
