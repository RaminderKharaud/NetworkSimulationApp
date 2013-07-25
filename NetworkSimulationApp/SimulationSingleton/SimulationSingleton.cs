using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkSimulationApp.Simulation;

namespace NetworkSimulationApp.Singleton
{
    public sealed class SimulationSingleton
    {
        private static readonly AdHocNetSimulation _simulationInstance = new AdHocNetSimulation();

        private SimulationSingleton() { }

        public static AdHocNetSimulation SimulationInstance
        {
            get
            {
                return _simulationInstance;
            }
        }

    }
}
