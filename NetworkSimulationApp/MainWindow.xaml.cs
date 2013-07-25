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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetworkSimulationApp
{
    /// <summary>
    /// File:                   MainWindow.xaml.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       Feb 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Todo:                   This class is handling click event for vertex which should not be the case in MVVM. This
    ///                         event needs to be moved to MainWindowViewModel
    ///                         
    /// Purpose:                The gui of this project has been implemented using WPF technology and for best practice,
    ///                         its implemented using MVVM model. Therefore this code behind file not suppose to implement 
    ///                         any GUI logic
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += ((MainWindowViewModel)this.DataContext).MainWindow_Closing;
        }
        /// <summary>
        /// Handles click event of vertex and send vertex ID to NodeClickLogic method in MainWindowViewModel.ca
        /// TODO: move this logic to MVVM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GraphNode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int NodeID = ((NetVertex)((ContentPresenter)sender).Content).ID;

            ((MainWindowViewModel)this.DataContext).NodeClickLogic(NodeID);
        }

    }
}
