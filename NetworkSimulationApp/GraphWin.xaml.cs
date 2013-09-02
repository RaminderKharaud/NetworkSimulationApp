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
using System.IO;
using Microsoft.Win32;
using NetworkSimulationApp.AdHocMessageBox;
using System.Diagnostics;
using System.Security.AccessControl;

namespace NetworkSimulationApp
{
    /// <summary>
    /// Interaction logic for GraphWin.xaml
    /// </summary>
    public partial class GraphWin : Window
    {
        public static string FilePath = "C:\\Python33\\python.exe";
        public GraphWin()
        {
            InitializeComponent();
            this._setControls();
            txtPath.Text = FilePath;
            if(MainWindowViewModel.OutputFilePath == null)
                MainWindowViewModel.OutputFilePath = AppDomain.CurrentDomain.BaseDirectory + "GraphOutput.txt";
            txtFilePath.Text = MainWindowViewModel.OutputFilePath;
        }

        private void btnCreateGraph_Click(object sender, RoutedEventArgs e)
        {
            string GraphCommand = null;
            int n = 0, m = 0, seed = -1;
            float probability = 0;
            string location = null;
            try
            {
                location = txtFilePath.Text.Trim();
                MainWindowViewModel.OutputFilePath = location;
               // location = "C:\\Python33\\output\\GraphOutput.txt";
                n = int.Parse(txtNodes.Text.Trim());
                 
                if (txtSeed.Text.Trim().Length > 0) seed = int.Parse(txtSeed.Text.Trim());

                if (this.cmbGraph.SelectedIndex == 0)
                {
                    m = int.Parse(txtEdges.Text.Trim());

                    if (seed >= 0)
                    {
                        GraphCommand = "G = nx.barabasi_albert_graph(" + n + "," + m + "," + seed + ")";
                    }
                    else
                    {
                        GraphCommand = "G = nx.barabasi_albert_graph(" + n + "," + m + ")";
                    }
                }
                else if (this.cmbGraph.SelectedIndex == 1)
                {
                    probability = float.Parse(txtProbability.Text.Trim());

                    if (seed >= 0)
                    {
                        GraphCommand = "G = nx.erdos_renyi_graph(" + n + "," + probability + "," + seed + "," + "True)";
                    }
                    else
                    {
                        GraphCommand = "G = nx.erdos_renyi_graph(" + n + "," + probability + "," + "True)";
                    }
                }
                else if (this.cmbGraph.SelectedIndex == 2)
                {
                    probability = float.Parse(txtProbability.Text.Trim());

                    if (seed >= 0)
                    {
                        GraphCommand = "G = nx.gnp_random_graph(" + n + "," + probability + "," + seed + "," + "True)";
                    }
                    else
                    {
                        GraphCommand = "G = nx.gnp_random_graph(" + n + "," + probability + "," + "True)";
                    }
                }

                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = txtPath.Text.Trim();
                info.RedirectStandardInput = true;
                info.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = false;
                p.StartInfo = info;
                p.Start();

                using (StreamWriter sw = p.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine("import networkx as nx");
                        sw.WriteLine(GraphCommand);
                        sw.WriteLine("nx.write_adjlist(G,'" + location + "')");
                    }
                }
                
                p.WaitForExit();
                p.Close();
                MainWindowViewModel.CanDraw = true;
            }
            catch (Exception ex)
            {
                ExceptionMessage.Show("Problem Creating Graph: " + ex.ToString());
            }
            
        }
        
        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openDlg = new OpenFileDialog();
                openDlg.RestoreDirectory = true;
                openDlg.Filter = "Python File |*.exe";
                if (true == openDlg.ShowDialog())
                {
                    this.txtPath.Text = openDlg.FileName;
                    FilePath = openDlg.FileName;
                }
            }
            catch (IOException ex)
            {
                ExceptionMessage.Show("Could not open file\n" + ex.ToString());
            }

        }

        private void cmbGraph_Selected(object sender, RoutedEventArgs e)
        {
            this._setControls();
        }

        private void _setControls()
        {
            this.txtProbability.IsEnabled = true;
            this.txtEdges.IsEnabled = true;
            if (this.cmbGraph.SelectedIndex == 0)
            {
                this.txtProbability.IsEnabled = false;
            }
            else if (this.cmbGraph.SelectedIndex == 1 || this.cmbGraph.SelectedIndex == 2)
            {
                this.txtEdges.IsEnabled = false;
            }
        }

    }
}
