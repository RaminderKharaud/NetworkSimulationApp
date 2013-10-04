using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// File:                   NodeList.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       May 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                This is a static class that holds a list of nodes for simulation. The node ids for
    ///                         simulation are matched with the index of node in this list. Node are accessed during 
    ///                         the simulation through this List. The list is concurrent to handle the multiple threads 
    ///                         access
    /// </summary>
    internal static class NodeList
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
