using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp.NonThreadSimulation
{
    internal class LoopSimulation
    {
        public void StartSimulation()
        {
            int length = NodeList.Nodes.Count;
            int WakeUpRange = NodeActivator.WakeUpNodeList.Count();
            bool equilibrium = false;
            NodeActivator.TotalWakeUpCalls = 0;
            Random random = new Random();
            NodeActivator.StartTime = DateTime.Now;
            int rand = 0, i = 0;

            for (i = 0; i < length; i++)
            {
                NodeList.Nodes[i].FlowReciever();
            }

            while (true)
            {
                WakeUpRange = NodeActivator.WakeUpNodeList.Count();
                rand = random.Next(0, WakeUpRange);
                equilibrium = NodeActivator.loopWakeUpCall(rand);
                if (equilibrium) break;
                if (WakeUpRange <= 1)
                {
                    Console.WriteLine("All nodes have been failed");
                    break;
                }
            }
        }
    }
}
