using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// File:                   AdHocNode.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       June 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                This is a first part of node class. Due to its size, this class is split
    ///                         into two partial classes. The second class is called AdHocNode.NodeStrategy.
    ///                         This class holds all the logic for a node. Node is a separate thread(like realworld)
    ///                         and everything is done by node logic in the network. This part of the class defines 
    ///                         data structures that nodes need to operate and flow process logic.
    /// </summary>

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
        public bool _WakeUpCall;
        private float _MinChange, _CurrUtility, _W;
        private float _TotalFlowSendAndReached, _TotalFlowConsumed, _TotalFlowSent, _TotalFlowForwarded;
        private double _FailureRate;
        private int _SourceNum, _DestNum;
        private byte[,] _Combinations;
        private int[] _CurrCombination;
        private object _Lock;
        private object _StrategyLock,_ChangeLock;
        Random random;

        public AdHocNode(int id, int graphId, float profit, double failureRate) //constructor
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
            _StrategyLock = new object();
            _ChangeLock = new object();
            this._W = profit;
            this._MinChange = 0.001f;
            this._FailureRate = failureRate;
            random = new Random();
        }

        #region public methods
        /// <summary>
        /// This method runs the thread with infinite loop. It call methods 
        /// to process flow when node is sleeping. if node gets a wake up call
        /// it will call the method to update node strategy.
        /// </summary>
        /// <param name="obj"></param>
        public void Start(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            this._intializeCombinations(); //intialize combinations
            double failure = 0;
            WakeUpCall = true;
            while (true)
            {
                failure = random.NextDouble();
                //if failure is less then given rate and there still more nodes can fail
                if (failure < this._FailureRate && NodeActivator.FailNum > 0)
                {
                    NodeActivator.FailNum = NodeActivator.FailNum - 1;
                    this._KillMySelf();
                    NodeActivator.RemoveNode(this.ID);
                    break;
                } 
                this._NodeLoop();

                if (token.IsCancellationRequested)
                {
                    break;
                }
                if (NodeActivator.Cancel) break;
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// this method is used by start method
        /// to call method for data processing 
        /// </summary>
        private void _NodeLoop()
        {
            this._FlowReciever();
            //if wakeup call than update strategy
            if (WakeUpCall)
            {
                WakeUpCall = false;
                this.NodeStrategy();
              //  NodeActivator.NodeDone = true;
            }
        }

        /// <summary>
        /// This method checks for all the flow comming in and process it
        /// accordingly. If flow is for node itself, it pass it to comsume
        /// method otherwise it is pass to forward method. After checking 
        /// is done, it call the method to sent its own flow.
        /// </summary>
        private void _FlowReciever()
        {
            this._SourceNum = this.Sources.Count;
            int i = 0;
            for (int j = 0; j < this._SourceNum; j++) //check for all flows came in from all sources
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
            this._SendMyOwnFlow(); //send my own flow
        }
        /// <summary>
        /// This method comsumes the flow and send message to 
        /// origin node that this much flow I recieved and keep 
        /// record for source node
        /// </summary>
        /// <param name="OrID">origin node which sent the flow</param>
        /// <param name="flowVal">total flow to consume</param>
        /// <param name="sourceID"> source node from which the flow came in</param>
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
        /// <summary>
        /// This method block the flow according to the source it came 
        /// from and forward rest of the flow and update its data accordingly
        /// </summary>
        /// <param name="flow">flow to send</param>
        /// <param name="sourceID">source node it came from</param>
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
        /// <summary>
        /// this method sends its own flow to all of its destinations 
        /// according to the current flow that is reaching to destinations
        /// </summary>
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
        /// <summary>
        /// this method calculates the block value for each 
        /// flow according to the source node it came from. 
        /// The block value is calculated for each flow according to
        /// the total flow comming from that source node.
        /// </summary>
        /// <param name="sourceID"></param>
        /// <returns></returns>
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
        /// <summary>
        /// This method finds out how much flow is reaching 
        /// to its destinations according to the current state of network
        /// and update its values accordingly.
        /// </summary>
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
        /// <summary>
        /// This method is called by the node when it is going to fail.
        /// It updates data structures in the connected node, update values
        /// in its commodities and check if the commodities that go through
        /// this node can find a new path. If there is a new path, it will update
        /// the nodes forwarding talbe accordingly otherwise make the flow for that
        /// commodity to zero.
        /// </summary>
        private void _KillMySelf()
        {
            int length = NodeList.Nodes.Count;
            int[] predecessors;
            string[] IDs;
            int origin, dest, nextPred, target;

            foreach (int id in this.MyOrigins.Keys) //tell my origins that I wont be recieving anything
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
            //update commodities that go through me
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
        /// <summary>
        /// This method checkes if the commodities that this node 
        /// is involved have another path or not
        /// </summary>
        /// <param name="origin">original node id</param>
        /// <param name="dest">destination node id</param>
        /// <param name="length">total number of nodes in the network</param>
        /// <returns></returns>
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
        public bool WakeUpCall
        {
            get
            {
                lock (_ChangeLock)
                {
                    return _WakeUpCall;
                }
            }
            set
            {
                lock (_ChangeLock)
                {
                    _WakeUpCall = value;
                }
            }
        }
    }

}
