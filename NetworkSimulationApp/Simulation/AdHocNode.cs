using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NetworkSimulationApp.AdHocMessageBox;
using System.Collections.Concurrent;

namespace NetworkSimulationApp.Simulation
{
   
    public partial class AdHocNode
    {
        public HashSet<int> Sources; 
        public ConcurrentDictionary<int, bool> Targets;
        public ConcurrentDictionary<int, double> FlowBlockValueForSources;
        public ConcurrentDictionary<int, ConcurrentDictionary<int,double>> SourcesAndFlowConsumed;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, double[]>> SourcesAndFlowForwarded;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, double>> TargetsAndFlowForwarded;
        public ConcurrentDictionary<int, double> TargetsAndFlowReached;
        public ConcurrentDictionary<int, double> MyTargetThresholds;
        public ConcurrentDictionary<int, ConcurrentDictionary<int, double>> TargetsAndMyFlowSent; 
        public ConcurrentDictionary<int, int> ForwardingTable;
        public ConcurrentDictionary<int,double> MyDestinationsAndDemands;
        public ConcurrentDictionary<int, double> MyDestinationsAndCurrentDemands;
      //  public Dictionary<int, int> MyDestinationsAndCurrFlow;
        public ConcurrentDictionary<int, double> FlowReached;
        public ConcurrentDictionary<int, ConcurrentDictionary<int,AdHocFlow>> InFlow;
        public int GraphID, ID;
      //  private double _threshold;
        private double _MinChange;
        public double CurrUtility;
        private double _TotalFlowSendAndReached,_TotalFlowConsumed,_TotalFlowSent,_TotalFlowForwarded;
        private int _SourceCounter,_SourceNum,_DestNum,_DestCounter;
      //  private int _SentCounter, _ForwardCounter, _FlowConsumendCounter, _FlowReachedCounter;
        public bool WakeUpCall;
        public double W;
        private int[,]_Combinations;
        private int[] _CurrCombination;
        public AdHocNode(int id, int graphId, double profit)
        {
            this.ID = id;
            this.GraphID = graphId;
            Targets = new ConcurrentDictionary<int, bool>();
            Sources = new HashSet<int>();
            ForwardingTable = new ConcurrentDictionary<int, int>();
            MyDestinationsAndDemands = new ConcurrentDictionary<int, double>();
            MyDestinationsAndCurrentDemands = new ConcurrentDictionary<int, double>();
            InFlow = new ConcurrentDictionary<int, ConcurrentDictionary<int, AdHocFlow>>();
            FlowBlockValueForSources = new ConcurrentDictionary<int, double>();
            MyTargetThresholds = new ConcurrentDictionary<int, double>();
            FlowReached = new ConcurrentDictionary<int, double>();
            TargetsAndFlowReached = new ConcurrentDictionary<int, double>();
            SourcesAndFlowConsumed = new ConcurrentDictionary<int, ConcurrentDictionary<int, double>>();
            SourcesAndFlowForwarded = new ConcurrentDictionary<int, ConcurrentDictionary<string, double[]>>();
            TargetsAndMyFlowSent = new ConcurrentDictionary<int, ConcurrentDictionary<int, double>>();
            TargetsAndFlowForwarded = new ConcurrentDictionary<int, ConcurrentDictionary<string, double>>();
            this.WakeUpCall = false;
            _SourceCounter = 0;
            _SourceNum = 0;
            W = profit;
            this._MinChange = 0.000000000000001D;
        }

        #region public methods
        public void Start(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            this._intializeCombinations();
            while (true)
            {
                this._NodeLoop();
               // Thread.Sleep(100);
              //  this.iterations++;
              //  Console.WriteLine(this.ID + "working");
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
            int i=0;
            for(int j = 0; j < this._SourceNum; j++)
            {
                i = this.Sources.ElementAt(j);
                foreach (int key in this.InFlow[i].Keys)
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
                            this._ForwardFlow(flow,i);
                        }
                        this.InFlow[i][key] = null;
                    }
                }
            }
            this._SendMyOwnFlow();
        }
        private void _ConsumeFlow(int OrID, double flowVal, int sourceID)
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
              //  Console.WriteLine(this.ID + ": have consumed the flow with amount: " + flowVal);
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Node: " + this.ID + "was trying to consume flow from: " + sourceID + "\n" + ex.ToString());
            }
        }
        private void _ForwardFlow(AdHocFlow flow, int sourceID)
        {
            int target = ForwardingTable[flow.DestinationID];
            double blockRate = _GetBlockRate(sourceID);
            double amount = flow.CurrFlow;
            double flowCame = flow.CurrFlow;
            double amountblocked = 0;
            try
            {
                if (Targets[target])
                {
                    amountblocked = (amount * blockRate);
                    amount = amount - amountblocked;
                    flow.CurrFlow = amount;
                    flow.FlowBlockedByPrevNode = amountblocked;
                    flow.FlowCameFrom = this.ID;
                    
                    if (NodeList.Nodes[target].InFlow[this.ID].ContainsKey(flow.OriginID + flow.DestinationID))
                    {
                        NodeList.Nodes[target].InFlow[this.ID][flow.OriginID + flow.DestinationID] = flow;
                    }
                    else
                    {
                        NodeList.Nodes[target].InFlow[this.ID].GetOrAdd((flow.OriginID + flow.DestinationID), flow);
                    }

                //    Console.WriteLine("Node: " + this.ID + " sent flow to: " + target);
                }
                string key = flow.OriginID + ":" + flow.DestinationID;
                double[] flowVals = new double[2];
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
                ExceptionMessage.Show("Node: " + this.ID + "was trying to forward  flow to: " + target + "\n" + ex.ToString());
            }
        }
        private void _SendMyOwnFlow()
        {
            int i = 0;
            double Currdemand;
            AdHocFlow myFlow = new AdHocFlow();
            myFlow.FlowCameFrom = this.ID;
            this._DestNum = this.MyDestinationsAndDemands.Count;
            try
            {
                for (int j = 0; j < this._DestNum; j++)
                {
                    if (this._DestCounter >= this._DestNum - 1) this._DestCounter = 0;

                    i = MyDestinationsAndDemands.ElementAt(this._DestCounter).Key;
                    Currdemand = MyDestinationsAndCurrentDemands.ElementAt(this._DestCounter).Value;
                    myFlow.OriginID = this.ID;
                    myFlow.DestinationID = i;
                    myFlow.OriginalFlow = Currdemand;
                    myFlow.CurrFlow = Currdemand;
                    myFlow.FlowCameFrom = this.ID;
                    if (Targets[ForwardingTable[i]])
                    {
                        if (NodeList.Nodes[ForwardingTable[i]].InFlow[this.ID].ContainsKey(this.ID + i))
                        {
                            NodeList.Nodes[ForwardingTable[i]].InFlow[this.ID][this.ID + i] = myFlow;
                        }
                        else
                        {
                            NodeList.Nodes[ForwardingTable[i]].InFlow[this.ID].GetOrAdd((this.ID + i), myFlow);
                        }
                    //    Console.WriteLine(this.ID + ": sending my own flow");
                        if (TargetsAndMyFlowSent[ForwardingTable[i]].ContainsKey(i))
                        {
                            TargetsAndMyFlowSent[ForwardingTable[i]][i] = Currdemand;
                        }
                        else
                        {
                            TargetsAndMyFlowSent[ForwardingTable[i]].GetOrAdd(i, Currdemand);
                        }
                        this._DestCounter++;
                        break;
                    }
                    if (TargetsAndMyFlowSent[ForwardingTable[i]].ContainsKey(i))
                    {
                        TargetsAndMyFlowSent[ForwardingTable[i]][i] = Currdemand;
                    }
                    else
                    {
                        TargetsAndMyFlowSent[ForwardingTable[i]].GetOrAdd(i, Currdemand);
                    }
                    this._DestCounter++;
                }
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Node: " + this.ID + "was trying to send its own  flow to: " + ForwardingTable[i] + "\n" + ex.ToString());
            }
        }

        private double _GetBlockRate(int sourceID)
        {
            double totalFlow = 0;
            double rate = 0;
            foreach (KeyValuePair<string, double[]> pair in this.SourcesAndFlowForwarded[sourceID])
            {
                totalFlow += pair.Value[1];
            }
            rate = this.FlowBlockValueForSources[sourceID] / totalFlow;
            return rate;
        }
        #endregion
    }
}
