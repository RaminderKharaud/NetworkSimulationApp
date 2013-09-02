using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.NonThreadSimulation
{
    internal static class NodeList
    {
        private static volatile Dictionary<int, AdHocNode> _nodes;
        public static Dictionary<int, AdHocNode> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    _nodes = new Dictionary<int, AdHocNode>();
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
