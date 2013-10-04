using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using GraphSharp.Controls;
using System.Windows.Input;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Windows;
using NetworkSimulationApp.Singleton;
using NetworkSimulationApp.AdHocMessageBox;

namespace NetworkSimulationApp
{
    /// <summary>
    /// File:                   MainWindowViewModel.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       Feb 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Todo:                   Directly connect click event for node to this ViewModel. Right now its connect through 
    ///                         codebehind file through DataContext property.
    ///                         
    /// Purpose:                The gui of this project has been implemented using WPF technology and for best practice,
    ///                         its implemented using MVVM model. This class is the main ViewModel for the Application window.
    ///                         It handles all the event and implements Graph logic for creating, modifying, saving, and
    ///                         opening existing Graph. For graph creation, third party library: "GraphSharp" is used. This 
    ///                         class saves the Graph information and send it simulation when simulation is started by user. 
    ///                         Rest is done by simulation.
    ///Knowldge Required        **In order to fully understand the implementation of this class, reader should have basic knowldge
    ///                         of MVVM pattern**
    ///                         **Reader dont need to learn GraphSharp library, reading this class carefull will teach all the basic
    ///                         operation of Graphsharp that programmer need to know in order to make anychanges or to reuse this Gui
    ///                         implementation for different application**
    /// </summary>
    partial class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Class Data

        public static string OutputFilePath;
        public static bool CanDraw;
        /*NetGraph is a GraphSharp object which will create graph
         * it is binded with the graph of GUI and vertexes and edges will be 
         * added to this graph. It is static because there is only one graph 
         * loaded when application runs*/

        private static NetGraph _graph;

        private ObservableCollection<CommoditiesEntryViewModel> _CommodityList;
        private ObservableCollection<int> _VList;  //list of current vertex in Graph by thier unique ID
        private HashSet<string> _CommEdges;
        private int _IDCount;  //ID for vertexes 
        private int _Counter;
        private int _FistNodeID;
        private string _layoutAlgorithmType;
        private string _openedFileName, _NodeFailureRate, _MaxDegree;
        private string _ProfitFactor, _NumberOfCommodities, _MinimumDemand, _MaximumDemand;
        private float _ProfitFactorVal, _NodeFailureRateVal;
        int _NumberOfCommoditiesVal,_MinimumDemandVal, _MaximumDemandVal, _MaxDegreeVal;
        //Following are the ICommand binded with GUI through properties
        private ICommand _createGraph;
        private ICommand _createVertex; 
        private ICommand _createEdge;
        private ICommand _removeVertex;
        private ICommand _removeEdge;
        private ICommand _drawGraph;
        private ICommand _openGraph;
        private ICommand _saveGraph;
        private ICommand _AddCommodity;
        private ICommand _start;
        private ICommand _Refresh;
        private ICommand _simulationMode;
        private int _SimMode;
        private int _Mode;
        private int[] _path;
        private int[,] _SimEdges;
     //   private float _TotalSingleFlow;
       // private List<NetVertex> _existingVertices;
        
        
        #endregion

        #region Constructors
        public MainWindowViewModel()
        {
            this._Intialization();
        }
        #endregion

        #region private methods
        /// <summary>
        /// This method intializes all the data for a brand new graph 
        /// Constructor also called this method when application starts
        /// </summary>
        private void _Intialization()
        {
            _IDCount = 0;
            _Counter = 0;
            _FistNodeID = 0;
            //  _existingVertices = new List<NetVertex>();
            _openedFileName = "";
            _Mode = 0;
            _ProfitFactor = "1";
            _ProfitFactorVal = 1;
            _NumberOfCommoditiesVal = 100;
            _NumberOfCommodities = "100";
            _MinimumDemand = "5";
            _MaximumDemand = "15";
            _NodeFailureRate = "0";
            _MaxDegree = "10";
            _MaxDegreeVal = 10;
            _NodeFailureRateVal = 0;
            _MinimumDemandVal = 5;
            _MaximumDemandVal = 15;
            _CommodityList = new ObservableCollection<CommoditiesEntryViewModel>();
            _VList = new ObservableCollection<int>();
            _CommEdges = new HashSet<string>();
            _AddNewCommodity(null);
            MainWindowViewModel.CanDraw = false;
        }
        /// <summary>
        /// This method adds new edge to the graph
        /// </summary>
        /// <param name="from">from is the origin of new edge</param>
        /// <param name="to">to is the destination of new edge</param>
        /// <returns>it returns the newly create edge, in case its needed</returns>
        private NetEdge _AddNewGraphEdge(NetVertex from, NetVertex to)
        {
            string edgeString = string.Format("{0}-{1} Connected", from.ID.ToString(), to.ID.ToString());
            NetEdge newEdge = new NetEdge(edgeString, from, to);
            Graph.AddEdge(newEdge);
            return newEdge;
        }
        /// <summary>
        /// This method is called by NodeClickLogic if the mode is set to 1
        /// the method is called two times: when user clicks first node and when user clicks second node
        /// to create an edge between them.
        /// first time it only store the node ID, second time it check if there is already an edge between those 
        /// node. If there is no edge then it will add new edge to the graph.
        /// </summary>
        /// <param name="NodeID"></param>
        private void _CreateEdge(int NodeID)
        {
            if (_Counter == 0)
            {
                _FistNodeID = NodeID;
                _Counter++;
            }
            else if (_Counter == 1)
            {
                int from = 0, to = 0;
                int count = Graph.VertexCount;
                _Mode = 0;
                _Counter = 0;
                for (int i = 0; i < count; i++)
                {
                    if (Graph.Vertices.ElementAt(i).ID == _FistNodeID) from = i;
                    if (Graph.Vertices.ElementAt(i).ID == NodeID) to = i;
                }
                if (from != to)
                {
                    try
                    {
                        if (!Graph.ContainsEdge(Graph.Vertices.ElementAt(from), Graph.Vertices.ElementAt(to)))
                            _AddNewGraphEdge(Graph.Vertices.ElementAt(from), Graph.Vertices.ElementAt(to));
                    }
                    catch (Exception ex)
                    {
                        ExceptionMessage.Show("Exception when creating edge: " + ex.ToString());
                    }
                }
            }
        }
        /// <summary>
        /// this method works the same way as CreateEdge
        /// this method is called by NodeClickLogic when mode is set to 2
        /// </summary>
        /// <param name="NodeID">ID of vertex</param>
        private void _RemoveEdge(int NodeID)
        {
            if (_Counter == 0)
            {
                _FistNodeID = NodeID;
                _Counter++;
            }
            else if (_Counter == 1)
            {
                _Mode = 0;
                _Counter = 0;
                
                if (NodeID != _FistNodeID)
                {
                    int from = 0, to = 0;
                    int count = Graph.VertexCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (Graph.Vertices.ElementAt(i).ID == _FistNodeID) from = i;
                        if (Graph.Vertices.ElementAt(i).ID == NodeID) to = i;
                    }
                    if (Graph.ContainsEdge(Graph.Vertices.ElementAt(from), Graph.Vertices.ElementAt(to)))
                    {
                        try
                        {
                            NetEdge TempEdge;
                            Graph.TryGetEdge(Graph.Vertices.ElementAt(from), Graph.Vertices.ElementAt(to), out TempEdge);
                            Graph.RemoveEdge(TempEdge);
                        }
                        catch(Exception ex)
                        {
                            ExceptionMessage.Show("Exception when removing edge: " + ex.ToString());
                        }
                    }
                    _Mode = 0;
                    _Counter = 0;
                }
            }
        }
        /// <summary>
        /// this method is called by NodeClickLogic when mode is set to 3
        /// it removes the given vertex from graph and _VList if vertex is not involved 
        /// in any of the commodity
        /// </summary>
        /// <param name="NodeID"></param>
        private void _RemoveVertex(int NodeID)
        {
            _Mode = 0;
            int index = 0;
            int count = Graph.VertexCount;
            for (int i = 0; i < count; i++)
            {
                if (Graph.Vertices.ElementAt(i).ID == NodeID) index = i;
            }
            try
            {
                bool CanRemove = true;
                foreach (CommoditiesEntryViewModel cv in CommodityList)
                {
                    if (cv.OriginID == NodeID || cv.DestinationID == NodeID) CanRemove = false;
                }
                if (CanRemove)
                {
                    Graph.RemoveVertex(Graph.Vertices.ElementAt(index));
                    _VList.Remove(NodeID);
                }
                else
                {
                    ExceptionMessage.Show("Can not remove this node, it is origin or destination in one or more Commodities");
                }
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Exception removing vertex: " + ex.ToString());
            }
        }
        /// <summary>
        /// this method is called when user click on the create Networkx
        /// graph option under the File menu.
        /// </summary>
        private void _CreateNetworkXGraph()
        {
            GraphWin graphWin = new GraphWin();
            graphWin.Show();
        }
        /// <summary>
        /// This method takes graph file data for networkx graph and 
        /// creates vertexes, edges. It also creates random commodities with
        /// valid paths. 
        /// </summary>
        /// <param name="lines">graph file text</param>
        private void _DrawGraphWithCommodities(string [] lines)
        {
            int CommodityNum = this._NumberOfCommoditiesVal;
            int MinComm = this._MinimumDemandVal;
            int MaxComm = this._MaximumDemandVal + 1;
            int nodeID = 0;
            bool CommoditySet = false;
            List<int> Origins = new List<int>();
            List<int> Destinations = new List<int>();
            List<float> Demands = new List<float>();
            int OriginID = 0, DestID = 0;
            float demand = 0;
            SortedSet<int> nodeList = new SortedSet<int>();
         //   HashSet<int> nodeList = new HashSet<int>();
            HashSet<int> edgeOutNodes = new HashSet<int>();
            Random random = new Random();
            Dictionary<int, LinkedList<int>> EdgeMap = new Dictionary<int, LinkedList<int>>();

            Graph = new NetGraph(true);
            try
            {
                foreach (string line in lines) //create and add vertex to the graph
                {
                    string[] IDs = line.Split(' ');

                    if (int.TryParse(IDs[0], out nodeID))
                    {
                        foreach (string id in IDs)
                        {
                            int currID = int.Parse(id);
                            if (nodeList.Add(currID))
                            {
                               // _VList.Add(currID);
                                Graph.AddVertex(new NetVertex(currID));
                            }
                        }
                    }
                }
                for (int i = 0; i < nodeList.Count; i++)
                {
                    _VList.Add(nodeList.ElementAt(i));
                }
                this._IDCount = this._VList.Count();
                int from = 0, to = 0, curID = 0;
                int count = Graph.VertexCount;
                bool valid = true;
                //create edges, and keep record of vertex with edges going out for each valid vertex
                foreach (string line in lines) 
                {
                    string[] IDs = line.Split(' ');

                    if (int.TryParse(IDs[0], out nodeID))
                    {
                        EdgeMap.Add(nodeID, new LinkedList<int>());
                        for (int j = 1; j < IDs.Length; j++)
                        {
                            curID = int.Parse(IDs[j]);
                            valid = true;
                            if(EdgeMap.ContainsKey(curID))
                            {
                                foreach (int id in EdgeMap[curID])
                                {
                                    if (id == nodeID) valid = false;
                                }
                            }
                            if (valid)
                            {
                                edgeOutNodes.Add(nodeID);
                                EdgeMap[nodeID].AddLast(curID);
                                for (int i = 0; i < count; i++)
                                {
                                    if (Graph.Vertices.ElementAt(i).ID == nodeID) from = i;
                                    if (Graph.Vertices.ElementAt(i).ID == curID) to = i;
                                }
                                this._AddNewGraphEdge(Graph.Vertices.ElementAt(from), Graph.Vertices.ElementAt(to));
                            }
                        }
                    }
                }

                //commodity is choosed at random, if that commodity does not already exist
                //check if it has valid path. if it exists then just add demand to it.

                for (int i = 0; i < _NumberOfCommoditiesVal; i++)
                {
                    CommoditySet = false;
                    while (!CommoditySet)
                    {
                        OriginID = random.Next(0, edgeOutNodes.Count);
                        OriginID = edgeOutNodes.ElementAt(OriginID);
                        DestID = random.Next(0, nodeList.Count);
                        DestID = nodeList.ElementAt(DestID);
                        demand = random.Next(MinComm, MaxComm);

                        for (int j = 0; j < Origins.Count; j++)
                        {
                            if (Origins[j] == OriginID && Destinations[j] == DestID)
                            {
                                CommoditySet = true;
                                Demands[j] += demand;
                                break;
                            }
                        }

                        if (CommoditySet) break;
                        this._path = null;
                        if (this._HasPath(OriginID, DestID, nodeList.Count, EdgeMap))
                        {
                           // this._GetSimulationEdges(OriginID, DestID);
                            Origins.Add(OriginID);
                            Destinations.Add(DestID);
                            Demands.Add(demand);
                            CommoditySet = true;
                        }
                    }
                }

                if (Origins.Count > 0) CommodityList.Remove(CommodityList.ElementAt(0));
                for (int i = 0; i < Origins.Count; i++)
                {
                    CommoditiesEntryViewModel cvm = new CommoditiesEntryViewModel();
                    cvm.OriginID = Origins[i];
                    cvm.DestinationID = Destinations[i];
                    cvm.DemandVal = Demands[i];
                    cvm.CombList = _VList;
                    cvm.ParentList = CommodityList;
                    CommodityList.Add(cvm);
                
                }

                NotifyPropertyChanged("Graph");
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Something wrong with the Data in the File\n" + ex.ToString());
                Graph = null;
            }
        }
        /// <summary>
        /// This methods check whether the given commodity has valid path or not
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dest"></param>
        /// <param name="length"></param>
        /// <param name="EdgeMap">dictionary that has outgoing edges for each valid node</param>
        /// <returns></returns>
        private bool _HasPath(int origin, int dest, int length, Dictionary<int, LinkedList<int>> EdgeMap)
        {
            HashSet<int> marked = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            int[] pred = new int[length];
            int currID = origin;
            queue.Enqueue(currID);
            marked.Add(currID);
            while (queue.Count != 0)
            {
                currID = queue.Dequeue();

                foreach (int id in EdgeMap[currID])
                {
                    if (marked.Add(id))
                    {
                        queue.Enqueue(id);
                        pred[id] = currID;
                        if (id == dest)
                        {
                            this._path = pred;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// this method add edges if they are not already added to the valid edges
        /// edges which are not part of any existing commodity will not be sent to 
        /// the simulation
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dest"></param>
        private void _GetSimulationEdges(int origin, int dest)
        {
            int source = dest;
            int target = 0;
            while (source != origin)
            {
                target = source;
                source = this._path[source];
                this._CommEdges.Add(source + ":" + target);
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// This method handles the click event of vertex and calls method according to 
        /// the mode value. If mode is zero, it does nothing.
        /// if user click the create Edge button (which will set mode to 1) and click 
        /// on any vertex afterwards, this method will call createEdge method 
        /// </summary>
        /// <param name="NodeID">NodeID is the ID of clicked vertex</param>
        public void NodeClickLogic(int NodeID)
        {
            if (NodeID >= 0)
            {
                if (_Mode == 1)  //AddEdgeMode
                {
                    _CreateEdge(NodeID);
                }
                else if (_Mode == 2)  //RemoveEdgeMode
                {
                    _RemoveEdge(NodeID);
                }
                else if (_Mode == 3) //RemoveVertexMode
                {
                    _RemoveVertex(NodeID);
                }
            }

        }
        public void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Graph != null)
            {
                int code = ExceptionMessage.ShowConfirmation("Have you saved the graph?");
                if (code == 0 || code == 2) e.Cancel = true;
            }
        }
        #endregion
    }
}
