using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using QuickGraph;
namespace NetworkSimulationApp
{
    /// <summary>
    /// this code is similar to Graphsharp tutorial example at http://sachabarbs.wordpress.com/2010/08/31/pretty-cool-graphs-in-wpf/
    /// </summary>
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
