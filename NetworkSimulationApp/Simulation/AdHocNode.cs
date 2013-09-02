﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace NetworkSimulationApp.Simulation
{

    internal partial class AdHocNode
    {
        public HashSet<int> Sources;
        public ConcurrentDictionary<int, byte> MyOrigins;
        public ConcurrentDictionary<string, byte> MyEnvolvedments;
        public ConcurrentDictionary<int, bool> Targets;
        public ConcurrentDictionary<int, float> FlowBlockValueForSources;
        public ConcurrentDictionary<int, ConcurrentDictionary<int, float>> SourcesAndFlowConsumed;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, float[]>> SourcesAndFlowForwarded;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, float>> TargetsAndFlowForwarded;
        public ConcurrentDictionary<int, float> TargetsAndFlowReached;
        public ConcurrentDictionary<int, float> MyTargetThresholds;
        public ConcurrentDictionary<int, ConcurrentDictionary<int, float>> TargetsAndMyFlowSent;
        public ConcurrentDictionary<int, int> ForwardingTable;
        public ConcurrentDictionary<int, float> MyDestinationsAndDemands;
        public ConcurrentDictionary<int, float> MyDestinationsAndCurrentDemands;
        public ConcurrentDictionary<int, float> FlowReached;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, AdHocFlow>> InFlow;
        public int GraphID, ID;
        public bool WakeUpCall;
        private float _MinChange, _CurrUtility, _W;
        private float _TotalFlowSendAndReached, _TotalFlowConsumed, _TotalFlowSent, _TotalFlowForwarded;
        private double _FailureRate;
        private int _SourceNum, _DestNum;
        private byte[,] _Combinations;
        private int[] _CurrCombination;
        private object _Lock;
        Random random;

        public AdHocNode(int id, int graphId, float profit, double failureRate)
        {
            this.ID = id;
            this.GraphID = graphId;
            Targets = new ConcurrentDictionary<int, bool>();
            Sources = new HashSet<int>();
            ForwardingTable = new ConcurrentDictionary<int, int>();
            MyDestinationsAndDemands = new ConcurrentDictionary<int, float>();
            MyDestinationsAndCurrentDemands = new ConcurrentDictionary<int, float>();
            InFlow = new ConcurrentDictionary<int, ConcurrentDictionary<string, AdHocFlow>>();
            FlowBlockValueForSources = new ConcurrentDictionary<int, float>();
            MyTargetThresholds = new ConcurrentDictionary<int, float>();
            FlowReached = new ConcurrentDictionary<int, float>();
            TargetsAndFlowReached = new ConcurrentDictionary<int, float>();
            SourcesAndFlowConsumed = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();
            SourcesAndFlowForwarded = new ConcurrentDictionary<int, ConcurrentDictionary<string, float[]>>();
            TargetsAndMyFlowSent = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();
            TargetsAndFlowForwarded = new ConcurrentDictionary<int, ConcurrentDictionary<string, float>>();
            MyOrigins = new ConcurrentDictionary<int, byte>();
            MyEnvolvedments = new ConcurrentDictionary<string, byte>();
            _Lock = new object();
            this._W = profit;
            this._MinChange = 0.001f;
            this._FailureRate = failureRate;
            random = new Random();
        }

        #region public methods
        public void Start(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            this._intializeCombinations();
            double failure = 0;
            while (true)
            {
                failure = random.NextDouble();
                if (failure < this._FailureRate)
                {
                    this._KillMySelf();
                    NodeActivator.RemoveNode(this.ID);
                    break;
                } 
                this._NodeLoop();

                if (token.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        #endregion

        #region private methods
        private void _NodeLoop()
        {
            this._FlowReciever();
            if (WakeUpCall)
            {
                WakeUpCall = false;
                this._NodeStrategy();
                NodeActivator.NodeDone = true;
            }
        }

        private void _FlowReciever()
        {
            this._SourceNum = this.Sources.Count;
            int i = 0;
            for (int j = 0; j < this._SourceNum; j++)
            {
                i = this.Sources.ElementAt(j);
                foreach (string key in this.InFlow[i].Keys)
                {
                    if (this.InFlow[i][key] != null)
                    {
                        if (this.InFlow[i][key].DestinationID == this.ID)
                        {
                            this._ConsumeFlow(this.InFlow[i][key].OriginID, this.InFlow[i][key].CurrFlow, i);
                        }
                        else
                        {
                            AdHocFlow flow = this.InFlow[i][key].Clone();
                            this._ForwardFlow(flow, i);
                        }
                        this.InFlow[i][key] = null;
                    }
                }
            }
            this._SendMyOwnFlow();
        }
        private void _ConsumeFlow(int OrID, float flowVal, int sourceID)
        {
            try
            {
                if (this.SourcesAndFlowConsumed[sourceID].ContainsKey(OrID))
                {
                    this.SourcesAndFlowConsumed[sourceID][OrID] = flowVal;
                }
                else
                {
                    this.SourcesAndFlowConsumed[sourceID].GetOrAdd(OrID, flowVal);
                }
                NodeList.Nodes[OrID].FlowReached[this.ID] = flowVal;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Node: " + this.ID + "was trying to consume flow from: " + sourceID + "\n" + ex.ToString());
            }
        }
        private void _ForwardFlow(AdHocFlow flow, int sourceID)
        {
            int target = ForwardingTable[flow.DestinationID];
            float blockRate = 0;
            float amount = flow.CurrFlow;
            float flowCame = flow.CurrFlow;
            float amountblocked = 0;
            string key = flow.OriginID + ":" + flow.DestinationID;

            blockRate = this.GetBlockRate(sourceID);
            try
            {
                if (Targets[target])
                {
                    if (flow.CurrFlow != 0)
                    {
                        amountblocked = (amount * blockRate);
                        amount = amount - amountblocked;
                        flow.CurrFlow = amount;
                        flow.FlowBlockedByPrevNode = amountblocked;
                    }
                    flow.FlowCameFrom = this.ID;
                    
                    if (NodeList.Nodes[target].InFlow[this.ID].ContainsKey(key))
                    {
                        NodeList.Nodes[target].InFlow[this.ID][key] = flow;
                    }
                    else
                    {
                        NodeList.Nodes[target].InFlow[this.ID].GetOrAdd(key, flow);
                    }

                }
                else
                {
                    NodeList.Nodes[flow.OriginID].FlowReached[flow.DestinationID] = 0;
                }
               // string key = flow.OriginID + ":" + flow.DestinationID;
                float[] flowVals = new float[2];
                flowVals[0] = flow.CurrFlow;
                flowVals[1] = flowCame;
                if (this.SourcesAndFlowForwarded[sourceID].ContainsKey(key))
                {
                    this.SourcesAndFlowForwarded[sourceID][key] = flowVals;
                }
                else
                {
                    this.SourcesAndFlowForwarded[sourceID].GetOrAdd(key, flowVals);
                }
                if (this.TargetsAndFlowForwarded[target].ContainsKey(key))
                {
                    this.TargetsAndFlowForwarded[target][key] = flowVals[0];
                }
                else
                {
                    this.TargetsAndFlowForwarded[target].GetOrAdd(key, flowVals[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Node: " + this.ID + "was trying to forward  flow to: " + target + "\n" + ex.ToString());
            }
        }
        private void _SendMyOwnFlow()
        {
            int destID = 0;
            float Currdemand;

            this._DestNum = this.MyDestinationsAndDemands.Count;
            try
            {
                for (int j = 0; j < this._DestNum; j++)
                {
                    destID = MyDestinationsAndDemands.ElementAt(j).Key;

                    Currdemand = MyDestinationsAndCurrentDemands.ElementAt(j).Value;
                    AdHocFlow myFlow = new AdHocFlow();
                    myFlow.FlowCameFrom = this.ID;
                    myFlow.OriginID = this.ID;
                    myFlow.DestinationID = destID;
                    myFlow.OriginalFlow = Currdemand;
                    myFlow.CurrFlow = Currdemand;
                    myFlow.FlowCameFrom = this.ID;
                    string key = this.ID + ":" + destID;
                    if (Targets[ForwardingTable[destID]])
                    {
                        if (NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID].ContainsKey(key))
                        {
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID][key] = myFlow;
                        }
                        else
                        {
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID].GetOrAdd(key, myFlow);
                        }
                    }
                    if (TargetsAndMyFlowSent[ForwardingTable[destID]].ContainsKey(destID))
                    {
                        TargetsAndMyFlowSent[ForwardingTable[destID]][destID] = Currdemand;
                    }
                    else
                    {
                        TargetsAndMyFlowSent[ForwardingTable[destID]].GetOrAdd(destID, Currdemand);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Node: " + this.ID + "was trying to send its own  flow to: " + ForwardingTable[destID] + "\n" + ex.ToString());
            }
        }

        public float GetBlockRate(int sourceID)
        {
            lock (_Lock)
            {
                float totalFlow = 0;
                float rate = 0;
                foreach (KeyValuePair<string, float[]> pair in this.SourcesAndFlowForwarded[sourceID])
                {
                    totalFlow += pair.Value[1];
                }

                if (totalFlow < this.FlowBlockValueForSources[sourceID]) return 1;

                if (totalFlow > 0)
                {
                    rate = this.FlowBlockValueForSources[sourceID] / totalFlow;
                }
                else
                {
                    rate = 0;
                }

                return rate;
            }
        }

        private void _UpdateSuccessfulFlow()
        {
            int prevID = this.ID;
            int nextID = 0;
            float currFlow = 0, rate = 0;
            foreach (int dest in this.MyDestinationsAndCurrentDemands.Keys)
            {
                currFlow = this.MyDestinationsAndCurrentDemands[dest];
                prevID = this.ID;
                nextID = this.ForwardingTable[dest];
                if (currFlow > 0)
                {
                    while (nextID != dest)
                    {
                        if (!NodeList.Nodes[prevID].Targets[nextID])
                        {
                            currFlow = 0;
                            break;
                        }
                        else
                        {
                            rate = NodeList.Nodes[nextID].GetBlockRate(prevID);
                            currFlow = currFlow - (currFlow * rate);
                        }

                        if (currFlow <= 0) break;
                        prevID = nextID;
                        nextID = NodeList.Nodes[prevID].ForwardingTable[dest];
                    }
                    this.FlowReached[dest] = currFlow;
                }
            }
        }

        private void _KillMySelf()
        {
            int length = NodeList.Nodes.Count;
            int[] predecessors;
            string[] IDs;
            int origin, dest, nextPred, target;
            foreach (int id in this.MyOrigins.Keys)
            {
                NodeList.Nodes[id].FlowReached[this.ID] = 0;
                NodeList.Nodes[id].MyDestinationsAndCurrentDemands[this.ID] = 0;
                NodeList.Nodes[id].MyDestinationsAndDemands[this.ID] = 0;
            }
            foreach (int key in this.MyDestinationsAndDemands.Keys)
            {
                this.MyDestinationsAndCurrentDemands[key] = 0;
            }
            this._SendMyOwnFlow();

            foreach (string key in this.MyEnvolvedments.Keys)
            {
                IDs = key.Split(':');
                origin = int.Parse(IDs[0]);
                dest = int.Parse(IDs[1]);
                predecessors = this._FindPath(origin, dest, length);
                string commodity = null;
                if (predecessors != null)
                {
                    if (predecessors != null)
                    {
                        int j = length;
                        nextPred = dest;
                        while (j >= 0)
                        {
                            target = nextPred;
                            nextPred = predecessors[nextPred];
                            if (!NodeList.Nodes[nextPred].ForwardingTable.ContainsKey(dest))
                            {
                                NodeList.Nodes[nextPred].ForwardingTable.GetOrAdd(dest, target);
                            }
                            else
                            {
                                NodeList.Nodes[nextPred].ForwardingTable[dest] = target;
                            }
                            if (nextPred == origin)
                            {
                                break;
                            }
                            else
                            {
                                commodity = origin + ":" + dest;
                                if (!NodeList.Nodes[nextPred].MyEnvolvedments.ContainsKey(commodity))
                                {
                                    NodeList.Nodes[nextPred].MyEnvolvedments.GetOrAdd(commodity, 0);
                                }
                            }
                            j--;
                        }
                    }
                }
                else
                {
                    NodeList.Nodes[origin].MyDestinationsAndCurrentDemands[dest] = 0;
                    NodeList.Nodes[origin].MyDestinationsAndDemands[dest] = 0;
                }
            }
        }

        private int[] _FindPath(int origin, int dest, int length)
        {
            HashSet<int> marked = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            int[] pred = new int[length];
            int currID = origin;
            queue.Enqueue(currID);
            marked.Add(currID);
            while (queue.Count != 0)
            {
                currID = queue.Dequeue();

                foreach (int id in NodeList.Nodes[currID].Targets.Keys)
                {
                    if (id != this.ID)
                    {
                        if (marked.Add(id))
                        {
                            queue.Enqueue(id);
                            pred[id] = currID;
                            if (id == dest) return pred;
                        }
                    }
                }
            }
            return null;
        }
        #endregion
    }
}
