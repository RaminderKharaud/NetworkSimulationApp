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
        public ConcurrentDictionary<int, float> FLowBlockRateForSources;
        public ConcurrentDictionary<int, ConcurrentDictionary<int,float>> SourcesAndFlowConsumed;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, float[]>> SourcesAndFlowForwarded;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, float>> TargetsAndFlowForwarded;
        public ConcurrentDictionary<int, float> TargetsAndFlowReached;
        public ConcurrentDictionary<int, float> MyTargetThresholds;
        public ConcurrentDictionary<int, ConcurrentDictionary<int, float>> TargetsAndMyFlowSent; 
        public Dictionary<int, int> ForwardingTable;
        public Dictionary<int,float> MyDestinationsAndDemands;
        public Dictionary<int, float> MyDestinationsAndCurrentDemands;
      //  public Dictionary<int, int> MyDestinationsAndCurrFlow;
        public ConcurrentDictionary<int, float> FlowReached;
        public ConcurrentDictionary<int, ConcurrentDictionary<int,AdHocFlow>> InFlow;
        public int GraphID, ID;
        private int _MaxCombination;
        private float FlowOutLimit;
        private int _MyTurn;
      //  private float _threshold;
        private float _CurrUtility,_MinChange;
        private float _TotalFlowSendAndReached,_TotalFlowConsumed,_TotalFlowSent,_TotalFlowForwarded;
        private int _SourceCounter,_SourceNum,_DestNum,_DestCounter;
      //  private int _SentCounter, _ForwardCounter, _FlowConsumendCounter, _FlowReachedCounter;
        private bool _FlowReady;
        public bool WakeUpCall;
        public float W;
        private int[,]_Combinations;
        public AdHocNode(int id, int graphId)
        {
            this.ID = id;
            this.GraphID = graphId;
            Targets = new ConcurrentDictionary<int, bool>();
            Sources = new HashSet<int>();
            ForwardingTable = new Dictionary<int, int>();
            MyDestinationsAndDemands = new Dictionary<int, float>();
            MyDestinationsAndCurrentDemands = new Dictionary<int, float>();
            InFlow = new ConcurrentDictionary<int, ConcurrentDictionary<int, AdHocFlow>>();
            FLowBlockRateForSources = new ConcurrentDictionary<int, float>();
            MyTargetThresholds = new ConcurrentDictionary<int, float>();
            FlowReached = new ConcurrentDictionary<int, float>();
            TargetsAndFlowReached = new ConcurrentDictionary<int, float>();
            SourcesAndFlowConsumed = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();
            SourcesAndFlowForwarded = new ConcurrentDictionary<int, ConcurrentDictionary<string, float[]>>();
            TargetsAndMyFlowSent = new ConcurrentDictionary<int, ConcurrentDictionary<int, float>>();
            TargetsAndFlowForwarded = new ConcurrentDictionary<int, ConcurrentDictionary<string, float>>();
            this._FlowReady = false;
            this.WakeUpCall = false;
            this.FlowOutLimit = 100;
            _SourceCounter = 0;
            _SourceNum = 0;
            W = 1;
            this._MinChange = 0.000001f;
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
   /*     public void Start(CancellationToken token)
        { } */
        #endregion

        #region private methods
        private void _NodeLoop()
        {
            this._FlowReciever();
            if (WakeUpCall)
            {
                WakeUpCall = false;
                this._NodeStrategy();
            }
        }
    
        private void _FlowReciever()
        {
            this._SourceNum = this.Sources.Count;
            int i=0;
            this._FlowReady = false;
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
                Console.WriteLine(this.ID + ": have consumed the flow with amount: " + flowVal);
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Node: " + this.ID + "was trying to consume flow from: " + sourceID + "\n" + ex.ToString());
            }
        }
  /*      private void _ProcessFlow(int key)
        {
            
            Console.WriteLine(this.ID + ": recieved Flow with amount: " + this.InFlow[key].OriginalFlow);
            if (this.InFlow[key].DestinationID == this.ID)
            {
                Console.WriteLine(this.ID + ": have consumed the flow with amount:" + InFlow[key].CurrFlow);
                this._UpdateReceivedFlow(this.InFlow[key].CurrFlow);

                int originID = this.InFlow[key].OriginID;

                NodeList.Nodes[originID].FlowReached[this.ID] = this.InFlow[key].CurrFlow;
              
                if (_FlowConsumendCounter < 5)
                {
                    _TotalFlowRecieved += InFlow[key].CurrFlow;
                    _FlowConsumendCounter++;
                }
                this.InFlow[key] = null;
                this._FlowReady = false;
            }
            else
            {
                this._FlowReady = true;
            }
        } */
   /*     private void _SendFlow(int key)
        {
            if (this._FlowReady == true)
            {
                if (this._MyTurn >= this._SourceNum)
                {
                    this._MyTurn = 0;
                    this._ForwardFlow(key);
                    this._SendMyOwnFlow();
                }
                else
                {
                    this._ForwardFlow(key);
                }
                this._FlowReady = false;
            }
            else
            {
                if (this._MyTurn >= this._SourceNum)
                {
                    this._MyTurn = 0;
                    this._SendMyOwnFlow();
                }
            }
            this._MyTurn++;
           /* AdHocFlow.signal[ID, this.LinkOut.ID].CurrFlowAmount = 5;
            this.LinkOut.FlowReady = true;
            Console.WriteLine("sent: {0}", GraphID);
            i++; 
        } */
        private void _ForwardFlow(AdHocFlow flow, int sourceID)
        {
            int target = ForwardingTable[flow.DestinationID];
            float blockRate = FLowBlockRateForSources[sourceID];
            float amount = flow.CurrFlow;
            float flowCame = flow.CurrFlow;
            float amountblocked = 0;
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

                    Console.WriteLine("Node: " + this.ID + " sent flow to: " + target);
                }
                string key = flow.OriginID + ":" + flow.DestinationID;
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
                ExceptionMessage.Show("Node: " + this.ID + "was trying to forward  flow to: " + target + "\n" + ex.ToString());
            }
        }
      /*  private void _ForwardFlow(int key)
        {
            int target = ForwardingTable[this.InFlow[key].DestinationID];
            bool forwarded = false;
            int attempts = 10;
            try
            {
                if (Targets[target])
                {
                    while (attempts >= 0)
                    {
                        if (NodeList.Nodes[target].InFlow[this.ID] == null)
                        {
                            this.InFlow[key].FlowCameFrom = this.ID;
                            NodeList.Nodes[target].InFlow[this.ID] = this.InFlow[key];
                            Console.WriteLine("Node: " + this.ID + " sent flow to: " + target);
                            forwarded = true;
                           
                            break;
                        }
                        Thread.Sleep(5);
                        attempts--;
                    }
                }

                if (!forwarded) this.InFlow[key] = null;
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Node: " + this.ID + "was trying to send flow to: " + target + "\n" + ex.ToString());
            }
        } */
        private void _SendMyOwnFlow()
        {
            int i = 0;
            float Currdemand;
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
                        Console.WriteLine(this.ID + ": sending my own flow");
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

        #endregion

        #region properties;

  
  
        #endregion
    }
}
