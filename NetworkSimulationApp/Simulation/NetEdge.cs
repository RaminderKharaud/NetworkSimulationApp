using QuickGraph;
using System.Diagnostics;
using System.ComponentModel;
using System;

namespace NetworkSimulationApp
{
    /// <summary>
    /// A simple identifiable edge.
    /// </summary>
    [DebuggerDisplay("{Source.ID} -> {Target.ID}")]

    class NetEdge : Edge<NetVertex>, INotifyPropertyChanged
    {
        private string _id;

        public string ID
        {
            get { return _id; }
            set
            {
                _id = value;
                NotifyPropertyChanged("ID");
            }
        }

         public NetEdge(string id, NetVertex source, NetVertex target)
            : base(source, target)
        {
            ID = id;
        }


        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
         
    }
}
