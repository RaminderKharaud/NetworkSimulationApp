﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using NetworkSimulationApp.AdHocMessageBox;

namespace NetworkSimulationApp.NonThreadSimulation
{
    /// <summary>
    /// File:                   AdHocNetSimulation.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       August 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                This class sets up all the data for simulation (nodes, edges etc) for simulation
    ///                         based on the data provided by GUI. It also fill all the data structure, forwarding 
    ///                         talbe for each node according to their edges and split nodes as required. This class
    ///                         holds the Generator method which is called by GUI to start the simulation. This class
    ///                         is same as in the thread version except that it work with regular data structures instead
    ///                         of concurrent data structures
    /// </summary>
    
    internal class AdHocNetSimulation
    {
        private int[] _Vertexes;
        private int[,] _Edges;
        private float[,] _Commodities;
        private float _ProfitVal;
        private float _MaxDemand, _MaxThreshold;
        private float _TotalSingleFlow;
        private float _FaiureRate;
        private int _MaxDegree;
        public static NonThreadedSimulationWin SimWindow;
        public AdHocNetSimulation() { }

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
                if (WinType.Equals("NetworkSimulationApp.NonThreadSimulation.NonThreadedSimulationWin")) _IsRunning = true;
             }
             if (!_IsRunning)
             {
                 this._Vertexes = vertexes;
                 this._Edges = edges;
                 this._Commodities = commodities;
                 this._ProfitVal = ProfitVal;
                 this._MaxDemand = maxDemand;
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

            int i = 0, j = 0;
            Dictionary<int, int> GraphIDtoID = new Dictionary<int, int>();
            SimWindow = new NonThreadedSimulationWin();
            NodeList.Nodes = null;
            NodeActivator.MaxFlow = 0;
            NodeActivator.TotalNumberOfEdges = 0;
            NodeActivator.NodeNums = this._Vertexes.Length;
            NodeActivator.EdgeNums = this._Edges.GetLength(0);
            NodeActivator.CommNums = this._Commodities.GetLength(0);
            NodeActivator.TotalNumberOfEdges = (float)this._Edges.GetLength(0);
            NodeActivator.NodeFailureRate = this._FaiureRate;
            NodeActivator.WakeUpNodeList = new HashSet<int>();
            NodeActivator.NodeSplitBySourceLog = new Dictionary<int, int>();
            NodeActivator.NodeSplitByTargetLog = new Dictionary<int, int>();
            NodeActivator.Cancel = false;
            NodeActivator.FailNum = 0;
            this._MaxThreshold = this._Vertexes.Length * this._MaxDemand;

            //set up poision style probability if failure probability is greater than zero
            if (this._FaiureRate > 0)
            {
                this._setNodeFailureNode();
                this._FaiureRate = this._FaiureRate / 10;
            }
            //add nodes to the static nodelist dictionary
            // The node IDs for simulation may be different than GUI ids
            foreach (int id in _Vertexes)
            {
                GraphIDtoID.Add(id, j);
                NodeList.Nodes.Add(j, new AdHocNode(j++, id, this._ProfitVal, this._FaiureRate));
            }
            try
            { //set up node data structures according to the edges in the network
                int to, from;
                for (i = 0; i < _Edges.GetLength(0); i++)
                {
                    from = this._Edges[i, 0];
                    to = this._Edges[i, 1];
                    from = GraphIDtoID[from];
                    to = GraphIDtoID[to];
                    NodeList.Nodes[from].Targets.Add(NodeList.Nodes[to].ID, true);
                    NodeList.Nodes[from].MyTargetThresholds.Add(NodeList.Nodes[to].ID, this._MaxThreshold);
                    NodeList.Nodes[from].TargetsAndFlowReached.Add(NodeList.Nodes[to].ID, 0);
                    NodeList.Nodes[from].TargetsAndMyFlowSent.Add(NodeList.Nodes[to].ID, new Dictionary<int, float>());
                    NodeList.Nodes[from].TargetsAndFlowForwarded.Add(NodeList.Nodes[to].ID, new Dictionary<string, float>());
                    NodeList.Nodes[to].Sources.Add(NodeList.Nodes[from].ID);
                    NodeList.Nodes[to].SourcesAndFlowConsumed.Add(NodeList.Nodes[from].ID, new Dictionary<int, float>());
                    NodeList.Nodes[to].SourcesAndFlowForwarded.Add(NodeList.Nodes[from].ID, new Dictionary<string, float[]>());
                    NodeList.Nodes[to].FlowBlockValueForSources.Add(NodeList.Nodes[from].ID, 0);
                    NodeList.Nodes[to].InFlow.Add(NodeList.Nodes[from].ID, new Dictionary<string, AdHocFlow>());
                }
                //update commodities according to the simulation ndoe ids
                for (i = 0; i < this._Commodities.GetLength(0); i++)
                {
                    NodeActivator.MaxFlow += this._Commodities[i, 2];
                    from = (int)this._Commodities[i, 0];
                    to = (int)this._Commodities[i, 1];
                    this._Commodities[i, 0] = GraphIDtoID[from];
                    this._Commodities[i, 1] = GraphIDtoID[to];
                }
                SimWindow.Show();
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

            SimWindow.StartSim();
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
                        AdHocNetSimulation.SimWindow.WriteOutput("Node: " + NodeList.Nodes[i].GraphID + " was split by sources");
                        Console.WriteLine("Node: " + NodeList.Nodes[i].GraphID + " was split by sources");
                        this._SplitBySources(i);
                    }
                    else
                    {
                        AdHocNetSimulation.SimWindow.WriteOutput("Node: " + NodeList.Nodes[i].GraphID + " was split by targets");
                        Console.WriteLine("Node: " + NodeList.Nodes[i].GraphID + " was split by targets");
                        this._SplitByTargets(i);
                    }
                }
            }
        }
        /// <summary>
        /// split the given node by source edges and reset data for each node 
        /// and connected nodes
        /// </summary>
        /// <param name="id"></param>
        private void _SplitBySources(int id)
        {
            int NewID = NodeList.Nodes.Count;
            int length = NodeList.Nodes[id].Sources.Count / 2;
            int source = 0;
            NodeList.Nodes.Add(NewID, new AdHocNode(NewID, NewID, this._ProfitVal, this._FaiureRate));
            NodeActivator.NodeSplitBySourceLog.Add(id, NewID);
            List<int> Sources = new List<int>();
            for(int i = 0; i < length; i++)
            {
                Sources.Add(NodeList.Nodes[id].Sources.ElementAt(i));
            }
            for (int i = 0; i < Sources.Count; i++)
            {
                source = Sources.ElementAt(i);

                NodeList.Nodes[id].Sources.Remove(source);
                NodeList.Nodes[id].SourcesAndFlowConsumed.Remove(source);
                NodeList.Nodes[id].SourcesAndFlowForwarded.Remove(source);
                NodeList.Nodes[id].FlowBlockValueForSources.Remove(source);
                NodeList.Nodes[id].InFlow.Remove(source);

                NodeList.Nodes[NewID].Sources.Add(source);
                NodeList.Nodes[NewID].SourcesAndFlowConsumed.Add(source, new Dictionary<int, float>());
                NodeList.Nodes[NewID].SourcesAndFlowForwarded.Add(source, new Dictionary<string, float[]>());
                NodeList.Nodes[NewID].FlowBlockValueForSources.Add(source, 0);
                NodeList.Nodes[NewID].InFlow.Add(source, new Dictionary<string, AdHocFlow>());

                NodeList.Nodes[source].Targets.Remove(id);
                NodeList.Nodes[source].MyTargetThresholds.Remove(id);
                NodeList.Nodes[source].TargetsAndFlowReached.Remove(id);
                NodeList.Nodes[source].TargetsAndMyFlowSent.Remove(id);
                NodeList.Nodes[source].TargetsAndFlowForwarded.Remove(id);

                NodeList.Nodes[source].Targets.Add(NewID, true);
                NodeList.Nodes[source].MyTargetThresholds.Add(NewID, this._MaxThreshold);
                NodeList.Nodes[source].TargetsAndFlowReached.Add(NewID, 0);
                NodeList.Nodes[source].TargetsAndMyFlowSent.Add(NewID, new Dictionary<int, float>());
                NodeList.Nodes[source].TargetsAndFlowForwarded.Add(NewID, new Dictionary<string, float>());
            }

            foreach (int targetID in NodeList.Nodes[id].Targets.Keys)
            {
                NodeList.Nodes[NewID].Targets.Add(targetID, true);
                NodeList.Nodes[NewID].MyTargetThresholds.Add(targetID, this._MaxThreshold);
                NodeList.Nodes[NewID].TargetsAndFlowReached.Add(targetID, 0);
                NodeList.Nodes[NewID].TargetsAndMyFlowSent.Add(targetID, new Dictionary<int, float>());
                NodeList.Nodes[NewID].TargetsAndFlowForwarded.Add(targetID, new Dictionary<string, float>());

                NodeList.Nodes[targetID].Sources.Add(NewID);
                NodeList.Nodes[targetID].SourcesAndFlowConsumed.Add(NewID, new Dictionary<int, float>());
                NodeList.Nodes[targetID].SourcesAndFlowForwarded.Add(NewID, new Dictionary<string, float[]>());
                NodeList.Nodes[targetID].FlowBlockValueForSources.Add(NewID, 0);
                NodeList.Nodes[targetID].InFlow.Add(NewID, new Dictionary<string, AdHocFlow>());
            }
        }
        /// <summary>
        /// split the given node by target edges and reset data for each node 
        /// and connected nodes
        /// </summary>
        /// <param name="id"></param>
        private void _SplitByTargets(int id)
        {
            int NewID = NodeList.Nodes.Count;
            int length = NodeList.Nodes[id].Targets.Count / 2;
            int target = 0;
            NodeList.Nodes.Add(NewID, new AdHocNode(NewID, NewID, this._ProfitVal, this._FaiureRate));
            NodeActivator.NodeSplitByTargetLog.Add(id, NewID);
            List<int> Targets = new List<int>();

            for(int i = 0; i < length; i++)
            {
                Targets.Add(NodeList.Nodes[id].Targets.ElementAt(i).Key);
            }

            for (int i = 0; i < Targets.Count; i++)
            {
                target = Targets.ElementAt(i);

                NodeList.Nodes[id].Targets.Remove(target);
                NodeList.Nodes[id].MyTargetThresholds.Remove(target);
                NodeList.Nodes[id].TargetsAndFlowReached.Remove(target);
                NodeList.Nodes[id].TargetsAndMyFlowSent.Remove(target);
                NodeList.Nodes[id].TargetsAndFlowForwarded.Remove(target);

                NodeList.Nodes[NewID].Targets.Add(target, true);
                NodeList.Nodes[NewID].MyTargetThresholds.Add(target, this._MaxThreshold);
                NodeList.Nodes[NewID].TargetsAndFlowReached.Add(target, 0);
                NodeList.Nodes[NewID].TargetsAndMyFlowSent.Add(target, new Dictionary<int, float>());
                NodeList.Nodes[NewID].TargetsAndFlowForwarded.Add(target, new Dictionary<string, float>());

                NodeList.Nodes[target].Sources.Remove(id);
                NodeList.Nodes[target].SourcesAndFlowConsumed.Remove(id);
                NodeList.Nodes[target].SourcesAndFlowForwarded.Remove(id);
                NodeList.Nodes[target].FlowBlockValueForSources.Remove(id);
                NodeList.Nodes[target].InFlow.Remove(id);

                NodeList.Nodes[target].Sources.Add(NewID);
                NodeList.Nodes[target].SourcesAndFlowConsumed.Add(NewID, new Dictionary<int, float>());
                NodeList.Nodes[target].SourcesAndFlowForwarded.Add(NewID, new Dictionary<string, float[]>());
                NodeList.Nodes[target].FlowBlockValueForSources.Add(NewID, 0);
                NodeList.Nodes[target].InFlow.Add(NewID, new Dictionary<string, AdHocFlow>());
            }

            foreach (int source in NodeList.Nodes[id].Sources)
            {
                NodeList.Nodes[NewID].Sources.Add(source);
                NodeList.Nodes[NewID].SourcesAndFlowConsumed.Add(source, new Dictionary<int, float>());
                NodeList.Nodes[NewID].SourcesAndFlowForwarded.Add(source, new Dictionary<string, float[]>());
                NodeList.Nodes[NewID].FlowBlockValueForSources.Add(source, 0);
                NodeList.Nodes[NewID].InFlow.Add(source, new Dictionary<string, AdHocFlow>());

                NodeList.Nodes[source].Targets.Add(NewID, true);
                NodeList.Nodes[source].MyTargetThresholds.Add(NewID, this._MaxThreshold);
                NodeList.Nodes[source].TargetsAndFlowReached.Add(NewID, 0);
                NodeList.Nodes[source].TargetsAndMyFlowSent.Add(NewID, new Dictionary<int, float>());
                NodeList.Nodes[source].TargetsAndFlowForwarded.Add(NewID, new Dictionary<string, float>());
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
                    if (this._Commodities[i,1] == NodeActivator.NodeSplitBySourceLog.ElementAt(j).Key)
                    {
                        if(NodeList.Nodes[NodeActivator.NodeSplitBySourceLog.ElementAt(j).Value].Sources.Contains((int)this._Commodities[i,0]))
                        {
                            this._Commodities[i,1] = NodeActivator.NodeSplitBySourceLog.ElementAt(j).Value;
                        }
                        else
                        {
                            if (this._GraphBFS((int)this._Commodities[i, 0], (int) this._Commodities[i, 1]) == null)
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
        /// <returns></returns>
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
                if (predecessors != null && !NodeList.Nodes[origin].MyDestinationsAndDemands.ContainsKey(dest))
                {
                    NodeList.Nodes[origin].MyDestinationsAndDemands.Add(dest, this._Commodities[i, 2]);
                    NodeList.Nodes[origin].MyDestinationsAndCurrentDemands.Add(dest, this._Commodities[i, 2]);
                    NodeList.Nodes[origin].FlowReached.Add(dest, 0);
                    NodeList.Nodes[dest].MyOrigins.Add(origin);
                    j = _Vertexes.Length;
                    nextPred = dest;
                    NodeActivator.WakeUpNodeList.Add(nextPred);
                   
                    while (j >= 0)
                    {
                        target = nextPred;
                        nextPred = predecessors[nextPred];
                        if (!NodeList.Nodes[nextPred].ForwardingTable.ContainsKey(dest))
                        {
                            NodeList.Nodes[nextPred].ForwardingTable.Add(dest, target);
                        }
                        NodeActivator.WakeUpNodeList.Add(nextPred);
                        if (nextPred == origin)
                        {
                            break;
                        }
                        else
                        {
                            NodeList.Nodes[nextPred].MyEnvolvedments.Add(origin + ":" + dest);
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
                range = (int) perctage;
                num = random.Next(range - 2, range + 3);
            }
            perctage = (num / 100) * this._Vertexes.Length;
            NodeActivator.FailNum = (int)perctage;
        }
        #endregion

    }
}
