using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkSimulationApp.AdHocMessageBox;

namespace NetworkSimulationApp
{
    /// <summary>
    /// File:                   CommoditiesEntryViewModel.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       June 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Todo:                   Nothing
    ///                         
    /// Purpose:                This class bind commodity controls which are created and deleted dynamically in the application.
    ///                         This Class is binded with CommodityControls item in WPF and it is also a Datacontext of DataTemple
    ///                         called "dtpCommodityControles"
    ///Knowldge Required        **In order to fully understand the implementation of this class, reader should have basic knowldge
    ///                         of WPF and MVVM pattern**
    /// </summary>
    class CommoditiesEntryViewModel : INotifyPropertyChanged
    {
        #region Class Data
        private string _Demand;
        private int _DemandVal;
        private int _OriginID, _DestinationID;
        /// <summary>
        /// any change to ObservableCOllection will update GUI
        /// </summary>
        private ObservableCollection<int> _CombList;  
        /// <summary>
        /// ParentList is the list of CommoditiesEntryViewModel from MainViewModel class
        /// By having reference to this list, this class can delete itself dynamically from GUI
        /// by removing itself from the combolist
        /// </summary>
        public ObservableCollection<CommoditiesEntryViewModel> ParentList;
        private ICommand _delete;
        #endregion
        //constructor
        public CommoditiesEntryViewModel()
        {
            _OriginID = 1;
            _DestinationID = 1;
            _DemandVal = 0;
            _Demand = null;
            _CombList = new ObservableCollection<int>();
        }
        /// <summary>
        /// User can only delete row of controls if there are two or more controls
        /// </summary>
        /// <param name="param">not required for implementation</param>
        /// <returns></returns>
        private bool _CanDelete(object param)
        {
            if (ParentList.Count > 1) return true;
            return false;
        }
        /// <summary>
        /// Delete itself from GUI dynamically if user press delete button
        /// </summary>
        /// <param name="param">not required for implementation</param>
        private void _DeleteMe(object param)
        {
            ParentList.Remove(this);
        }

        #region Properties
        //Properties binded with controls in the GUI
        public ICommand Delete
        {
            get
            {
                if (_delete == null)
                {
                    _delete = new RelayCommand(this._DeleteMe, this._CanDelete);
                }
                return _delete;
            }
        }
        /// <summary>
        /// Combobox in GUI updated everytime user add or remove vertex 
        /// </summary>
        public ObservableCollection<int> CombList
        {
            get
            {
                return _CombList;
            }
            set
            {
                _CombList = value;
                NotifyPropertyChanged("CombList");
            }
        }
        public int OriginID
        {
            get
            {
                return _OriginID;
            }
            set
            {
                _OriginID = value;
            }
        }
        public int DestinationID
        {
            get
            {
                return _DestinationID;
            }
            set
            {
                _DestinationID = value;
            }
        }
        /// <summary>
        /// Demand comes as string from textbox
        /// If user enters invalid data like nonnumeric value,
        /// error message will be shown when focus leave textbox
        /// </summary>
        public string StringDemand
        {
            get
            {
                return _Demand;
            }
            set
            {
                _Demand = value;
                try
                {
                    _DemandVal = int.Parse(_Demand);
                }
                catch (Exception ex)
                {
                    _DemandVal = 0;
                    ExceptionMessage.Show("Demand has invalid value");
                }
            }
        }
        public int DemandVal
        {
            get
            {
                return _DemandVal;
            }
            set
            {
                _DemandVal = value;
                StringDemand = _DemandVal.ToString();
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
