using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkSimulationApp.NonThreadSimulation
{
    /// <summary>
    /// File:                   LoopSimulation.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       August 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                This class defines the main loop for non-thread Simulation
    /// </summary>
    class LoopSimulation
    {
        public void start(object obj)
        {
            int length = NodeList.Nodes.Count;
            int WakeUpRange = NodeActivator.WakeUpNodeList.Count();
            bool equilibrium = false;
            NodeActivator.TotalWakeUpCalls = 0;
            Random random = new Random();
            NodeActivator.StartTime = DateTime.Now;
            int rand = 0, i = 0;
            CancellationToken token = (CancellationToken)obj;

            //initialize data for each node before simulation loop starts
            for (i = 0; i < length; i++)
            {
                NodeList.Nodes[i].FlowReciever();
                if (NodeActivator.Cancel) break;
            }

            //this loop only breaks if equilibium reached or all nodes get failed
            while (true)
            {
                if (NodeActivator.Cancel) break;
                WakeUpRange = NodeActivator.WakeUpNodeList.Count();
                if (WakeUpRange <= 1)
                {
                    AdHocNetSimulation.SimWindow.WriteOutput("\nAll nodes have been failed\n");
                    break;
                }
                rand = random.Next(0, WakeUpRange);
                equilibrium = NodeActivator.loopWakeUpCall(rand);

                if (equilibrium) break;
                if (token.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
