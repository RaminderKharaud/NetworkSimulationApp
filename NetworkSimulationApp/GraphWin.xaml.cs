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
    /// File:                   GraphWin.xaml.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       August 2013
    /// 
    /// Revision    1.1         No Revision Yet
    ///                         
    /// Purpose:                Interaction logic for GraphWin.xaml: handles logic for 
    ///                         the create networkx graph window
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
                MainWindowViewModel.OutputFilePath = AppDomain.CurrentDomain.BaseDirectory;
            txtFilePath.Text = MainWindowViewModel.OutputFilePath;
        }
        /// <summary>
        /// When user user click on the create graph button, this method creates 
        /// a networkx graph by starting process and write that file to the 
        /// output path location from where the DrawGraph button will read file and
        /// draw the graph on canvas. There any parameter is missing, user will get
        /// error message. If python dont have writing permission to the output path, this method
        /// will not know that.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCreateGraph_Click(object sender, RoutedEventArgs e)
        {
            string GraphCommand = null;
            int n = 0, m = 0, seed = -1;
            float probability = 0;
            string location = null;
            try
            {
                location = txtFilePath.Text.Trim() + "GraphOutput.txt";
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
        /// <summary>
        /// opens up file dialog box to select python.exe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// this method enable or disaple text fields based on which graph type is
        /// selected
        /// </summary>
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
