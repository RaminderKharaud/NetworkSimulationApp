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
        private static volatile ConcurrentDictionary<int, AdHocNode> _nodes;
        public static ConcurrentDictionary<int, AdHocNode> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    _nodes = new ConcurrentDictionary<int, AdHocNode>();
                    return _nodes;
                }
                else
                {
                    return _nodes;
                }
            }
            set
            {
                _nodes = value;
            }
        }
    }
}
