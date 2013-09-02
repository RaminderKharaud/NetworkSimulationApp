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

namespace NetworkSimulationApp.NonThreadSimulation
{
    /// <summary>
    /// Interaction logic for NonThreadedSimulationWin.xaml
    /// </summary>
    public partial class NonThreadedSimulationWin : Window
    {
        bool cancelSim;
        public NonThreadedSimulationWin()
        {
            cancelSim = false;
            InitializeComponent();
        }

        public void WriteOutput(string output)
        {
            Application.Current.Dispatcher.BeginInvoke(
              DispatcherPriority.Background,
              new Action(() => this.txtOutput.Text += output + '\n'));
        }

        public void StartSim(float singleFlow)
        {
            int length = NodeList.Nodes.Count;
            int WakeUpRange = NodeActivator.WakeUpNodeList.Count();
            bool equilibrium = false;
            NodeActivator.TotalWakeUpCalls = 0;
            Random random = new Random();
            NodeActivator.StartTime = DateTime.Now;
            int rand = 0, i = 0;

            this.txtOutput.Text += "Total Single Edge Flow: " + singleFlow + "\n\n";
            this.txtOutput.Text += "Performing Simulation...\n\n";

            for (i = 0; i < length; i++)
            {
                NodeList.Nodes[i].FlowReciever();
            }

            while (true)
            {
                rand = random.Next(0, WakeUpRange);
                equilibrium = NodeActivator.loopWakeUpCall(rand);
                if (equilibrium) break;
            }
        }

        private void btnSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (!cancelSim)
            {
                cancelSim = true;
                this.txtOutput.Text += "\n\n Simulation Cancled";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancelSim = true; ;
        }
    }
}
