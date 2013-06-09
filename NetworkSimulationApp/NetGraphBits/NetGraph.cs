using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using QuickGraph;
namespace NetworkSimulationApp
{
     [Serializable()] 
    class NetGraph : BidirectionalGraph<NetVertex, NetEdge>
    {
         public NetGraph() { }
    
        public NetGraph(bool allowParallelEdges)
            : base(allowParallelEdges) { }

        public NetGraph(bool allowParallelEdges, int vertexCapacity)
            : base(allowParallelEdges, vertexCapacity) { }
    }
}
