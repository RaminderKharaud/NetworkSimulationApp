using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.Simulation
{
    public class AdHocFlow 
    {
        public int OriginID;
        public int DestinationID;
        public double OriginalFlow;
        public double CurrFlow;
        public double CommDemand;
        public int FlowCameFrom;
        public double FlowBlockedByPrevNode;

        public AdHocFlow Clone()
        {
            AdHocFlow flow = new AdHocFlow();
            flow.OriginID = this.OriginID;
            flow.DestinationID = this.DestinationID;
            flow.OriginalFlow = this.OriginalFlow;
            flow.CurrFlow = this.CurrFlow;
            flow.CommDemand = this.CommDemand;
            flow.FlowCameFrom = this.FlowCameFrom;
            flow.FlowBlockedByPrevNode = this.FlowBlockedByPrevNode;

            return flow;
        }
    }
}
