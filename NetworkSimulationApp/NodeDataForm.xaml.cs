using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
namespace NetworkSimulationApp
{
    /// <summary>
    /// Interaction logic for NodeDataForm.xaml
    /// </summary>
    public partial class NodeDataForm : Window
    {
        int NodeID;
       
        public NodeDataForm()
        {
            InitializeComponent();
        } 
       
        public NodeDataForm(int ID)
        {
            InitializeComponent();
            NodeID = ID;
            this.Title = "Node ID: " + ID.ToString();
        }

    }
}
