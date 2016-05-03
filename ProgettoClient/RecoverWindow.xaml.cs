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

namespace ProgettoClient
{
    /// <summary>
    /// Logica di interazione per RecoverWindow.xaml
    /// </summary>
    public partial class RecoverWindow : Window
    {
        public RecoverWindow()
        {
            InitializeComponent();
            List<string> lista = new List<string>();
            lista.Add("ciao");
            lista.Add("aaaaa");
            lista.Add("zzzz");
            recoverListView.ItemsSource = lista;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

        }


    }

}

/// vedere qui
///http://www.wpf-tutorial.com/listview-control/listview-with-gridview/