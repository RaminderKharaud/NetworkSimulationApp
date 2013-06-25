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
namespace NetworkSimulationApp
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Class Data
        private int _IDCount;
        private int _Counter;
        private int _FistNodeID;
        private string _layoutAlgorithmType;
        private string _openedFileName;
        private NetGraph _graph;
        private ICommand _createGraph;
        private ICommand _createVertex; 
        private ICommand _createEdge;
        private ICommand _removeVertex;
        private ICommand _removeEdge;
        private ICommand _openGraph;
        private ICommand _saveGraph;
        private ICommand _AddCommodity;
        private ICommand _start;
        private bool _AddEdgeMode;
        private bool _RemoveEdgeMode;
        private bool _RemoveVertexMode;
        private int _Mode;
       // private List<NetVertex> _existingVertices;
        private AdHocNetSimulation _Simulation;
        private ObservableCollection<CommoditiesEntryViewModel> _CommodityList;
        private ObservableCollection<int> _VList;
        #endregion

        #region Constructors
        public MainWindowViewModel()
        {
            this._Intialization();
        }
        #endregion

        #region Commands

        #region Graph Commands
        private bool _CanCreateNewGraph(object parameter)
        {
            if(Graph == null) return true;
            return false;
        }
        private void _CreateNewGraph(object parameter)
        {
            Graph = new NetGraph(true);
           // _existingVertices.Add(new NetVertex(1));
            _VList.Add(1);
            Graph.AddVertex(new NetVertex(1));
            NotifyPropertyChanged("Graph");
            _IDCount = 1; 
        }
        private bool _CanOpenGraph(object parameter)
        {
            if (Graph == null) return true;
            return false;
        }
        private void _OpenGraph(object parameter)
        {
            string dataFromFile = "";
           // Create an open file dialog box and only show XAML files.
            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter = "Graph File |*.grf";
            // Did they click on the OK button?
            if (true == openDlg.ShowDialog())
            {
                // Load all text of selected file.
                dataFromFile = File.ReadAllText(openDlg.FileName);
            }
            _openedFileName = openDlg.FileName;

            Graph = new NetGraph(true);
            string[] Parts = dataFromFile.Split(':');
            int VertexCount = int.Parse(Parts[0]);

            for (int i = 1; i <= VertexCount; i++)
            {
              //  _existingVertices.Add(new NetVertex(i));
                _VList.Add(i);
                Graph.AddVertex(new NetVertex(i));
            }
            
            Parts = Parts[1].Split(',');
            string[] temp;
            int idTo, idFrom;
            int from = 0, to = 0;
            int count = Graph.VertexCount;
            foreach (string part in Parts)
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
            
            NotifyPropertyChanged("Graph");
        }
       
        private bool _CanSaveGraph(object parameter)
        {
            if (Graph != null && Graph.EdgeCount > 0) return true;
            return false;
        }
        private void _SaveGraph(object parameter)
        {
            int EdgeCount = Graph.EdgeCount;
            String txtData = Graph.VertexCount.ToString() + ":";

            for (int i = 0; i < EdgeCount; i++)
            {
                txtData += Graph.Edges.ElementAt(i).Source.ID.ToString() + "-";
                txtData += Graph.Edges.ElementAt(i).Target.ID.ToString() + ",";
            }
            txtData = txtData.TrimEnd(',');
            try
            {
                SaveFileDialog saveDlg = new SaveFileDialog();
                saveDlg.Filter = "Graph File |*.grf";
                saveDlg.FileName = _openedFileName;
                saveDlg.OverwritePrompt = false;
                saveDlg.CheckFileExists = false;
                if (true == saveDlg.ShowDialog())
                {
                    File.WriteAllText(saveDlg.FileName, txtData);
                }
                _openedFileName = saveDlg.FileName;
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Exception saving graph " + ex.ToString());
            }
        }
        #endregion 

        #region Vertex Commands
        private bool _CanAddVertex(object parameter)
        {
            if (Graph == null)return false;
            return true;
        }
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
        private bool _CanRemoveVertex(object parameter)
        {
            if (Graph == null || Graph.VertexCount < 2)return false;
            return true;
        }
        private void _RemoveVertex(object parameter)
        {
            _Mode = 3;
        }
        #endregion

        #region Edge Commands
        private bool _CanAddEdge(object parameter)
        {
            if (Graph == null || Graph.VertexCount < 2) return false;
            return true;
        }
        private void _AddEdge(object parameter)
        {
            _Mode = 1;
            _Counter = 0;
        }
        private bool _CanRemoveEdge(object parameter)
        {
            if (Graph != null && Graph.EdgeCount > 0) return true;
            return false;
        }
        private void _RemoveEdge(object parameter)
        {
            _Mode = 2;
            _Counter = 0;
        }
        #endregion

        #region AddCommodity Commands
        private bool _canAddCommodity(object parameter)
        {
            if (Graph == null || Graph.VertexCount < 2) return false;
            return true;
        }
        private void _AddNewCommodity(object parameter)
        {
            CommoditiesEntryViewModel CommView = new CommoditiesEntryViewModel();
            CommView.CombList = _VList;
            CommView.ParentList = CommodityList;
            CommodityList.Add(CommView);
        }
        #endregion

        #region start Commands

        private bool _CanStart(object parameter)
        {
            if (Graph != null && Graph.VertexCount > 1 && Graph.EdgeCount > 0) return true;
            return false;
        }
        private void _Start(object parameter)
        {
          _Simulation.Generator(Graph);
        //    AdHocNetSimulation simulation = new AdHocNetSimulation();
        //    simulation.Generator(this);
        }
        #endregion

        #endregion

        #region private methods
        private void _Intialization()
        {
            _IDCount = 0;
            _Counter = 0;
            _FistNodeID = 0;
            //  _existingVertices = new List<NetVertex>();
            _Simulation = new AdHocNetSimulation();
            _AddEdgeMode = false;
            _RemoveEdgeMode = false;
            _RemoveVertexMode = false;
            _openedFileName = "";
            _Mode = 0;
            _CommodityList = new ObservableCollection<CommoditiesEntryViewModel>();
            _VList = new ObservableCollection<int>();
            _AddNewCommodity(null);
        }
        private NetEdge _AddNewGraphEdge(NetVertex from, NetVertex to)
        {
            string edgeString = string.Format("{0}-{1} Connected", from.ID.ToString(), to.ID.ToString());
            NetEdge newEdge = new NetEdge(edgeString, from, to);
            Graph.AddEdge(newEdge);
            return newEdge;
        }
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
        #endregion

        #region Public Properties

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
