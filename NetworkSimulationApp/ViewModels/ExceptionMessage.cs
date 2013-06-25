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
        public static MessageBoxResult result;
        
        public static void Show(string error)
        {
            result = MessageBox.Show(error);
        }
        public static int ShowConfirmation(string error)
        {
            int code = 0;
            string caption = "Warning";
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;
            result = MessageBox.Show(error, caption, button, icon);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    code = 1;
                    break;
                case MessageBoxResult.No:
                    code = 0;
                    break;
                case MessageBoxResult.Cancel:
                    code = 2;
                    break;
            }
            return code;
        }
    }
}
