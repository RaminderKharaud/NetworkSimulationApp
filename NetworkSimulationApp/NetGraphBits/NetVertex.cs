using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NetworkSimulationApp
{
    /// <summary>
    /// A simple identifiable vertex.
    /// </summary>
    
    class NetVertex
    {
        public int ID { get; private set; }

        public NetVertex(int id)
        {
            ID = id;
        }

    }
}
