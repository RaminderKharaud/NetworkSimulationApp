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
    class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Class Data

        /*NetGraph is a GraphSharp object which will create graph
         * it is binded with the graph of GUI and vertexes and edges will be 
         * added to this graph. It is static because there is only one graph 
         * loaded when application runs*/
        private static NetGraph _graph;

        private ObservableCollection<CommoditiesEntryViewModel> _CommodityList;
        private ObservableCollection<int> _VList;  //list of current vertex in Graph by thier unique ID

        private int _IDCount;  //ID for vertexes 
        private int _Counter;
        private int _FistNodeID;
        private string _layoutAlgorithmType;
        private string _openedFileName;
        private string _ProfitFactor, _NumberOfCommodities, _MinimumDemand, _MaximumDemand;
        private double _ProfitFactorVal;
        int _NumberOfCommoditiesVal,_MinimumDemandVal, _MaximumDemandVal;
        //Following are the ICommand binded with GUI through properties
        private ICommand _createGraph;
        private ICommand _createVertex; 
        private ICommand _createEdge;
        private ICommand _removeVertex;
        private ICommand _removeEdge;
        private ICommand _openGraph;
        private ICommand _saveGraph;
        private ICommand _AddCommodity;
        private ICommand _start;
        private ICommand _Refresh;
        private int _Mode;
       // private List<NetVertex> _existingVertices;
        
        
        #endregion

        #region Constructors
        public MainWindowViewModel()
        {
            this._Intialization();
        }
        #endregion

        #region Commands 
        /*Most of these commands are binded with gui button in WPF therefore they
         * are required to pass object parameter whether its required or not. For more information
         * please study MVVM pattern for WPF */
        #region Graph Commands
        /// <summary>
        /// user can only create graph if the graph is null
        /// if the graph has been created or loaded from existing file then user can not 
        /// create a new graph. 
        /// </summary>
        /// <param name="parameter">parameter has to be passed but does not require in the implementation</param>
        /// <returns></returns>
        private bool _CanCreateNewGraph(object parameter)
        {
            if(Graph == null) return true;
            return false;
        }
        /// <summary>
        /// creates graph by creating only one node
        /// </summary>
        /// <param name="parameter">not required for implementation</param>
        private void _CreateNewGraph(object parameter)
        {
            Graph = new NetGraph(true);
           // _existingVertices.Add(new NetVertex(1));
            _VList.Add(1);
            Graph.AddVertex(new NetVertex(1));
            NotifyPropertyChanged("Graph");
            _IDCount = 1; 
        }
        /// <summary>
        /// User can only open a Graph if no new graph is created otherwise
        /// user have to restart the application.
        /// </summary>
        /// <param name="parameter">not required</param>
        /// <returns></returns>
        private bool _CanOpenGraph(object parameter)
        {
            if (Graph == null) return true;
            return false;
        }
        /// <summary>
        /// implements logic to open an existing graph from a *.grf file
        /// </summary>
        /// <param name="parameter">not required</param>
        private void _OpenGraph(object parameter)
        {
            string param = parameter.ToString();
            if (param.Equals("Networkx"))
            {
                this._OpenNetworkXGraph();
            }
            else if (param.Equals("Graph"))
            {
                this._OpenDefaultGraph();
            }
        }
        private void _OpenDefaultGraph()
        {
            string[] lines = null;
           // Create an open file dialog box and only show *.grf files.
            try
            {
                OpenFileDialog openDlg = new OpenFileDialog();
                openDlg.Filter = "Graph File |*.grf";
                //read all lines of file
                if (true == openDlg.ShowDialog())
                {
                    lines = File.ReadAllLines(openDlg.FileName);
                }
                _openedFileName = openDlg.FileName;
            }
            catch (IOException ex)
            {
                ExceptionMessage.Show("Could not open file\n" + ex.ToString());
            }

            Graph = new NetGraph(true);
          
            //following block of code try to read the file and create graph
            // to fully understant this code please read the graph file in notepad
            try
            {
                string[] IDs = lines[0].Split(',');
                
                foreach (string id in IDs)
                {
                    int currID = int.Parse(id);
                    _VList.Add(currID);
                    Graph.AddVertex(new NetVertex(currID));
                    
                }
                _IDCount = int.Parse(IDs[IDs.Length - 1]);

                string[] edges = lines[1].Split(',');
                string[] temp;
                int idTo, idFrom;
                int from = 0, to = 0;
                int count = Graph.VertexCount;
                foreach (string part in edges)
                {
                    temp = part.Split('-');
                    idFrom = int.Parse(temp[0]);
                    idTo = int.Parse(temp[1]);

                    for (int i = 0; i < count; i++)
                    {
                        if (Graph.Vertices.ElementAt(i).ID == idFrom) from = i;
                        if (Graph.Vertices.ElementAt(i).ID == idTo) to = i;
                    }

                    //   _AddNewGraphEdge(_existingVertices[--idTo], _existingVertices[--idFrom]);
                    _AddNewGraphEdge(Graph.Vertices.ElementAt(from), Graph.Vertices.ElementAt(to));
                }
                string[] commodities = lines[2].Split(',');
                if (commodities.Length > 0) CommodityList.RemoveAt(0);
                foreach (string part in commodities)
                {
                    temp = part.Split('-');
                    from = int.Parse(temp[0]);
                    to = int.Parse(temp[1]);
                    if (from != to)
                    {
                        CommoditiesEntryViewModel cvm = new CommoditiesEntryViewModel();
                        cvm.OriginID = from;
                        cvm.DestinationID = to;
                        cvm.DemandVal = int.Parse(temp[2]);
                        cvm.CombList = _VList;
                        cvm.ParentList = CommodityList;
                        CommodityList.Add(cvm);
                    }
                }
                NotifyPropertyChanged("Graph");
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Something wrong with the Data in the File\n" + ex.ToString());
                Graph = null;
            }
        }

        private void _OpenNetworkXGraph()
        {
            string[] lines = null;
            int CommodityNum = this._NumberOfCommoditiesVal;
            int MinComm = this._MinimumDemandVal;
            int MaxComm = this._MaximumDemandVal + 1;
            int nodeID = 0;
            HashSet<int> nodeList = new HashSet<int>();
            HashSet<int> edgeOutNodes = new HashSet<int>();
            Random random = new Random();
            // Create an open file dialog box and only show *.grf files.
            try
            {
                OpenFileDialog openDlg = new OpenFileDialog();
                openDlg.Filter = "Text File |*.txt";
                //read all lines of file
                if (true == openDlg.ShowDialog())
                {
                    lines = File.ReadAllLines(openDlg.FileName);
                }
                _openedFileName = openDlg.FileName;
            }
            catch (IOException ex)
            {
                ExceptionMessage.Show("Could not open file\n" + ex.ToString());
            }
            Graph = new NetGraph(true);

            try
            {
                foreach (string line in lines)
                {
                    string[] IDs = line.Split(' ');
                    
                    if (int.TryParse(IDs[0], out nodeID))
                    {
                        foreach (string id in IDs)
                        {
                            int currID = int.Parse(id);
                            if(nodeList.Add(currID))
                            {
                                _VList.Add(currID);
                                Graph.AddVertex(new NetVertex(currID));
                            }
                        }
                    }
                }
                int from = 0, to = 0, curID = 0;
                int count = Graph.VertexCount;

                foreach (string line in lines)
                {
                    string[] IDs = line.Split(' ');

                    if (int.TryParse(IDs[0], out nodeID))
                    {
                        for(int j = 1; j < IDs.Length; j++)
                        {
                            edgeOutNodes.Add(nodeID);
                            curID = int.Parse(IDs[j]);
                            for (int i = 0; i < count; i++)
                            {
                                if (Graph.Vertices.ElementAt(i).ID == nodeID) from = i;
                                if (Graph.Vertices.ElementAt(i).ID == curID) to = i;
                            }
                            
                            this._AddNewGraphEdge(Graph.Vertices.ElementAt(from), Graph.Vertices.ElementAt(to));
                        }
                    }
                }

                int x = 0;
                bool valid = false;
                int randomDemand = 0;
                int randomID = 0;
                CommodityList.RemoveAt(0);
                for (int i = 0; i < CommodityNum; i++)
                {
                    if (x == edgeOutNodes.Count) x = 0;
                    valid = false;
                    while (!valid)
                    {
                        randomDemand = random.Next(MinComm, MaxComm);
                        randomID = random.Next(0, nodeList.Count);
                        if (edgeOutNodes.ElementAt(x) != nodeList.ElementAt(randomID))
                        {
                            valid = true;
                            CommoditiesEntryViewModel cvm = new CommoditiesEntryViewModel();
                            cvm.OriginID = edgeOutNodes.ElementAt(x);
                            cvm.DestinationID = nodeList.ElementAt(randomID);
                            cvm.DemandVal = randomDemand;
                            cvm.CombList = _VList;
                            cvm.ParentList = CommodityList;
                            CommodityList.Add(cvm);

                        }
                    }
                    x++;
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
       /// if graph is not null and has atleast one edge, it can be saved
       /// </summary>
       /// <param name="parameter">not required</param>
       /// <returns></returns>
        private bool _CanSaveGraph(object parameter)
        {
            if (Graph != null && Graph.EdgeCount > 0) return true;
            return false;
        }
        /// <summary>
        /// this method implements logic to save the graph to .grf file
        /// first line of file will have vertex info
        /// second line will have edges info
        /// third line will have commodities and their respective demands
        /// </summary>
        /// <param name="parameter"></param>
        private void _SaveGraph(object parameter)
        {
            int EdgeCount = Graph.EdgeCount;
            string[] txtArray = new string[3];
            foreach (NetVertex vx in Graph.Vertices) txtArray[0] += vx.ID.ToString() + ",";
            txtArray[0] = txtArray[0].TrimEnd(',');
           
            for (int i = 0; i < EdgeCount; i++)
            {
                txtArray[1] += Graph.Edges.ElementAt(i).Source.ID.ToString() + "-";
                txtArray[1] += Graph.Edges.ElementAt(i).Target.ID.ToString() + ",";
            }
        
            txtArray[1] = txtArray[1].TrimEnd(',');
            foreach (CommoditiesEntryViewModel cvm in CommodityList)
            {
                txtArray[2] += cvm.OriginID + "-" + cvm.DestinationID + "-" + cvm.DemandVal + ",";
            }
            txtArray[2] = txtArray[2].TrimEnd(',');
            try
            {
                SaveFileDialog saveDlg = new SaveFileDialog();
                saveDlg.Filter = "Graph File |*.grf";
                saveDlg.FileName = _openedFileName;
                saveDlg.OverwritePrompt = false;
                saveDlg.CheckFileExists = false;
                if (true == saveDlg.ShowDialog())
                {
                    File.WriteAllLines(saveDlg.FileName, txtArray);
                }
                _openedFileName = saveDlg.FileName;
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Exception saving graph " + ex.ToString());
            }
        }
        /// <summary>
        /// Refresh button is only enable if Graph is not null
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private bool _CanRefresh(object parameter)
        {
            if (Graph != null) return true;
            return false;
        }
        /// <summary>
        /// this command refresh Graph if Graph is not loaded properly then
        /// user should press this button
        /// </summary>
        /// <param name="parameter"></param>
        private void _RefreshGraph(object parameter)
        {
            NotifyPropertyChanged("Graph");
            if(_layoutAlgorithmType == null) LayoutAlgorithmType = "LinLog";
        }
        #endregion 

        #region Vertex Commands
        /// <summary>
        /// user can add new vertex if the graph is not null
        /// i.e. graph has been created
        /// </summary>
        /// <param name="parameter">not required</param>
        /// <returns></returns>
        private bool _CanAddVertex(object parameter)
        {
            if (Graph == null)return false;
            return true;
        }
        /// <summary>
        /// add new vertex with new numerical ID
        /// </summary>
        /// <param name="parameter"></param>
        private void _AddVertex(object parameter)
        {
         //   _existingVertices.Add(new NetVertex(_IDCount + 1));
            try
            {
                _VList.Add(_IDCount + 1);
                Graph.AddVertex(new NetVertex(_IDCount + 1));
                NotifyPropertyChanged("Graph");
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Exception when adding new vertex: " + ex.ToString());
            }
            if(_layoutAlgorithmType == null) LayoutAlgorithmType = "LinLog";
            _IDCount++;
        }
        /// <summary>
        /// user can remove vertex if there are more than one vertex in the graph
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private bool _CanRemoveVertex(object parameter)
        {
            if (Graph == null || Graph.VertexCount < 2)return false;
            return true;
        }
        /// <summary>
        /// Mode is set to 3, which is a remore vertex mode
        /// deletion is handled by the node click event
        /// when Mode is 3, the first vertex that user click on will be deleted
        /// </summary>
        /// <param name="parameter"></param>
        private void _RemoveVertex(object parameter)
        {
            _Mode = 3;
        }
        #endregion

        #region Edge Commands
        /// <summary>
        /// user can add edge if there are 2 or more vertexes
        /// </summary>
        /// <param name="parameter">not required</param>
        /// <returns></returns>
        private bool _CanAddEdge(object parameter)
        {
            if (Graph == null || Graph.VertexCount < 2) return false;
            return true;
        }
        /// <summary>
        /// set mode to 1 which is add edge mode
        /// after pressing Add Edge button, user will click on two nodes to create an edge between them
        /// </summary>
        /// <param name="parameter">not required</param>
        private void _AddEdge(object parameter)
        {
            _Mode = 1;
            _Counter = 0;
        }
        /// <summary>
        /// user can remove edge if there is any edged exist in the graph
        /// </summary>
        /// <param name="parameter">not required</param>
        /// <returns></returns>
        private bool _CanRemoveEdge(object parameter)
        {
            if (Graph != null && Graph.EdgeCount > 0) return true;
            return false;
        }
        /// <summary>
        /// set Mode to 2
        /// after pressing Remove Edge button, user will click two node which have edge between them
        /// if user click on wrong node, nothing will happen
        /// </summary>
        /// <param name="parameter">not required</param>
        private void _RemoveEdge(object parameter)
        {
            _Mode = 2;
            _Counter = 0;
        }
        #endregion

        #region AddCommodity Commands
        /* Commodity controls are created and deleted dynamically therefore these controls
         * are binded through different class called CommoditiesEntryModel. */
        /// <summary>
        /// if Graph has 2 or more nodes, allow user to create commodity
        /// </summary>
        /// <param name="parameter">not required</param>
        /// <returns></returns>
        private bool _canAddCommodity(object parameter)
        {
            if (Graph == null || Graph.VertexCount < 2) return false;
            return true;
        }
        /// <summary>
        /// Commodity controls are created and deleted dynamically therefore these controls
        /// are binded through different class called CommoditiesEntryModel.
        /// Each row of controls has new instance of EntryModel class.
        /// Every new commodity is added to CommodityList
        /// </summary>
        /// <param name="parameter"></param>
        private void _AddNewCommodity(object parameter)
        {
            CommoditiesEntryViewModel CommView = new CommoditiesEntryViewModel();
            CommView.CombList = _VList;
            CommView.ParentList = CommodityList;
            CommodityList.Add(CommView);
        }
        #endregion

        #region start Commands
        /// <summary>
        /// start button starts simulation
        /// this button is only enabled if there are some vertex and edges on the graph
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private bool _CanStart(object parameter)
        {
            if (Graph != null && Graph.VertexCount > 1 && Graph.EdgeCount > 0) return true;
            return false;
        }
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="parameter"></param>
        private void _Start(object parameter)
        {
            
            int[] vertexes = _VList.ToArray(); 
            int[,] edges = new int[Graph.EdgeCount,2];
            double[,] commodities = new double[CommodityList.Count, 3];
            int i = 0;
            bool commoditiesExist = false;
            for (i = 0; i < Graph.EdgeCount; i++)
            {
                edges[i, 0] = Graph.Edges.ElementAt(i).Source.ID;
                edges[i, 1] = Graph.Edges.ElementAt(i).Target.ID;
            }
            i = 0;
            foreach (CommoditiesEntryViewModel cvm in CommodityList)
            {
                if (cvm.OriginID != cvm.DestinationID && cvm.DemandVal > 0)
                {
                    commodities[i, 0] = cvm.OriginID;
                    commodities[i, 1] = cvm.DestinationID;
                    commodities[i, 2] = cvm.DemandVal;
                    commoditiesExist = true;
                }
                i++;
            }
            if (commoditiesExist)
            {
                
              //  _Simulation.Generator(Graph);
                SimulationSingleton.SimulationInstance.Generator(vertexes, edges, commodities,_ProfitFactorVal);
            }
            else
            {
                ExceptionMessage.Show("no commodities exist");
            }

        }
        #endregion

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
            _NumberOfCommoditiesVal = 10;
            _NumberOfCommodities = "10";
            _MinimumDemand = "5";
            _MaximumDemand = "15";
            _MinimumDemandVal = 5;
            _MaximumDemandVal = 15;
            _CommodityList = new ObservableCollection<CommoditiesEntryViewModel>();
            _VList = new ObservableCollection<int>();
            _AddNewCommodity(null);
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
            if (NodeID > 0)
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

        #region Public Properties
        //These properties are binded with GUI interface. 

        public string LayoutAlgorithmType
        {
            get { return _layoutAlgorithmType; }
            set
            {
                _layoutAlgorithmType = value;
                NotifyPropertyChanged("LayoutAlgorithmType");
            }
        }
        
        public NetGraph Graph
        {
            get { return _graph; }
            set
            {
                _graph = value;
                NotifyPropertyChanged("Graph");
            }
        }

        public string ProfitFactor
        {
            get
            {
                return _ProfitFactor;
            }
            set
            {
                _ProfitFactor = value;
                try
                {
                    _ProfitFactorVal = double.Parse(_ProfitFactor);
                }
                catch (Exception ex)
                {
                    _ProfitFactorVal = 1;
                    ExceptionMessage.Show("Profit has invalid value: " + ex.ToString());
                }
            }
        }

        public string NumberOfCommodities
        {
            get
            {
                return _NumberOfCommodities;
            }
            set
            {
                _NumberOfCommodities = value;
                try
                {
                    _NumberOfCommoditiesVal = int.Parse(_NumberOfCommodities);
                }
                catch (Exception ex)
                {
                    _NumberOfCommoditiesVal = 10;
                    ExceptionMessage.Show("Number of Commodities has invalid value: " + ex.ToString());
                }
            }
        }

        public string MinimumDemandValue
        {
            get
            {
                return _MinimumDemand;
            }
            set
            {
                _MinimumDemand = value;
                try
                {
                    _MinimumDemandVal = int.Parse(_MinimumDemand);
                }
                catch (Exception ex)
                {
                    _MinimumDemandVal = 5;
                    ExceptionMessage.Show("Demand Range has invalid value: " + ex.ToString());
                }
            }
        }
        public string MaximumDemandValue
        {
            get
            {
                return _MaximumDemand;
            }
            set
            {
                _MaximumDemand = value;
                try
                {
                    _MaximumDemandVal = int.Parse(_MaximumDemand);
                }
                catch (Exception ex)
                {
                    _MaximumDemandVal = 15;
                    ExceptionMessage.Show("Demand Range has invalid value: " + ex.ToString());
                }
            }
        }
        public ICommand OpenGraph
        {
            get
            {
                if (_openGraph == null)
                {
                    _openGraph = new RelayCommand(this._OpenGraph, this._CanOpenGraph);
                }
                return _openGraph;
            }
        }

        public ICommand RefreshGraph
        {
            get
            {
                if (_Refresh == null)
                {
                    _Refresh = new RelayCommand(this._RefreshGraph, this._CanRefresh);
                }
                return _Refresh;
            }
        }

        public ICommand SaveGraph
        {
            get
            {
                if (_saveGraph == null)
                {
                    _saveGraph = new RelayCommand(this._SaveGraph, this._CanSaveGraph);
                }
                return _saveGraph;
            }
        }

        public ICommand AddCommodity
        {
            get
            {
                if (_AddCommodity == null)
                {
                    _AddCommodity = new RelayCommand(this._AddNewCommodity, this._canAddCommodity);
                }
                return _AddCommodity;
            }
        }
        
        public ICommand CreateVertex
        {
            get
            {
                if (_createVertex == null)
                {
                    _createVertex = new RelayCommand(this._AddVertex,this._CanAddVertex);
                }
                return _createVertex;
            }
        }
        public ICommand RemoveVertex
        {
            get
            {
                if (_removeVertex == null)
                {
                    _removeVertex = new RelayCommand(this._RemoveVertex, this._CanRemoveVertex);
                }
                return _removeVertex;
            }
        }
        public ICommand CreateEdge
        {
            get
            {
                if (_createEdge == null)
                {
                    _createEdge = new RelayCommand(this._AddEdge, this._CanAddEdge);
                }
                return _createEdge;
            }
        }
        public ICommand RemoveEdge
        {
            get
            {
                if (_removeEdge == null)
                {
                    _removeEdge = new RelayCommand(this._RemoveEdge, this._CanRemoveEdge);
                }
                return _removeEdge;
            }
        }
        public ICommand CreateGraph
        {
            get
            {
                if (_createGraph == null)
                {
                    _createGraph = new RelayCommand(this._CreateNewGraph, this._CanCreateNewGraph);
                }
                return _createGraph;
            }
        }
        public ICommand Start
        {
            get
            {
                if (_start == null)
                {
                    _start = new RelayCommand(this._Start, this._CanStart);
                }
                return _start;
            }
        }

        public ObservableCollection<CommoditiesEntryViewModel> CommodityList
        {
            get
            {
                return _CommodityList;
            }
            set
            {
                _CommodityList = value;
                NotifyPropertyChanged("Comm");
            }
        }
      
        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// this method is required in MVVM to communicate with GUI
        /// this method lets the GUI know when any property changes so that
        /// GUI can update itself
        /// </summary>
        /// <param name="info"></param>
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
              //  PropertyChanged(
            }
        }

        #endregion
        
    }
}
