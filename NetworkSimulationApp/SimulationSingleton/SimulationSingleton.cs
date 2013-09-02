using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkSimulationApp.Simulation;
using NetworkSimulationApp;

namespace NetworkSimulationApp.Singleton
{
    public sealed class SimulationSingleton
    {
        private static readonly AdHocNetSimulation _simulationInstance = new AdHocNetSimulation();
        private static readonly NonThreadSimulation.AdHocNetSimulation _NsimulationInstance = new NonThreadSimulation.AdHocNetSimulation();
        private SimulationSingleton() { }

        internal static AdHocNetSimulation SimulationInstance
        {
            get
            {
                return _simulationInstance;
            }
        }

        internal static NonThreadSimulation.AdHocNetSimulation NonThreadedSimulationInstance
        {
            get
            {
                return _NsimulationInstance;
            }
        }
    }
}
