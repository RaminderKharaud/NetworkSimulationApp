using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.Simulation
{
    internal class AdHocFlow 
    {
        public int OriginID;
        public int DestinationID;
        public float OriginalFlow;
        public float CurrFlow;
        public int FlowCameFrom;
        public float FlowBlockedByPrevNode;

        public AdHocFlow Clone()
        {
            AdHocFlow flow = new AdHocFlow();
            flow.OriginID = this.OriginID;
            flow.DestinationID = this.DestinationID;
            flow.OriginalFlow = this.OriginalFlow;
            flow.CurrFlow = this.CurrFlow;
            flow.FlowCameFrom = this.FlowCameFrom;
            flow.FlowBlockedByPrevNode = this.FlowBlockedByPrevNode;

            return flow;
        }

    }
}
