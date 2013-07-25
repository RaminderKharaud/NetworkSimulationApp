using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.Simulation
{
    public struct AdHocSignal
    {
        public int OrigID;
        public int DestID;
        public int OrigFlowAmount;
        public int CurrFlowAmount;
        public int BlockedFlowByPrevNode;

        /*   public Signal(int i)
           {
                OrigID = 0;
                DestID = 0;
                OrigFlowAmount = 0;
                CurrFlowAmount = 0;
                BlockedFlowByPrevNode = 0;
           } */
    }
}
