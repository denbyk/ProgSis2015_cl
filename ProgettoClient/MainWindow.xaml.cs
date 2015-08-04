using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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

namespace ProgettoClient
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DirMonitor d;

        public MainWindow()
        {
            InitializeComponent();
            MyLogger.add("si comincia");
            //questo andrebbe fatta da un thread diverso.
            d = new DirMonitor("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test", new TimeSpan(0,0,15));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(buttSelSyncSir))
            {
                ///folderbrowser
            }
        }
    }
}


/*
 * da debuggare:
 *      rileva file updated come old.
*/