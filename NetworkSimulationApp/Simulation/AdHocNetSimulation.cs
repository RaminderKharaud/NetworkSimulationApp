using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

namespace NetworkSimulationApp
{
    /// <summary>
    /// This class is called by ModelView when user press the start button to start simulation.
    /// This class set up all the variables needed for the simulation to begin and call the actuall simulation loop
    /// </summary>
    class AdHocNetSimulation
    {
        ConcurrentDictionary<int, AdHocNode> Nodes;
        private bool _isRunning;
        SimulationWin SimWindow;
      
        public AdHocNetSimulation()
        {
            this._isRunning = false;
            this.Nodes = new ConcurrentDictionary<int, AdHocNode>();
        }
        #region public methods
       
        /// <summary>
        /// Generator method checks whether the simulation is already running or not to make
        /// sure only one simulation can run at one time. It does this by checking all the windows opened
        /// at a given time. If no simulation is running then it will call private _Generate method to
        /// new simulation. If the Simulation is running it will show the message.
        /// </summary>
        /// <param name="Graph"> Graph is a NetGraph Object with nodes and edges passed by refference from
        /// from ModelView</param>
        public void Generator(NetGraph Graph)
        {
            string WinType = "";
            this.IsRunning = false;
             foreach (Window window in Application.Current.Windows)
            {
                WinType = window.GetType().ToString();
                if (WinType.Equals("NetworkSimulationApp.SimulationWin")) this.IsRunning = true;
             }
            if (!this.IsRunning)
            {
                if (Graph.VertexCount > 2)
                {
                    this._Generate(Graph);
                }
                else
                {
                    MessageBoxResult simRunning = MessageBox.Show("Graph must have more than 2 nodes");
                }
            }
            else
            {
                MessageBoxResult simRunning = MessageBox.Show("Simulation already running");
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Graph"></param>
        private void _Generate(NetGraph Graph)
        {
            int Ecount = Graph.EdgeCount;
            int i = 0,j = 0;
            this.Nodes.Clear();
            Dictionary<int, int> GraphIDtoID = new Dictionary<int, int>();

            foreach(NetVertex vertex in Graph.Vertices)
            {
                i = vertex.ID;
                GraphIDtoID.Add(i, j);
                this.Nodes.GetOrAdd(j, new AdHocNode(j++,i));
            }
         
       //     new AdHocFlow(Graph.VertexCount);
            try
            {
                int to, from;
                for (i = 0; i < Ecount; i++)
                {
                    to = Graph.Edges.ElementAt(i).Target.ID;
                    from = Graph.Edges.ElementAt(i).Source.ID;
                    to = GraphIDtoID[to];
                    from = GraphIDtoID[from];
                    this.Nodes[from].Targets.Add(this.Nodes[to].ID, this.Nodes[to]);
                    this.Nodes[to].Sources.Add(this.Nodes[from].ID, this.Nodes[from]);
                }
            }
            catch (Exception e)
            { 
                ExceptionMessage.Show("There was a problem with simulation: " + e.ToString());
            }
           
            NodeList.Nodes = this.Nodes;
            SimWindow = new SimulationWin();
            SimWindow.Show();
            SimWindow.StartSim();
        }

        private void _CreateCommodities(int VertexCount)
        {
            int Targets = 0, Dest = 0;
            int[] TargetList;
            int j = 0;
            for (int i = 0; i < VertexCount; i++)
            {
                Targets = this.Nodes[i].Targets.Count;
                TargetList = new int[Targets + 1];
                j = 0;
                foreach (KeyValuePair<int, AdHocNode> pair in this.Nodes[i].Targets)
                {
                    TargetList[j] = ((AdHocNode)pair.Value).ID;
                    ++j;
                }
                TargetList[Targets] = i;
                Dest = this._getRendomDestination(VertexCount, TargetList);
                if (Dest != -1)
                {

                }
            }
        }

        private int _getRendomDestination(int vertexCount, int[] TargetList)
        {
            Random random = new Random();
            int dest = 0;
            int trails = vertexCount * 50;
            bool found = false,HasMatch = false;
            int len = TargetList.Length;
            int i = 0, j = 0;
            for (i = 0; i < trails; i++)
            {
                HasMatch = false;
                dest = random.Next(0, vertexCount);
                for (j = 0; j < len; j++)
                {
                    if (dest == TargetList[j]) HasMatch = true;
                }
                if (!HasMatch)
                {
                    found = true;
                    break;
                }
            }
            if(found) return dest;
            return -1;
        }
        
        #endregion

        #region properties
        public bool IsRunning
            {
                get
                {
                    return _isRunning;
                }
                set
                {
                    _isRunning = value;
                }

            }
        #endregion
    }

 }
/*
               ((AdHocNode)Nodes[1]).FlowReady = true;
               Thread node1 = new Thread(new ThreadStart(((AdHocNode)Nodes[1]).Start));
               Thread node2 = new Thread(new ThreadStart(((AdHocNode)Nodes[2]).Start));
         //      Thread node3 = new Thread(new ThreadStart(((AdHocNode)Nodes[3]).Start));
               int result = 0;
                
                   try
                   {
                   //    Task.Factory.StartNew(((AdHocNode)Nodes[1]).Start);
                    //   Task.Factory.StartNew(((AdHocNode)Nodes[2]).Start);

                       node1.Start();
                       node2.Start();
                  //     node1.Join();
                 //      node2.Join(); 
                   //    Monitor.Pulse(this);
                       //       node3.Join();
                       Console.WriteLine("done");
                       // threads producer and consumer have finished at this point.
                   }
                   catch (ThreadStateException e)
                   {
                       Console.WriteLine(e);  // Display text of exception
                       result = 1;            // Result says there was an error
                   }
                   catch (ThreadInterruptedException e)
                   {
                       Console.WriteLine(e);  // This exception means that the thread
                       // was interrupted during a Wait
                       result = 1;            // Result says there was an error
                   }
                   
                   
                   // Even though Main returns void, this provides a return code to 
                   // the parent process.
                   Environment.ExitCode = result; */
