using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace NetworkSimulationApp
{
    public static class ExceptionMessage
    {
        public static MessageBoxResult prob;
        
        public static void Show(string error)
        {
            prob = MessageBox.Show(error);
        }
    }
}
