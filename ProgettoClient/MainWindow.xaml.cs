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
using System.Collections.Generic;

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
            //RecordFile a = new RecordFile("ciao", 1,1, DateTime.Today);
            //RecordFile b = new RecordFile("ciao", 1,1, DateTime.Today);
            //MyLogger.add(a == b);
            //MyLogger.add(a.Equals(b));

            RecordFile t = new RecordFile("ciao", 1, 2, DateTime.Today);
            MyLogger.add(t);

           

            //formatter.Serialize(s, t);
            //s.Close();
            //t = null;
            //s = null;
            //MyLogger.add("oggetto salvato");
            //s = new FileStream("test_serializzazione.bin", FileMode.Open);
            //t = (RecordFile) formatter.Deserialize(s);
            //MyLogger.add(t);

            var p = new RecordFile("test", 0, 0, DateTime.Now);
            var set = new HashSet<RecordFile>();
            set.Add(t);
            set.Add(p);
            MyLogger.add(set);

            //var hashset = (HashSet<string>)info.GetValue("hashset", typeof(HashSet<string>));
            //hashset.OnDeserialization(this);


            
            set = null;

            s.Close();

            s = new FileStream("test_serializzazione.bin", FileMode.Open);
            set = (HashSet<RecordFile>)formatter.Deserialize(s);
            MyLogger.add(s);
        }
    }
}
