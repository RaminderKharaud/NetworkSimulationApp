using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// File:                   AdHocFlow.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       May 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Todo:                   Directly connect click event for node to this ViewModel. Right now its connect through 
    ///                         codebehind file through DataContext property.
    ///                         
    /// Purpose:                This class represents the virtual flow that nodes sends to each other.
    internal class AdHocFlow 
    {
        public int OriginID;
        public int DestinationID;
        public float OriginalFlow;
        public float CurrFlow;
        public int FlowCameFrom;
        public float FlowBlockedByPrevNode;

        //clone method creates new flow package with same information
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
