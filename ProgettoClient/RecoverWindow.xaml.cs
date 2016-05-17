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
        List<string> RecoverViewModeList;
        List<recoverListEntry> RecoverEntryList;
        MainWindow mainW;
        int versionToDisplay; //-1 se display di file da tutte le versioni
        enum RecoverMode
        {
            RecoverSelected,
            RecoverAll
        }
        RecoverMode rmode;

        const string cnstRecoverAllStr = "Recover all";
        const string cnstRecoverFileStr = "Recover file";

        public RecoverWindow(MainWindow mainW)
        {
            InitializeComponent();
            this.mainW = mainW;
            RecoverEntryList = new List<recoverListEntry>();
            RecoverEntryList.Add( new recoverListEntry() { Name = "Caricamento in corso...", lastMod = ""});
            recoverListView.ItemsSource = RecoverEntryList;

            RecoverViewModeList = new List<string> { "file da tutte le versioni" };
            comboRecoverViewMode.ItemsSource = RecoverViewModeList;
            //seleziono prima stringa (file da tutte le versioni)
            comboRecoverViewMode.SelectedIndex = 0;
            versionToDisplay = -1;
            rmode = RecoverMode.RecoverSelected;
            this.buttRecover.IsEnabled = false;
        }

        private void buttRecover_click(object sender, RoutedEventArgs e)
        {
            if(rmode == RecoverMode.RecoverSelected)
            {
                RecoverFile();
            }
            else
            {
                recoverWholeBackup();
            }
        }

        private void recoverWholeBackup()
        {
            //verifica che utente sia sicuro
            MessageBoxResult result = MessageBox.Show("this procedure may overwrite some files. Are you sure you want to proceed?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                //non fa niente
                return;
            }
            //ottieni versione selezionata
            //TODO: gestirlo in comboSelectionChange
            int selectedVersion = -100;
            //affida a thread logico compito di recuperare il file 
            //salvo versionToRecover nella proprietò thread-safe di mainWindow.
            mainW.versionToRecover = selectedVersion;
            //sblocco il logicThread.
            mainW.needToRecoverWholeBackup = true;
            mainW.CycleNowEvent.Set();
        }

        private void RecoverFile()
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
            //TODO: implementare le due rappresentazioni: per files e per versioni di backup.
            if(versionToDisplay == -1)
            {
                ShowFilesFromEveryBackup(recInfos);
                rmode = RecoverMode.RecoverSelected;
                buttRecover.Content = cnstRecoverFileStr;
            }
            else
            {
                ShowFilesFromSpecificBackup(recInfos, versionToDisplay);
                rmode = RecoverMode.RecoverAll;
                buttRecover.Content = cnstRecoverAllStr;
            }

            recoverListView.Items.Refresh();
            this.buttRecover.IsEnabled = true;
        }


        private void ShowFilesFromSpecificBackup(RecoverInfos recInfos, int versionToDisplay)
        {
            List<RecoverRecord> rrlist = recInfos.getVersionSpecificRecoverList(versionToDisplay);
            foreach (RecoverRecord rec in rrlist)
            {
                RecoverEntryList.Add(new recoverListEntry(rec));
            }
        }

        private void ShowFilesFromEveryBackup(RecoverInfos recInfos)
        {

            List<RecoverRecord> rrlist = recInfos.getRecoverUniqueList();
            foreach (RecoverRecord rec in rrlist)
            {
                RecoverEntryList.Add(new recoverListEntry(rec));
            }
        }



        /// <summary>
        /// TODO: DA CANCELLARE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DEBUGBUTT_Click(object sender, RoutedEventArgs e)
        {
            //test vari generali

            //Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            //string s = "C:\\dati\\ciao.txt";
            //string[] sTot = MyConverter.extractNameAndFolder(s);
            //sfd.InitialDirectory = sTot[0];
            //sfd.FileName = sTot[1];
            //bool? result = sfd.ShowDialog();
            //if (result == true)
            //{
            //    FileStream fout = File.Open(sfd.FileName, FileMode.CreateNew);
            //}
            //else
            //{
            //    //gestire annullamento
            //    throw new NotImplementedException();
            //}
        }

        private void comboRecoverViewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
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