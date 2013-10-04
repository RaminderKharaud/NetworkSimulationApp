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
using System.Windows.Threading;
using NetworkSimulationApp.AdHocMessageBox;

namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// File:                   SimulationWin.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       August 2013
    /// 
    /// Revision    1.1         No Revision Yet
    ///                         
    /// Purpose:                Interaction logic for SimulationWin.xaml
    /// </summary>
    public partial class SimulationWin : Window
    {
        List<Task> taskList;
        CancellationTokenSource CommonCalToken;
        bool canceled;
        public SimulationWin()
        {
            taskList = new List<Task>();
            canceled = false;
            InitializeComponent();
          //  this.Closing += Window_Closing;
        }
        /// <summary>
        /// This method writes output to the simulation window
        /// </summary>
        /// <param name="output">line that will be written to window</param>
        public void WriteOutput(string output)
        {
            Application.Current.Dispatcher.BeginInvoke(
              DispatcherPriority.Background,
              new Action(() => this.txtOutput.Text += output + '\n'));
        }
        /// <summary>
        /// This method actually starts the thread simulation by creating 
        /// a new thread for each node in the static node list and a separate 
        /// thread for node activator.
        /// </summary>
        /// <param name="singleFlow"></param>
        public void StartSim(float singleFlow)
        {
            int result = 0, id = 0;
            CommonCalToken = new CancellationTokenSource();
            taskList.Clear();
          //  ((AdHocNode)NodeList.Nodes.ElementAt(0).Value).FlowReady = true;
        //    Thread node1 = new Thread(new ThreadStart(((AdHocNode)Nodes[1]).Start));
            this.txtOutput.Text += "Total Single Edge Flow: " + singleFlow + "\n\n";

            foreach (KeyValuePair<int, int> pair in NodeActivator.NodeSplitBySourceLog)
            {
                id = NodeList.Nodes[pair.Key].GraphID;
                this.txtOutput.Text += "Node: " + id + " was split by sources\n";
            }
            foreach (KeyValuePair<int, int> pair in NodeActivator.NodeSplitByTargetLog)
            {
                id = NodeList.Nodes[pair.Key].GraphID;
                this.txtOutput.Text += "Node: " + id + " was split by targets\n";
            }

            this.txtOutput.Text += "\nPerforming Simulation...\n\n";
            try
            {
                foreach (KeyValuePair<int, AdHocNode> pair in NodeList.Nodes)
                {
                  taskList.Add(Task.Factory.StartNew(((AdHocNode)pair.Value).Start,CommonCalToken.Token));
                //    taskList.Add(Task.Factory.StartNew(() => ((AdHocNode)pair.Value).Start(), CommonCalToken.Token));
                }
                taskList.Add(Task.Factory.StartNew(NodeActivator.Start, CommonCalToken.Token));
            }
            catch (ThreadStateException e)
            {
                Console.WriteLine(e);  // Display text of exception
                result = 1;            // Result says there was an error
            }
            catch (ThreadInterruptedException e)
            {
                Console.WriteLine(e);  // This exception means that the thread
                // was interrupted during a Wait
                result = 1;            // Result says there was an error
            }

            // return code to the parent process.
            Environment.ExitCode = result;
        }
        /// <summary>
        /// cancel thread through cancelation token
        /// </summary>
        public void CancleThreads()
        {
           // CommonCalToken.Cancel();
            NodeActivator.Cancel = true;
            canceled = true;
        }
        
        /// <summary>
        /// stop simulation if its not already stopped or finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSimulation_Click(object sender, RoutedEventArgs e)
        {  
            if (!canceled)
            {
                NodeActivator.Cancel = true;
                this.CancleThreads();
                this.txtOutput.Text += "\n\n Simulation Cancled";
            }
        }
        /// <summary>
        /// closing window will also stop the simulation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!NodeActivator.Cancel)
            {
                e.Cancel = true;
                ExceptionMessage.Show("Simualtion still running");
            }
        }
    }
}
