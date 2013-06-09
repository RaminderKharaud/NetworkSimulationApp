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
    /// Interaction logic for SimulationWin.xaml
    /// </summary>
    public partial class SimulationWin : Window
    {
        List<Task> taskList;
        CancellationTokenSource CommonCalToken;
        
        public SimulationWin()
        {
            taskList = new List<Task>(); 
            InitializeComponent();
        }

        public void StartSim()
        {
            int result = 0;
            CommonCalToken = new CancellationTokenSource();
            taskList.Clear();
            ((AdHocNode)NodeList.Nodes.ElementAt(0).Value).FlowReady = true;
        //    Thread node1 = new Thread(new ThreadStart(((AdHocNode)Nodes[1]).Start));
            
            try
            {
                foreach (KeyValuePair<int, AdHocNode> pair in NodeList.Nodes)
                {
                  taskList.Add(Task.Factory.StartNew(((AdHocNode)pair.Value).Start,CommonCalToken.Token));
                //    taskList.Add(Task.Factory.StartNew(() => ((AdHocNode)pair.Value).Start(), CommonCalToken.Token));
                }
               
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
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CommonCalToken.Cancel();
            
        }
    }
}
