using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;


namespace NetworkSimulationApp
{
    class CommoditiesEntryViewModel : INotifyPropertyChanged
    {
        private string _Demand;
        public int DemandVal;
        private int _OriginID, _DestinationID;
        private ObservableCollection<int> _CombList;
        public ObservableCollection<CommoditiesEntryViewModel> ParentList;
        private ICommand _delete;
        public CommoditiesEntryViewModel()
        {
            _OriginID = 0;
            _DestinationID = 0;
            DemandVal = 0;
            _Demand = null;
            _CombList = new ObservableCollection<int>();
        }

        private bool _CanDelete(object param)
        {
            if (DemandVal > 0) return true;
            return false;
        }
        private void _DeleteMe(object param)
        {
            ParentList.Remove(this);
        }
        #region Properties
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
                    DemandVal = int.Parse(_Demand);
                }
                catch (Exception ex)
                {
                    DemandVal = 0;
                    ExceptionMessage.Show("Demand has invalid value");
                }
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
