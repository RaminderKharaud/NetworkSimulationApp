using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NetworkSimulationApp
{
   
    public class AdHocNode
    {
        public Dictionary<int, AdHocNode> Targets;
        public Dictionary<int, AdHocNode> Sources;
        public Dictionary<int, int[][]> Commodities;
        public int GraphID, ID;
        private int FlowOutLimit;
        private bool _flowReady;
        private int _threshold;
        private int MyUtility;
        int i;
        public AdHocNode(int id, int graphId)
        {
            this.ID = id;
            this.GraphID = graphId;
            this._flowReady = false;
            this._threshold = 100;
            this.FlowOutLimit = 100;
            i = 0;
        }

        #region public methods
        public void Start()
        {
            while (i < 20)
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
                if (FlowReady)
                {
                    try
                    {
                       int i =  NodeList.Nodes[1].ID;
                        this._FlowReciever();
                        this._ProcessFlow();
                        this._SendFlow();
                        this.FlowReady = false;
               
                    }
                    catch (SynchronizationLockException e)
                    {
                        Console.WriteLine(e);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }   
        }
    
        private void _FlowReciever()
        {
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
        private void _ProcessFlow()
        {
        }
        private void _SendFlow()
        {
           /* AdHocFlow.signal[ID, this.LinkOut.ID].CurrFlowAmount = 5;
            this.LinkOut.FlowReady = true;
            Console.WriteLine("sent: {0}", GraphID);
            i++; */
        }
        #endregion

        #region properties;
        public bool FlowReady
        {
            get
            {
                return this._flowReady;
            }
            set
            {
                this._flowReady = value;
            }
        }
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
