using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.NonThreadSimulation
{
    internal class AdHocFlow
    {
        public int OriginID;
        public int DestinationID;
        public float OriginalFlow;
        public float CurrFlow;
        public int FlowCameFrom;
        public float FlowBlockedByPrevNode;
    }
}
