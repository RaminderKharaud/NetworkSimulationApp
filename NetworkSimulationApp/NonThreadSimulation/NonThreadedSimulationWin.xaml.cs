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
using System.Windows.Threading;
using System.Threading;
using NetworkSimulationApp.AdHocMessageBox;

namespace NetworkSimulationApp.NonThreadSimulation
{
    /// <summary>
    /// Interaction logic for NonThreadedSimulationWin.xaml
    /// </summary>
    public partial class NonThreadedSimulationWin : Window
    {
        bool cancelSim;
        CancellationTokenSource CommonCalToken;
        public NonThreadedSimulationWin()
        {
            cancelSim = false;
            InitializeComponent();
            this.Closing += Window_Closing;
        }

        public void WriteOutput(string output)
        {
            Application.Current.Dispatcher.BeginInvoke(
              DispatcherPriority.Background,
              new Action(() => this.txtOutput.Text += output + '\n'));
        }
        //create a new thread for simulation and call Loop Simulation
        public void StartSim()
        {
            NodeActivator.SimWin = this;
            CommonCalToken = new CancellationTokenSource();
            LoopSimulation loop = new LoopSimulation();

            this.WriteOutput("\nTotal Single Edge Flow: " + NodeActivator.SingleEdgeFlow + "\n\n");
            this.WriteOutput("Performing Simulation...\n\n");

            Task.Factory.StartNew(((LoopSimulation)loop).start, CommonCalToken.Token);
        }

        public void CancelThread()
        {
            CommonCalToken.Cancel();
        }

        private void btnSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (!cancelSim)
            {
                cancelSim = true;
               // CommonCalToken.Cancel();
                this.txtOutput.Text += "\n\n Simulation Cancled";
                NodeActivator.Cancel = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!NodeActivator.Cancel)
            {
                ExceptionMessage.Show("Simulation still running");
                e.Cancel = true;
            }
        }
    }
}
