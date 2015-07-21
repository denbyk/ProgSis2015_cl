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
            d = new DirMonitor("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //d.scanDir();
            RecordFile a = new RecordFile("ciao", 1,1, DateTime.Today);
            RecordFile b = new RecordFile("ciao", 1,1, DateTime.Today);
            MyLogger.add(a == b);
            MyLogger.add(a.Equals(b));
        }
    }
}
