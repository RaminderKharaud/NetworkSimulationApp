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
    /// Purpose:                This is a partial class for MainWindowModel.
    ///                         This class hold the commands which are binded with the main GUI
    /// </summary>

    partial class MainWindowViewModel
    {
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
            if (Graph == null) return true;
            return false;
        }
        /// <summary>
        /// creates graph by creating only one node
        /// </summary>
        /// <param name="parameter">not required for implementation</param>
        private void _CreateNewGraph(object parameter)
        {
            Graph = new NetGraph(true);
            // _existingVertices.GetOrAdd(new NetVertex(1));
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
            else if (param.Equals("CreateGraph"))
            {
                this._CreateNetworkXGraph();
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
            // Create an open file dialog box and only show *.grf files.
            try
            {
                OpenFileDialog openDlg = new OpenFileDialog();
                openDlg.Filter = "Text File |*.doc;*.txt;*.grf";  //  *.doc";
                //read all lines of file
                if (true == openDlg.ShowDialog())
                {
                    lines = File.ReadAllLines(openDlg.FileName);
                }
                this._openedFileName = openDlg.FileName;
                this._DrawGraphWithCommodities(lines);
            }
            catch (IOException ex)
            {
                ExceptionMessage.Show("Could not open file\n" + ex.ToString());
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
       
        private bool _CanDrawGraph(object parameter)
        {
            if (Graph != null || MainWindowViewModel.CanDraw == false) return false;
            return true;
        }
        private void _DrawGraph(object parameter)
        {
            try
            {
                string[] lines = File.ReadAllLines(MainWindowViewModel.OutputFilePath);
                this._DrawGraphWithCommodities(lines);
            }
            catch (FileNotFoundException)
            {
                ExceptionMessage.Show("File not found. Output File Path may not have writing permissions for Python. Change out path or permissions and try again");
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Problem Reading output file" + ex.ToString());
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
            if (_layoutAlgorithmType == null) LayoutAlgorithmType = "LinLog";
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
            if (Graph == null) return false;
            return true;
        }
        /// <summary>
        /// add new vertex with new numerical ID
        /// </summary>
        /// <param name="parameter"></param>
        private void _AddVertex(object parameter)
        {
            //   _existingVertices.GetOrAdd(new NetVertex(_IDCount + 1));
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
            if (_layoutAlgorithmType == null) LayoutAlgorithmType = "LinLog";
            _IDCount++;
        }
        /// <summary>
        /// user can remove vertex if there are more than one vertex in the graph
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private bool _CanRemoveVertex(object parameter)
        {
            if (Graph == null || Graph.VertexCount < 2) return false;
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

        private bool _CanChangeSimulationMode(object parameter)
        {
            if (Graph == null) return false;
            return true;
        }
        private void _SimulationMode(object parameter)
        {
            this._SimMode = int.Parse(parameter.ToString());
        }

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
            int degree = 10;
           // int[,] edges = new int[Graph.EdgeCount, 2];
            float[,] commodities = new float[CommodityList.Count, 3];
            int i = 0;
            bool commoditiesExist = false;
          /*  for (i = 0; i < Graph.EdgeCount; i++)
            {
                edges[i, 0] = Graph.Edges.ElementAt(i).Source.ID;
                edges[i, 1] = Graph.Edges.ElementAt(i).Target.ID;
            } */
            i = 0;
            foreach (CommoditiesEntryViewModel cvm in CommodityList)
            {
                commodities[i, 0] = cvm.OriginID;
                commodities[i, 1] = cvm.DestinationID;
                commodities[i, 2] = cvm.DemandVal;
                commoditiesExist = true;
                i++;
            }
            if (commoditiesExist)
            {
                if (this._SimMode == 0)
                {
                    SimulationSingleton.NonThreadedSimulationInstance.Generator(vertexes, this._SimEdges, commodities, _ProfitFactorVal,
                    (float)this._MaximumDemandVal, degree, this._NodeFailureRateVal);
                }
                else
                {
                    SimulationSingleton.SimulationInstance.Generator(vertexes, this._SimEdges, commodities, _ProfitFactorVal, 
                    (float) this._MaximumDemandVal, degree, this._NodeFailureRateVal);
                }
               
            }
            else
            {
                ExceptionMessage.Show("no commodities exist");
            }

        }
        #endregion

        #endregion
    }
}
