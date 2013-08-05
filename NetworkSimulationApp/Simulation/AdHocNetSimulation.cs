using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using NetworkSimulationApp.AdHocMessageBox;
using System.Collections.Concurrent;

namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// This class is called by ModelView when user press the start button to start simulation.
    /// This class set up all the variables needed for the simulation to begin and call the actuall simulation loop
    /// </summary>
    public class AdHocNetSimulation
    {
      //  protected volatile ConcurrentDictionary<int, AdHocNode> Nodes;
        private SimulationWin _SimWindow;
        private int[] _Vertexes;
        private int[,] _Edges;
        private float[,] _Commodities;
      
        public AdHocNetSimulation()
        {
         //   NodeList.Nodes = new ConcurrentDictionary<int, AdHocNode>();
        }
        #region public methods
       
        /// <summary>
        /// Generator method checks whether the simulation is already running or not to make
        /// sure only one simulation can run at one time. It does this by checking all the windows opened
        /// at a given time. If no simulation is running then it will call private _Generate method to
        /// new simulation. If the Simulation is running it will show the message.
        /// </summary>
        /// <param name="Graph"> Graph is a NetGraph Object with nodes and edges passed by refference
        /// from ModelView</param>
        public void Generator(int[] vertexes, int[,] edges, float[,] commodities)
        {
            string WinType = "";
            bool _IsRunning = false;
             foreach (Window window in Application.Current.Windows)
            {
                WinType = window.GetType().ToString();
                if (WinType.Equals("NetworkSimulationApp.SimulationWin")) _IsRunning = true;
             }
            if (!_IsRunning)
            {
                this._Vertexes = vertexes;
                this._Edges = edges;
                this._Commodities = commodities;
                this._generate();
            }
            else
            {
                ExceptionMessage.Show("Simulation already running");
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Graph"></param>
        private void _generate()
        {
            int i = 0,j = 0;
            NodeList.Nodes.Clear();
            Dictionary<int, int> GraphIDtoID = new Dictionary<int, int>();
            NodeActivator.NodeNums = this._Vertexes.Count();

            foreach (int id in _Vertexes)
            {
                GraphIDtoID.Add(id, j);
                NodeList.Nodes.GetOrAdd(j, new AdHocNode(j++, id));
            }
       //     new AdHocFlow(Graph.VertexCount);
            try
            {
                int to, from;
                for (i = 0; i < _Edges.GetLength(0); i++)
                {
                    from = this._Edges[i, 0];
                    to = this._Edges[i,1];
                    from = GraphIDtoID[from];
                    to = GraphIDtoID[to];
                    NodeList.Nodes[from].Targets.GetOrAdd(NodeList.Nodes[to].ID,true);
                    NodeList.Nodes[from].MyTargetThresholds.GetOrAdd(NodeList.Nodes[to].ID, 50);
                    NodeList.Nodes[from].TargetsAndFlowReached.GetOrAdd(NodeList.Nodes[to].ID, 0);
                    NodeList.Nodes[from].TargetsAndMyFlowSent.GetOrAdd(NodeList.Nodes[to].ID, new ConcurrentDictionary<int, float>());
                    NodeList.Nodes[from].TargetsAndFlowForwarded.GetOrAdd(NodeList.Nodes[to].ID, new ConcurrentDictionary<string, float>());
                    NodeList.Nodes[to].Sources.Add(NodeList.Nodes[from].ID);
                    NodeList.Nodes[to].SourcesAndFlowConsumed.GetOrAdd(NodeList.Nodes[from].ID, new ConcurrentDictionary<int, float>());
                    NodeList.Nodes[to].SourcesAndFlowForwarded.GetOrAdd(NodeList.Nodes[from].ID, new ConcurrentDictionary<string, float []>());
                    NodeList.Nodes[to].FLowBlockRateForSources.GetOrAdd(NodeList.Nodes[from].ID, 0);
                    NodeList.Nodes[to].InFlow.GetOrAdd(NodeList.Nodes[from].ID, new ConcurrentDictionary<int, AdHocFlow>());
                }
                for (i = 0; i < this._Commodities.GetLength(0); i++)
                {
                    from = (int) this._Commodities[i, 0];
                    to = (int) this._Commodities[i, 1];
                    this._Commodities[i, 0] = GraphIDtoID[from];
                    this._Commodities[i, 1] = GraphIDtoID[to];
                }
                this._TableFillAlgo();
            }
            catch (Exception e)
            { 
                ExceptionMessage.Show("There was a problem with Data provided to simulation: " + e.ToString());
            }
           
        //    NodeList.Nodes = NodeList.Nodes;
            _SimWindow = new SimulationWin();
            _SimWindow.Show();
            _SimWindow.StartSim();
        }

        private void _TableFillAlgo()
        {
            int origin = 0, dest = 0;
            int[] predecessors;
            int nextPred = 0,j = 0;
            int target = 0;
            for (int i = 0; i < this._Commodities.GetLength(0); i++)
            {
                origin = (int) this._Commodities[i, 0];
                dest = (int) this._Commodities[i, 1];

                NodeList.Nodes[origin].MyDestinationsAndDemands.Add(dest, this._Commodities[i, 2]);
                NodeList.Nodes[origin].MyDestinationsAndCurrentDemands.Add(dest, this._Commodities[i, 2]);
                NodeList.Nodes[origin].FlowReached.GetOrAdd(dest, 0);
                predecessors = this._GraphBFS(origin, dest);
                if (predecessors != null)
                {
                    j = _Vertexes.Length;
                    nextPred = dest;
                    while (j >= 0)
                    {
                        target = nextPred;
                        nextPred = predecessors[nextPred];
                        NodeList.Nodes[nextPred].ForwardingTable.Add(dest, target);
                        if (nextPred == origin) break;
                        j--;
                    }
                }
            }
        }
        private int[] _GraphBFS(int origin, int dest)
        {
            HashSet<int> marked = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            int[] pred = new int[_Vertexes.Length];
            int currID = origin;
            queue.Enqueue(0);
          //  marked.Add(0, 0);
            while (queue.Count != 0)
            {
                currID = queue.Dequeue();
                marked.Add(currID);
                foreach (int id in NodeList.Nodes[currID].Targets.Keys)
                {
                    if (marked.Add(id))
                    {
                        queue.Enqueue(id);
                        pred[id] = currID;
                        if (id == dest) return pred;
                    }
                }
            }
            return null;
        }
        /*
        private void _CreateCommodities(int VertexCount)
        {
            int Targets = 0, Dest = 0;
            int[] TargetList;
            int j = 0;
            
            for (int origin = 0; origin < VertexCount; origin++)
            {
                Targets = NodeList.Nodes[origin].Targets.Count;
                TargetList = new int[Targets + 1]; //origin node also go into check List
                int[][] commodities = new int[Targets][];
                j = 0;
                foreach (KeyValuePair<int, AdHocNode> pair in NodeList.Nodes[origin].Targets)
                {
                    TargetList[j] = ((AdHocNode)pair.Value).ID;
                    ++j;
                }
                TargetList[Targets] = origin; //origin and dest can't be the same
                Dest = this._getRendomDestination(VertexCount, TargetList);
                if (Dest != -1)
                {
                    for (int target = 0; target < TargetList.Length; target++)
                    {
                        int [] path = _sortestPath(TargetList[target], Dest,commodities);
                        commodities[target] = path;
                    }
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

        private int[] _sortestPath(int origin,int dest,int [][] paths){
            int[] PossiblePath = null;

            return PossiblePath;
        }
        */
        #endregion

        #region properties
       
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
