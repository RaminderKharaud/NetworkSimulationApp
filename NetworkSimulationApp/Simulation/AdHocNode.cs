using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NetworkSimulationApp.AdHocMessageBox;

namespace NetworkSimulationApp.Simulation
{
   
    public class AdHocNode
    {
        public HashSet<int> Targets;
        public HashSet<int> Sources;
        public Dictionary<int, int> ForwardingTable;
        public Dictionary<int,int> MyDestinationsAndDemands;
        public Dictionary<int, AdHocFlow> InFlow;
        public int GraphID, ID;
        private int FlowOutLimit;
        private bool _FlowReady;
        private int _MyTurn;
        private int _threshold;
        private int MyUtility;
        private int _SourceCounter,_SourceNum,_DestNum,_DestCounter;
        int iterations;
        public AdHocNode(int id, int graphId)
        {
            this.ID = id;
            this.GraphID = graphId;
            Targets = new HashSet<int>();
            Sources = new HashSet<int>();
            ForwardingTable = new Dictionary<int, int>();
            MyDestinationsAndDemands = new Dictionary<int, int>();
            InFlow = new Dictionary<int, AdHocFlow>();
            this._FlowReady = false;
            this._threshold = 100;
            this.FlowOutLimit = 100;
            _SourceCounter = 0;
            _SourceNum = 0;
            iterations = 0;
        }

        #region public methods
        public void Start()
        {
            while (this.iterations < 20)
            {
                this._NodeLoop();
                
            }
           
        }
   /*     public void Start(CancellationToken token)
        { } */
        #endregion

        #region private methods
        private void _NodeLoop()
        {
            lock(this)   // Enter synchronization block
            {
             //   if (FlowReady)
             //   {
                    try
                    {
                    //   int i =  NodeList.Nodes[1].ID;
                        this._FlowReciever();
                      //  this._ProcessFlow();
                     //   this._SendFlow();
                     //   this.FlowReady = false;
               
                    }
                    catch (SynchronizationLockException e)
                    {
                        Console.WriteLine(e);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Console.WriteLine(e);
                    }
               // }
            }   
        }
    
        private void _FlowReciever()
        {
            this._SourceNum = this.Sources.Count;
            int i=0;
            this._FlowReady = false;
            for(int j = 0; j < this._SourceNum; j++)
            {
                i = this.Sources.ElementAt(this._SourceCounter);
                if (this._SourceCounter == this._SourceNum - 1) this._SourceCounter = 0;
                if (this.InFlow[i] != null) 
                {
                    this._SourceCounter++;
                    this._ProcessFlow(i);
                    break;
                }
                this._SourceCounter++;
                j++;
            }
            this._SendFlow(i);
         /*   _FlowIn = LinkIn.FlowOut;
            foreach (int DestId in _FlowIn.DestinationID)
            {
                if (DestId == this.ID)
                {
                    this.MyUtility += _FlowIn.FlowAmount[DestId];
                }
            } */ 
           /* if (AdHocFlow.signal[this.LinkIn.ID, ID].CurrFlowAmount == 5)
            {
                Console.WriteLine("got: {0}", GraphID);
            }
            else
            {
                Console.WriteLine("empty flow");
            } */
        }
        private void _ProcessFlow(int key)
        {
            Console.WriteLine("Flow amount is: " + this.InFlow[key].OriginalFlow);
            if (this.InFlow[key].DestinationID == this.ID)
            {
                this.InFlow[key] = null;
                Console.WriteLine("I have consumed the flow");
                this._FlowReady = false;
            }
            else
            {
                this._FlowReady = true;
            }
        }
        private void _SendFlow(int key)
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
            i++; */
        }
        private void _ForwardFlow(int key)
        {
            int target = ForwardingTable[this.InFlow[key].DestinationID];
            int attempts = 10;
            try
            {
                while (attempts >= 0)
                {
                    if (NodeList.Nodes[target].InFlow[this.ID] == null)
                    {
                        NodeList.Nodes[target].InFlow[this.ID] = this.InFlow[key];
                        Console.WriteLine("Node: " + this.ID + " sent flow to: " + target);
                        break;
                    }
                    Thread.Sleep(5);
                    attempts--;
                }
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Node: " + this.ID + "was trying to send flow to: " + target + "\n" + ex.ToString());
            }
        }
        private void _SendMyOwnFlow()
        {
            int i = 0,demand =0;
            AdHocFlow myFlow = new AdHocFlow();
            

            this._DestNum = this.MyDestinationsAndDemands.Count;
            for (int j = 0; j < this._DestNum; j++)
            {
                if (this._DestCounter >= this._DestNum - 1) this._DestCounter = 0;

                i = MyDestinationsAndDemands.ElementAt(this._DestCounter).Key;
                demand = MyDestinationsAndDemands.ElementAt(this._DestCounter).Value;
                myFlow.DestinationID = i;
                myFlow.OriginalFlow = demand;
                if (NodeList.Nodes[ForwardingTable[i]].InFlow[this.ID] == null)
                {
                    this._DestCounter++;
                    NodeList.Nodes[ForwardingTable[i]].InFlow[this.ID] = myFlow;
                    Console.WriteLine(this.ID +": sending my own flow");
                    break;
                }
                this._DestCounter++;
            }

        }
        #endregion

        #region properties;

        public int Threshold
        {
            get
            {
                return this._threshold;
            }
            set
            {
                this._threshold = value;
            }
        }
  
        #endregion
    }
}
