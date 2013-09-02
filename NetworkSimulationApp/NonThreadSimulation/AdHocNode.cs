using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.NonThreadSimulation
{
    internal partial class AdHocNode
    {
        public HashSet<int> Sources;
        public HashSet<int> MyOrigins;
        public HashSet<string> MyEnvolvedments;
        public Dictionary<int, bool> Targets;
        public Dictionary<int, float> FlowBlockValueForSources;
        public Dictionary<int, Dictionary<int, float>> SourcesAndFlowConsumed;
        public Dictionary<int, Dictionary<string, float[]>> SourcesAndFlowForwarded;
        public Dictionary<int, Dictionary<string, float>> TargetsAndFlowForwarded;
        public Dictionary<int, float> TargetsAndFlowReached;
        public Dictionary<int, float> MyTargetThresholds;
        public Dictionary<int, Dictionary<int, float>> TargetsAndMyFlowSent;
        public Dictionary<int, int> ForwardingTable;
        public Dictionary<int, float> MyDestinationsAndDemands;
        public Dictionary<int, float> MyDestinationsAndCurrentDemands;
        public Dictionary<int, float> FlowReached;
        public Dictionary<int, Dictionary<string, AdHocFlow>> InFlow;
        public int GraphID, ID;
        public bool WakeUpCall;
        private float _MinChange, _CurrUtility, _W;
        private float _TotalFlowSendAndReached, _TotalFlowConsumed, _TotalFlowSent, _TotalFlowForwarded;
        private double _FailureRate;
        private int _SourceNum, _DestNum;
        private byte[,] _Combinations;
        private int[] _CurrCombination;
        private bool _initialized,_failed;
        private Random _Random;

        public AdHocNode(int id, int graphId, float profit, double failureRate)
        {
            this.ID = id;
            this.GraphID = graphId;
            Targets = new Dictionary<int, bool>();
            Sources = new HashSet<int>();
            MyOrigins = new HashSet<int>();
            MyEnvolvedments = new HashSet<string>();
            ForwardingTable = new Dictionary<int, int>();
            MyDestinationsAndDemands = new Dictionary<int, float>();
            MyDestinationsAndCurrentDemands = new Dictionary<int, float>();
            InFlow = new Dictionary<int, Dictionary<string, AdHocFlow>>();
            FlowBlockValueForSources = new Dictionary<int, float>();
            MyTargetThresholds = new Dictionary<int, float>();
            FlowReached = new Dictionary<int, float>();
            TargetsAndFlowReached = new Dictionary<int, float>();
            SourcesAndFlowConsumed = new Dictionary<int, Dictionary<int, float>>();
            SourcesAndFlowForwarded = new Dictionary<int, Dictionary<string, float[]>>();
            TargetsAndMyFlowSent = new Dictionary<int, Dictionary<int, float>>();
            TargetsAndFlowForwarded = new Dictionary<int, Dictionary<string, float>>();
            _Random = new Random();
            this._W = profit;
            this._MinChange = 0.001f;
            this._FailureRate = failureRate;
        }

        #region private methods

        public void FlowReciever()
        {
            double failure = _Random.NextDouble();

            if (!this._initialized)
            {
                this._initialized = true;
                this._intializeCombinations();
            }
            
            if (failure < this._FailureRate)
            {
                this._KillMySelf();
                NodeActivator.RemoveNode(this.ID);
                _failed = true;
            }
            if (!_failed)
            {
                this._SourceNum = this.Sources.Count;
                int i = 0;
                string key = null;

                for (int j = 0; j < this._SourceNum; j++)
                {
                    i = this.Sources.ElementAt(j);
                    for (int x = 0; x < this.InFlow[i].Count; x++)
                    {
                        if (this.InFlow[i].ElementAt(x).Value.OriginID != -1)
                        {
                            if (this.InFlow[i].ElementAt(x).Value.DestinationID == this.ID)
                            {
                                this._ConsumeFlow(this.InFlow[i].ElementAt(x).Value.OriginID, this.InFlow[i].ElementAt(x).Value.CurrFlow, i);
                            }
                            else
                            {
                                key = this.InFlow[i].ElementAt(x).Key;
                                this._ForwardFlow(key, i);
                            }
                            key = this.InFlow[i].ElementAt(x).Key;
                            this.InFlow[i][key].OriginID = -1;
                        }
                    }
                }
                this._SendMyOwnFlow();
            }
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
                    this.SourcesAndFlowConsumed[sourceID].Add(OrID, flowVal);
                }
                NodeList.Nodes[OrID].FlowReached[this.ID] = flowVal;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Node: " + this.ID + "was trying to consume flow from: " + sourceID + "\n" + ex.ToString());
            }
        }
        private void _ForwardFlow(string flowID, int sourceID)
        {
            int target = ForwardingTable[this.InFlow[sourceID][flowID].DestinationID];
            int OriginID = this.InFlow[sourceID][flowID].OriginID;
            int DestinationID = this.InFlow[sourceID][flowID].DestinationID;
            float blockRate = 0;
            float amount = this.InFlow[sourceID][flowID].CurrFlow;
            float flowCame = this.InFlow[sourceID][flowID].CurrFlow;
            float amountblocked = 0;
            string key = OriginID + ":" + DestinationID;
            blockRate = this.GetBlockRate(sourceID);
            try
            {
                if (Targets[target])
                {
                    if (amount != 0)
                    {
                        amountblocked = (amount * blockRate);
                        amount = amount - amountblocked;

                    }
                    if (NodeList.Nodes[target].InFlow[this.ID].ContainsKey(key))
                    {
                        NodeList.Nodes[target].InFlow[this.ID][key].OriginID = OriginID;
                        NodeList.Nodes[target].InFlow[this.ID][key].DestinationID = DestinationID;
                        NodeList.Nodes[target].InFlow[this.ID][key].FlowCameFrom = this.ID;
                        NodeList.Nodes[target].InFlow[this.ID][key].CurrFlow = amount;
                        NodeList.Nodes[target].InFlow[this.ID][key].FlowBlockedByPrevNode = amountblocked;
                        NodeList.Nodes[target].InFlow[this.ID][key].OriginalFlow = this.InFlow[sourceID][flowID].OriginalFlow;
                    }
                    else
                    {
                        AdHocFlow flow = new AdHocFlow();

                        flow.OriginID = OriginID;
                        flow.DestinationID = DestinationID;
                        flow.FlowCameFrom = this.ID;
                        flow.CurrFlow = amount;
                        flow.FlowBlockedByPrevNode = amountblocked;
                        flow.OriginalFlow = this.InFlow[sourceID][flowID].OriginalFlow;

                        NodeList.Nodes[target].InFlow[this.ID].Add(key, flow);
                    }
                }
                else
                {
                    NodeList.Nodes[OriginID].FlowReached[DestinationID] = 0;
                }

                float[] flowVals = new float[2];
                flowVals[0] = amount;
                flowVals[1] = flowCame;
                if (this.SourcesAndFlowForwarded[sourceID].ContainsKey(key))
                {
                    this.SourcesAndFlowForwarded[sourceID][key] = flowVals;
                }
                else
                {
                    this.SourcesAndFlowForwarded[sourceID].Add(key, flowVals);
                }
                if (this.TargetsAndFlowForwarded[target].ContainsKey(key))
                {
                    this.TargetsAndFlowForwarded[target][key] = flowVals[0];
                }
                else
                {
                    this.TargetsAndFlowForwarded[target].Add(key, flowVals[0]);
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
            string key = null;
            this._DestNum = this.MyDestinationsAndDemands.Count;
            try
            {
                for (int j = 0; j < this._DestNum; j++)
                {
                    destID = MyDestinationsAndDemands.ElementAt(j).Key;

                    Currdemand = MyDestinationsAndCurrentDemands.ElementAt(j).Value;

                    if (Targets[ForwardingTable[destID]])
                    {
                        key = this.ID + ":" + destID;
                        if (NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID].ContainsKey(key))
                        {
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID][key].FlowCameFrom = this.ID;
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID][key].OriginID = this.ID;
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID][key].DestinationID = destID;
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID][key].OriginalFlow = Currdemand;
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID][key].CurrFlow = Currdemand;
                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID][key].FlowCameFrom = this.ID;
                        }
                        else
                        {
                            AdHocFlow myFlow = new AdHocFlow();
                            myFlow.FlowCameFrom = this.ID;
                            myFlow.OriginID = this.ID;
                            myFlow.DestinationID = destID;
                            myFlow.OriginalFlow = Currdemand;
                            myFlow.CurrFlow = Currdemand;
                            myFlow.FlowCameFrom = this.ID;

                            NodeList.Nodes[ForwardingTable[destID]].InFlow[this.ID].Add(key, myFlow);
                        }
                    }
                    if (TargetsAndMyFlowSent[ForwardingTable[destID]].ContainsKey(destID))
                    {
                        TargetsAndMyFlowSent[ForwardingTable[destID]][destID] = Currdemand;
                    }
                    else
                    {
                        TargetsAndMyFlowSent[ForwardingTable[destID]].Add(destID, Currdemand);
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
            string [] IDs;
            int origin, dest,nextPred,target;
            foreach (int id in this.MyOrigins)
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

            foreach (string key in this.MyEnvolvedments)
            {
                IDs = key.Split(':');
                origin = int.Parse(IDs[0]);
                dest = int.Parse(IDs[1]);
                predecessors = this._FindPath(origin, dest, length);
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
                                NodeList.Nodes[nextPred].ForwardingTable.Add(dest, target);
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
                                NodeList.Nodes[nextPred].MyEnvolvedments.Add(origin + ":" + dest);
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
