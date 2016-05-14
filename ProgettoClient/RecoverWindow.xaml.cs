using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        List<recoverListEntry> RecoverEntryList;
        MainWindow mainW;

        public RecoverWindow(MainWindow mainW)
        {
            InitializeComponent();
            this.mainW = mainW;
            RecoverEntryList = new List<recoverListEntry>();
            RecoverEntryList.Add( new recoverListEntry() { Name = "Caricamento in corso...", lastMod = ""});
            recoverListView.ItemsSource = RecoverEntryList;
            this.buttRecover.IsEnabled = false;
        }

        private void buttRecover_click(object sender, RoutedEventArgs e)
        {
            //ottieni elemento selezionato
            recoverListEntry rles = (recoverListView.SelectedItem as recoverListEntry);
            //affida a thread logico compito di recuperare il file 
            //salvo recoverRecord nella proprietò thread-safe di mainWindow.
            mainW.fileToRecover = rles.rr;
            //sblocco il logicThread.
            mainW.needToAskForFileToRecover = true;
            mainW.CycleNowEvent.Set();
            //lo rimuovo dalla lista di RecoverEntry
            RecoverEntryList.Remove(rles);
            //rimuovo anche dall'oggetto RecoverInfos in mainW.
            mainW.recInfos.removeRecoverRecord(rles.rr);
            //refresh della listView
            recoverListView.Items.Refresh();
        }

        internal void showRecoverInfos(RecoverInfos recInfos)
        {
            RecoverEntryList.Clear();

            List<RecoverRecord> rrlist = recInfos.getRecoverList();
            foreach (RecoverRecord rec in rrlist)
            {
                RecoverEntryList.Add(new recoverListEntry(rec));
            }

            recoverListView.Items.Refresh();
            this.buttRecover.IsEnabled = true;
        }



        /// <summary>
        /// TODO: DA CANCELLARE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DEBUGBUTT_Click(object sender, RoutedEventArgs e)
        {
            //test vari generali
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            string s = "C:\\dati\\ciao.txt";
            string[] sTot = MyConverter.extractNameAndFolder(s);
            sfd.InitialDirectory = sTot[0];
            sfd.FileName = sTot[1];
            bool? result = sfd.ShowDialog();
            if (result == true)
            {
                FileStream fout = File.Open(sfd.FileName, FileMode.CreateNew);
            }
            else
            {
                //gestire annullamento
                throw new NotImplementedException();
            }
        }
    }

    public class recoverListEntry
    {
        public string Name { get; set; }
        public string lastMod{ get; set; }

        //recoverListEntry contiene recoverRecord che contiene RecordFile.
        public RecoverRecord rr;

        //per creare la entry fittizia inizialmente
        public recoverListEntry() { }

        public recoverListEntry(RecoverRecord rRec)
        {
            this.rr = rRec;
            this.Name = rRec.rf.nameAndPath;
            this.lastMod = rRec.rf.lastModified.ToString();
        }

    }


}

/// vedere qui
///http://www.wpf-tutorial.com/listview-control/listview-with-gridview/