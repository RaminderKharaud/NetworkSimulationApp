using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace NetworkSimulationApp.Simulation
{
    public static class NodeList
    {
        private static ConcurrentDictionary<int, AdHocNode> _nodes;
        public static ConcurrentDictionary<int, AdHocNode> Nodes
        {
            get
            {
                return _nodes;
            }
            set
            {
                _nodes = value;
            }
        }
    }
}
