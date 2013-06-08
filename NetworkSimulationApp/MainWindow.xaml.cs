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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel;
        public MainWindow()
        {
            this.DataContext = ViewModel;
            InitializeComponent();
        }

        private void GraphNode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int NodeID = ((NetVertex)((ContentPresenter)sender).Content).ID;

            ((MainWindowViewModel)this.DataContext).NodeClickLogic(NodeID);
        }

        private void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            
        }

    }
}
