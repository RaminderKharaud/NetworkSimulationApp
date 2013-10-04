using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkSimulationApp.Simulation;
using NetworkSimulationApp;

namespace NetworkSimulationApp.Singleton
{
    /// <summary>
    /// File:                   SimulationSingleton.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       July 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                This is a singlton class that make sure only one instance of thread
    ///                         simulation or non threaded simulation is created during the life of application process
    /// </summary>
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
