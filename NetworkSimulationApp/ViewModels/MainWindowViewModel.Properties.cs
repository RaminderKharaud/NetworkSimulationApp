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
    ///                         This class has public properties that GUI communicates through
    /// </summary>

    partial class MainWindowViewModel : INotifyPropertyChanged
    {

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
                    _ProfitFactorVal = float.Parse(_ProfitFactor);
                }
                catch (Exception ex)
                {
                    _ProfitFactorVal = 1;
                    _ProfitFactor = "1";
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
                    _NumberOfCommodities = "10";
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
                    _MinimumDemand = "5";
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
                    _MaximumDemand = "15";
                    ExceptionMessage.Show("Demand Range has invalid value: " + ex.ToString());
                }
            }
        }
        public string NodeFailureRate
        {
            get
            {
                return _NodeFailureRate;
            }
            set
            {
                _NodeFailureRate = value;
                try
                {
                    _NodeFailureRateVal = float.Parse(_NodeFailureRate);
                }
                catch (Exception ex)
                {
                    _NodeFailureRateVal = 0;
                    _NodeFailureRate = "0";
                    ExceptionMessage.Show("Node Failure Rate has invalid value: " + ex.ToString());
                }
            }
        }
        public string MaxDegree
        {
            get
            {
                return _MaxDegree;
            }
            set
            {
                _MaxDegree = value;
                try
                {
                    _MaxDegreeVal = int.Parse(_MaxDegree);
                }
                catch (Exception ex)
                {
                    _MaxDegreeVal = 10;
                    _MaxDegree = "10";
                    ExceptionMessage.Show("Max Degree has invalid value: " + ex.ToString());
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

        public ICommand SimulationMode
        {
            get
            {
                if (_simulationMode == null)
                {
                    _simulationMode = new RelayCommand(this._SimulationMode, this._CanChangeSimulationMode);
                }
                return _simulationMode;
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

        public ICommand DrawGraph
        {
            get
            {
                if (_drawGraph == null)
                {
                    _drawGraph = new RelayCommand(this._DrawGraph, this._CanDrawGraph);
                }
                return _drawGraph;
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
                    _createVertex = new RelayCommand(this._AddVertex, this._CanAddVertex);
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
