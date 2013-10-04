 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using NetworkSimulationApp.AdHocMessageBox;
using System.Collections.Concurrent;

namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// File:                   AdHocNetSimulation.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       May 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                This class sets up all the data for simulation (nodes, edges etc) for simulation
    ///                         based on the data provided by GUI. It also fill all the data structure, forwarding 
    ///                         talbe for each node according to their edges and split nodes as required. This class
    ///                         holds the Generator method which is called by GUI to start the simulation.
    /// </summary>
    internal class AdHocNetSimulation
    {
        public static SimulationWin SimWindow;
        private int[] _Vertexes;
        private int[,] _Edges;
        private float[,] _Commodities;
        private float _ProfitVal, _TotalSingleFlow;
        private double _FaiureRate;
        private float _MaxDemand, _MaxThreshold;
        private int _MaxDegree;

        public AdHocNetSimulation(){ }

        #region public methods

        /// <summary>
        /// Generator method checks whether the simulation is already running or not to make
        /// sure only one simulation can run at one time. If no simulation is running then it 
        /// will call private _Generate method to start new simulation. If the Simulation is 
        /// running it will show the message.
        /// </summary>
        public void Generator(int[] vertexes, int[,] edges, float[,] commodities, float ProfitVal, float maxDemand, int Nodedegree, float nodeFailureRate)
        {
            string WinType = "";
            bool _IsRunning = false;
             foreach (Window window in Application.Current.Windows)
            {
                WinType = window.GetType().ToString();
                if (WinType.Equals("NetworkSimulationApp.Simulation.SimulationWin")) _IsRunning = true;
             }
            if (!_IsRunning)
            {
                this._Vertexes = vertexes;
                this._Edges = edges;
                this._Commodities = commodities;
                this._ProfitVal = ProfitVal;
                this._MaxDemand = maxDemand;
                this._FaiureRate = nodeFailureRate;
                this._FaiureRate = nodeFailureRate;
                this._MaxDegree = Nodedegree;
                this._generate();
            }
            else
            {
                ExceptionMessage.Show("Simulation already running");
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// This method does all the job before calling the simulation window.
        /// </summary>
        private void _generate()
        {
            //initiate all the data including NodeActivator static variables

            int i = 0,j = 0;
            NodeList.Nodes = null;
            NodeActivator.MaxFlow = 0;
            NodeActivator.TotalNumberOfEdges = 0;
            Dictionary<int, int> GraphIDtoID = new Dictionary<int, int>();
            SimWindow = new SimulationWin();
            NodeActivator.NodeNums = this._Vertexes.Count();
            NodeActivator.EdgeNums = this._Edges.GetLength(0);
            NodeActivator.CommNums = this._Commodities.GetLength(0);
            NodeActivator.SimWindow = SimWindow;
            NodeActivator.TotalNumberOfEdges = (float) this._Edges.GetLength(0);
            NodeActivator.WakeUpNodeList = new HashSet<int>();
            NodeActivator.NodeSplitBySourceLog = new Dictionary<int, int>();
            NodeActivator.NodeSplitByTargetLog = new Dictionary<int, int>();
            NodeActivator.FailNum = 0;
            NodeActivator.Cancel = false;
            this._MaxThreshold = this._Vertexes.Length * this._MaxDemand;

            //set up poision style probability if failure probability is greater than zero
            if (this._FaiureRate > 0)
            {
                this._setNodeFailureNode();
                this._FaiureRate = this._FaiureRate / 100000;
            }

            j = 0;
            //add nodes to the static nodelist dictionary
            // The node IDs for simulation may be different than GUI ids
            foreach (int id in _Vertexes)
            {
                GraphIDtoID.Add(id, j);
                NodeList.Nodes.GetOrAdd(j, new AdHocNode(j++, id, this._ProfitVal, this._FaiureRate));
            }

            try
            { //set up node data structures according to the edges in the network
                int to, from;
                for (i = 0; i < _Edges.GetLength(0); i++)
                {
                    from = this._Edges[i, 0];
                    to = this._Edges[i,1];
                    from = GraphIDtoID[from];
                    to = GraphIDtoID[to];
                    NodeList.Nodes[from].Targets.GetOrAdd(NodeList.Nodes[to].ID,true);
                    NodeList.Nodes[from].MyTargetThresholds.GetOrAdd(NodeList.Nodes[to].ID, this._MaxThreshold);
                    NodeList.Nodes[from].TargetsAndFlowReached.GetOrAdd(NodeList.Nodes[to].ID, 0);
                    NodeList.Nodes[from].TargetsAndMyFlowSent.GetOrAdd(NodeList.Nodes[to].ID, new ConcurrentDictionary<int, float>());
                    NodeList.Nodes[from].TargetsAndFlowForwarded.GetOrAdd(NodeList.Nodes[to].ID, new ConcurrentDictionary<string, float>());
                    NodeList.Nodes[to].Sources.Add(NodeList.Nodes[from].ID);
                    NodeList.Nodes[to].SourcesAndFlowConsumed.GetOrAdd(NodeList.Nodes[from].ID, new ConcurrentDictionary<int, float>());
                    NodeList.Nodes[to].SourcesAndFlowForwarded.GetOrAdd(NodeList.Nodes[from].ID, new ConcurrentDictionary<string, float []>());
                    NodeList.Nodes[to].FlowBlockValueForSources.GetOrAdd(NodeList.Nodes[from].ID, 0);
                    NodeList.Nodes[to].InFlow.GetOrAdd(NodeList.Nodes[from].ID, new ConcurrentDictionary<string, AdHocFlow>());
                }
                //update commodities according to the simulation ndoe ids
                for (i = 0; i < this._Commodities.GetLength(0); i++)
                {
                    NodeActivator.MaxFlow += this._Commodities[i, 2];
                    from = (int) this._Commodities[i, 0];
                    to = (int) this._Commodities[i, 1];
                    this._Commodities[i, 0] = GraphIDtoID[from];
                    this._Commodities[i, 1] = GraphIDtoID[to];
                }

                this._SplitNodes();
                this._SplitCommodities();
                this._TableFillAlgo();
                this._TotalSingleFlow = this._CalculateSingleEdgeFlow();
                NodeActivator.SingleEdgeFlow = this._TotalSingleFlow;
            }
            catch (Exception e)
            { 
                ExceptionMessage.Show("There was a problem with Data provided to simulation: " + e.ToString());
            }
           
            SimWindow.Show();
            SimWindow.StartSim(this._TotalSingleFlow);
        }
        /// <summary>
        /// split nodes according to the max degree provided by user
        /// </summary>
        private void _SplitNodes()
        {
            int length = NodeList.Nodes.Count;
            for (int i = 0; i < length; i++)
            {
                if ((NodeList.Nodes[i].Sources.Count + NodeList.Nodes[i].Targets.Count) > this._MaxDegree)
                {
                    if (NodeList.Nodes[i].Sources.Count > NodeList.Nodes[i].Targets.Count)
                    {
                        this._SplitBySources(i);
                    }
                    else
                    {
                        this._SplitByTargets(i);
                    }
                }
            }
        }
        /// <summary>
        /// split the given node by source edges and reset data for each node 
        /// and connected nodes
        /// </summary>
        private void _SplitBySources(int id)
        {
            int NewID = NodeList.Nodes.Count;
            int length = NodeList.Nodes[id].Sources.Count / 2;
            ConcurrentDictionary<int, float> removedItem1;
            ConcurrentDictionary<string,float[]> removedItem2;
            ConcurrentDictionary<string, AdHocFlow> removedItem3;
            float removedItem4;
            bool removedItem5;
            ConcurrentDictionary<string,float> removedItem6;
            int source = 0;
            NodeList.Nodes.GetOrAdd(NewID, new AdHocNode(NewID, NewID, this._ProfitVal, this._FaiureRate));
            NodeActivator.NodeSplitBySourceLog.Add(id, NewID);
            List<int> Sources = new List<int>();
            for (int i = 0; i < length; i++)
            {
                Sources.Add(NodeList.Nodes[id].Sources.ElementAt(i));
            }
            for (int i = 0; i < Sources.Count; i++)
            {
                source = Sources.ElementAt(i);

                NodeList.Nodes[id].Sources.Remove(source);
                NodeList.Nodes[id].SourcesAndFlowConsumed.TryRemove(source, out removedItem1);
                NodeList.Nodes[id].SourcesAndFlowForwarded.TryRemove(source, out removedItem2);
                NodeList.Nodes[id].FlowBlockValueForSources.TryRemove(source, out removedItem4);
                NodeList.Nodes[id].InFlow.TryRemove(source, out removedItem3);

                NodeList.Nodes[NewID].Sources.Add(source);
                NodeList.Nodes[NewID].SourcesAndFlowConsumed.GetOrAdd(source, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[NewID].SourcesAndFlowForwarded.GetOrAdd(source, new ConcurrentDictionary<string, float[]>());
                NodeList.Nodes[NewID].FlowBlockValueForSources.GetOrAdd(source, 0);
                NodeList.Nodes[NewID].InFlow.GetOrAdd(source, new ConcurrentDictionary<string, AdHocFlow>());

                NodeList.Nodes[source].Targets.TryRemove(id, out removedItem5);
                NodeList.Nodes[source].MyTargetThresholds.TryRemove(id, out removedItem4);
                NodeList.Nodes[source].TargetsAndFlowReached.TryRemove(id, out removedItem4);
                NodeList.Nodes[source].TargetsAndMyFlowSent.TryRemove(id, out removedItem1);
                NodeList.Nodes[source].TargetsAndFlowForwarded.TryRemove(id, out removedItem6);

                NodeList.Nodes[source].Targets.GetOrAdd(NewID, true);
                NodeList.Nodes[source].MyTargetThresholds.GetOrAdd(NewID, this._MaxThreshold);
                NodeList.Nodes[source].TargetsAndFlowReached.GetOrAdd(NewID, 0);
                NodeList.Nodes[source].TargetsAndMyFlowSent.GetOrAdd(NewID, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[source].TargetsAndFlowForwarded.GetOrAdd(NewID, new ConcurrentDictionary<string, float>());
            }

            foreach (int targetID in NodeList.Nodes[id].Targets.Keys)
            {
                NodeList.Nodes[NewID].Targets.GetOrAdd(targetID, true);
                NodeList.Nodes[NewID].MyTargetThresholds.GetOrAdd(targetID, this._MaxThreshold);
                NodeList.Nodes[NewID].TargetsAndFlowReached.GetOrAdd(targetID, 0);
                NodeList.Nodes[NewID].TargetsAndMyFlowSent.GetOrAdd(targetID, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[NewID].TargetsAndFlowForwarded.GetOrAdd(targetID, new ConcurrentDictionary<string, float>());

                NodeList.Nodes[targetID].Sources.Add(NewID);
                NodeList.Nodes[targetID].SourcesAndFlowConsumed.GetOrAdd(NewID, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[targetID].SourcesAndFlowForwarded.GetOrAdd(NewID, new ConcurrentDictionary<string, float[]>());
                NodeList.Nodes[targetID].FlowBlockValueForSources.GetOrAdd(NewID, 0);
                NodeList.Nodes[targetID].InFlow.GetOrAdd(NewID, new ConcurrentDictionary<string, AdHocFlow>());
            }
        }
        /// <summary>
        /// split the given node by target edges and reset data for each node 
        /// and connected nodes
        /// </summary>
        private void _SplitByTargets(int id)
        {
            int NewID = NodeList.Nodes.Count;
            int length = NodeList.Nodes[id].Targets.Count / 2;
            ConcurrentDictionary<int, float> removedItem1;
            ConcurrentDictionary<string, float[]> removedItem2;
            ConcurrentDictionary<string, AdHocFlow> removedItem3;
            float removedItem4;
            bool removedItem5;
            ConcurrentDictionary<string, float> removedItem6;
            int target = 0;
            NodeList.Nodes.GetOrAdd(NewID, new AdHocNode(NewID, NewID, this._ProfitVal, this._FaiureRate));
            NodeActivator.NodeSplitByTargetLog.Add(id, NewID);
            List<int> Targets = new List<int>();

            for (int i = 0; i < length; i++)
            {
                Targets.Add(NodeList.Nodes[id].Targets.ElementAt(i).Key);
            }

            for (int i = 0; i < Targets.Count; i++)
            {
                target = Targets.ElementAt(i);

                NodeList.Nodes[id].Targets.TryRemove(target, out removedItem5);
                NodeList.Nodes[id].MyTargetThresholds.TryRemove(target, out removedItem4);
                NodeList.Nodes[id].TargetsAndFlowReached.TryRemove(target,out removedItem4);
                NodeList.Nodes[id].TargetsAndMyFlowSent.TryRemove(target, out removedItem1);
                NodeList.Nodes[id].TargetsAndFlowForwarded.TryRemove(target, out removedItem6);

                NodeList.Nodes[NewID].Targets.GetOrAdd(target, true);
                NodeList.Nodes[NewID].MyTargetThresholds.GetOrAdd(target, this._MaxThreshold);
                NodeList.Nodes[NewID].TargetsAndFlowReached.GetOrAdd(target, 0);
                NodeList.Nodes[NewID].TargetsAndMyFlowSent.GetOrAdd(target, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[NewID].TargetsAndFlowForwarded.GetOrAdd(target, new ConcurrentDictionary<string, float>());

                NodeList.Nodes[target].Sources.Remove(id);
                NodeList.Nodes[target].SourcesAndFlowConsumed.TryRemove(id, out removedItem1);
                NodeList.Nodes[target].SourcesAndFlowForwarded.TryRemove(id, out removedItem2);
                NodeList.Nodes[target].FlowBlockValueForSources.TryRemove(id, out removedItem4);
                NodeList.Nodes[target].InFlow.TryRemove(id, out removedItem3);

                NodeList.Nodes[target].Sources.Add(NewID);
                NodeList.Nodes[target].SourcesAndFlowConsumed.GetOrAdd(NewID, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[target].SourcesAndFlowForwarded.GetOrAdd(NewID, new ConcurrentDictionary<string, float[]>());
                NodeList.Nodes[target].FlowBlockValueForSources.GetOrAdd(NewID, 0);
                NodeList.Nodes[target].InFlow.GetOrAdd(NewID, new ConcurrentDictionary<string, AdHocFlow>());
            }

            foreach (int source in NodeList.Nodes[id].Sources)
            {
                NodeList.Nodes[NewID].Sources.Add(source);
                NodeList.Nodes[NewID].SourcesAndFlowConsumed.GetOrAdd(source, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[NewID].SourcesAndFlowForwarded.GetOrAdd(source, new ConcurrentDictionary<string, float[]>());
                NodeList.Nodes[NewID].FlowBlockValueForSources.GetOrAdd(source, 0);
                NodeList.Nodes[NewID].InFlow.GetOrAdd(source, new ConcurrentDictionary<string, AdHocFlow>());

                NodeList.Nodes[source].Targets.GetOrAdd(NewID, true);
                NodeList.Nodes[source].MyTargetThresholds.GetOrAdd(NewID, this._MaxThreshold);
                NodeList.Nodes[source].TargetsAndFlowReached.GetOrAdd(NewID, 0);
                NodeList.Nodes[source].TargetsAndMyFlowSent.GetOrAdd(NewID, new ConcurrentDictionary<int, float>());
                NodeList.Nodes[source].TargetsAndFlowForwarded.GetOrAdd(NewID, new ConcurrentDictionary<string, float>());
            }
        }

        /// <summary>
        /// This method reset commodites for splitted nodes if splitted node is origin or destination
        /// for any commodity. f the split node is origin for some commodities and both new nodes have
        /// same outgoing edges (i.e. node has been split by incoming edges) then each commodity will
        /// pick its origin randomly from both nodes. If both new nodes have different outgoing edges 
        /// (node split by outgoing edges) then each commodity will pick its origin according to which 
        /// new node it has valid path. Same technique is used for destination node splitting. This algorithm
        /// also preserves all the single edge commodities when any node involving single edge commodity is split.
        /// </summary>
        private void _SplitCommodities()
        {
            Random random = new Random();
            int rand = 0;

            for (int i = 0; i < this._Commodities.GetLength(0); i++)
            {
                for (int j = 0; j < NodeActivator.NodeSplitBySourceLog.Count; j++)
                {
                    if (this._Commodities[i, 1] == NodeActivator.NodeSplitBySourceLog.ElementAt(j).Key)
                    {
                        if (NodeList.Nodes[NodeActivator.NodeSplitBySourceLog.ElementAt(j).Value].Sources.Contains((int)this._Commodities[i, 0]))
                        {
                            this._Commodities[i, 1] = NodeActivator.NodeSplitBySourceLog.ElementAt(j).Value;
                        }
                        else
                        {
                            if (this._GraphBFS((int)this._Commodities[i, 0], (int)this._Commodities[i, 1]) == null)
                            {
                                this._Commodities[i, 1] = NodeActivator.NodeSplitBySourceLog.ElementAt(j).Value;
                            }
                        }
                    }
                    if (this._Commodities[i, 0] == NodeActivator.NodeSplitBySourceLog.ElementAt(j).Key)
                    {
                        rand = random.Next(0, 2);
                        if (rand == 1) this._Commodities[i, 0] = NodeActivator.NodeSplitBySourceLog.ElementAt(j).Value;
                    }
                }
                for (int j = 0; j < NodeActivator.NodeSplitByTargetLog.Count; j++)
                {
                    if (this._Commodities[i, 0] == NodeActivator.NodeSplitByTargetLog.ElementAt(j).Key)
                    {
                        if (NodeList.Nodes[NodeActivator.NodeSplitByTargetLog.ElementAt(j).Value].Targets.ContainsKey((int)this._Commodities[i, 1]))
                        {
                            this._Commodities[i, 0] = NodeActivator.NodeSplitByTargetLog.ElementAt(j).Value;
                        }
                        else
                        {
                            if (this._GraphBFS((int)this._Commodities[i, 0], (int)this._Commodities[i, 1]) == null)
                            {
                                this._Commodities[i, 0] = NodeActivator.NodeSplitByTargetLog.ElementAt(j).Value;
                            }
                        }
                    }

                    if (this._Commodities[i, 1] == NodeActivator.NodeSplitByTargetLog.ElementAt(j).Key)
                    {
                        rand = random.Next(0, 2);
                        if (rand == 1) this._Commodities[i, 1] = NodeActivator.NodeSplitByTargetLog.ElementAt(j).Value;
                    }
                }
            }
        }
        /// <summary>
        /// This method calcuates the total single edge commodity flow
        /// </summary>
        private float _CalculateSingleEdgeFlow()
        {
            float totalFlow = 0;

            for (int i = 0; i < this._Commodities.GetLength(0); i++)
            {
                if (NodeList.Nodes[(int)this._Commodities[i, 0]].Targets.ContainsKey((int)this._Commodities[i, 1]))
                    totalFlow += this._Commodities[i, 2];
            }
            return totalFlow;
        }
        /// <summary>
        /// This method fill up the forwarding table of each node that is involved in 
        /// commodities by getting a shortest path for each commodity.
        /// </summary>
        private void _TableFillAlgo()
        {
            int origin = 0, dest = 0;
            int[] predecessors;
            int nextPred = 0;
            int j = 0;
            int target = 0;
            for (int i = 0; i < this._Commodities.GetLength(0); i++)
            {
                origin = (int)this._Commodities[i, 0];
                dest = (int)this._Commodities[i, 1];
                predecessors = this._GraphBFS(origin, dest);
                if (predecessors != null)
                {
                    NodeList.Nodes[origin].MyDestinationsAndDemands.GetOrAdd(dest, this._Commodities[i, 2]);
                    NodeList.Nodes[origin].MyDestinationsAndCurrentDemands.GetOrAdd(dest, this._Commodities[i, 2]);
                    NodeList.Nodes[origin].FlowReached.GetOrAdd(dest, 0);
                    NodeList.Nodes[dest].MyOrigins.GetOrAdd(origin,0);
                    j = _Vertexes.Length;
                    nextPred = dest;
                    NodeActivator.WakeUpNodeList.Add(nextPred);
                    while (j >= 0)
                    {
                        target = nextPred;
                        nextPred = predecessors[nextPred];
                        if (!NodeList.Nodes[nextPred].ForwardingTable.ContainsKey(dest))
                        {
                            NodeList.Nodes[nextPred].ForwardingTable.GetOrAdd(dest, target);
                        }
                        NodeActivator.WakeUpNodeList.Add(nextPred);
                        if (nextPred == origin)
                        {
                            break;
                        }
                        else
                        {
                            if (!NodeList.Nodes[nextPred].MyEnvolvedments.ContainsKey(origin + ":" + dest))
                                NodeList.Nodes[nextPred].MyEnvolvedments.GetOrAdd(origin + ":" + dest,0);
                        }
                        j--;
                    }
                }
            }
        }
        /// <summary>
        /// This is GFS algorithm that returns shortest path for a given origin destination pair
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dest"></param>
        /// <returns>array with shortest path</returns>
        private int[] _GraphBFS(int origin, int dest)
        {
            HashSet<int> marked = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            int[] pred = new int[NodeList.Nodes.Count];
            int currID = origin;
            queue.Enqueue(currID);
            marked.Add(currID);
            while (queue.Count != 0)
            {
                currID = queue.Dequeue();
                foreach (int id in NodeList.Nodes[currID].Targets.Keys)
                {
                    if (marked.Add(id))
                    {
                        queue.Enqueue(id);
                        pred[id] = currID;
                        if (id == dest) return pred;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// this method finds the total number of nodes that will fail for simulation
        /// by using a poison style probability distribution
        /// </summary>
        private void _setNodeFailureNode()
        {
            Random random = new Random();
            double num = 0;
            int range = 0;
            double perctage = this._FaiureRate * 100;
            if (perctage <= 2)
            {
                num = random.Next(0, 3);
            }
            else
            {
                range = (int)perctage;
                num = random.Next(range - 2, range + 3);
            }
            perctage = (num / 100) * this._Vertexes.Length;
            NodeActivator.FailNum = (int)perctage;
        }
        #endregion

    }

 }

