﻿using Ookii.Dialogs.Wpf;
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
        //contiene int delle versioni da recuperare corrispondeti alle RecoverViewModeList (non comprende la posizione 0 "file da tutte le versioni")
        List<int> RecoverViewModeListInt; 

        List<recoverListEntry> RecoverEntryList;
        MainWindow mainW;
        int versionToDisplay; //-1 se display di file da tutte le versioni
        recoverListEntry RListRecoveringEntry;

        //todo? forse recInfo è già thread-safe senza i lock xkè usato da un solo thread?
        private RecoverInfos _recInfos;
        internal RecoverInfos recInfos
        {
            get
            {
                lock (this)
                {
                    return _recInfos;
                }
            }
            set
            {
                lock (this)
                {
                    _recInfos = value;
                }
            }
        }


        public RecoverWindow(MainWindow mainW)
        {
            InitializeComponent();
            this.mainW = mainW;
            RecoverEntryList = new List<recoverListEntry>();
            RecoverEntryList.Add( new recoverListEntry() { Name = "Caricamento in corso...", lastMod = ""});
            recoverListView.ItemsSource = RecoverEntryList;

            recInfos = new RecoverInfos();
            RecoverViewModeList = new List<string> { "file da tutte le versioni" };
            comboRecoverViewMode.ItemsSource = RecoverViewModeList;
            //seleziono prima stringa (file da tutte le versioni)
            comboRecoverViewMode.SelectedIndex = 0;
            versionToDisplay = -1;
            this.buttRecover.IsEnabled = false;
        }

        private void buttRecover_click(object sender, RoutedEventArgs e)
        {
            RecoverFile();
        }

        private void recoverWholeBackup()
        {
            string path;
            //verifica che utente sia sicuro
            MessageBoxResult result = MessageBox.Show("Some files may be overwritten. Do you want to save in another folder?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    //scegliere cartella in cui salvare i backup
                    VistaFolderBrowserDialog folderDiag = new VistaFolderBrowserDialog();
                    folderDiag.ShowDialog();
                    System.Diagnostics.Debug.Assert(versionToDisplay != -1);
                    path = folderDiag.SelectedPath;
                    break;
                case MessageBoxResult.No:
                    path = ""; //cioè usa i path originali
                    break;
                default:
                    //non fare niente
                    return;
                    break;
            }
            mainW.needToRecoverWholeBackup = true;
            mainW.RecoveringQuery = new MainWindow.RecoveringQuery_st(versionToDisplay, path);
            mainW.MakeLogicThreadCycle();
        }

        private void RecoverFile()
        {
            //ottieni elemento selezionato
            RListRecoveringEntry = (recoverListView.SelectedItem as recoverListEntry);
            //affida a thread logico compito di recuperare il file 
            //salvo recoverRecord nella proprietò thread-safe di mainWindow.
            mainW.fileToRecover = RListRecoveringEntry.rr;
            //sblocco il logicThread.
            mainW.needToAskForFileToRecover = true;
            mainW.MakeLogicThreadCycle();
        }

        public void cleanRecovered()
        {
            //TODO!: solo se recover effettuato!
            //lo rimuovo dalla lista di RecoverEntry
            RecoverEntryList.Remove(RListRecoveringEntry);
            //rimuovo anche dall'oggetto RecoverInfos in mainW.
            //todo? lo fa il thread sbagliato? o chissene?
            recInfos.removeRecoverRecord(RListRecoveringEntry.rr);
            //refresh della listView
            recoverListView.Items.Refresh();
        }

        internal void setRecoverInfos(RecoverInfos recInfos)
        {
            this.recInfos = recInfos;
            showRecoverInfos();
            updateRecoverViewModes();
        }

        private void updateRecoverViewModes()
        {
            IEnumerable<int> vnumbs = recInfos.getBackupVersionNumbers().Distinct();
            foreach (var n in vnumbs)
            {
                RecoverViewModeList.Add(n.ToString());
            }
            RecoverViewModeListInt = vnumbs.ToList();
            //comboRecoverViewMode.ItemsSource = RecoverViewModeList;
        }

        private void showRecoverInfos()
        {
            RecoverEntryList.Clear();
            //TODO: implementare le due rappresentazioni: per files e per versioni di backup.
            if (versionToDisplay == -1)
            {
                ShowFilesFromEveryBackup();
            }
            else
            {
                ShowFilesFromSpecificBackup(versionToDisplay);
            }

            recoverListView.Items.Refresh();
            this.buttRecover.IsEnabled = true;
        }


        private void ShowFilesFromSpecificBackup(int versionToDisplay)
        {
            List<RecoverRecord> rrlist = recInfos.getVersionSpecificRecoverList(versionToDisplay);
            foreach (RecoverRecord rec in rrlist)
            {
                RecoverEntryList.Add(new recoverListEntry(rec));
            }
        }

        private void ShowFilesFromEveryBackup()
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
            
        }

        private void comboRecoverViewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = comboRecoverViewMode.SelectedIndex;
            if (i == 0)
            {
                versionToDisplay = -1; //così la prima è -1
                buttRecoverAll.IsEnabled = false;
            }
            else
            {
                versionToDisplay = RecoverViewModeListInt[i-1]; //caso 0 escluso
                buttRecoverAll.IsEnabled = true;
            }
            showRecoverInfos();
        }

        private void buttRecoverAll_click(object sender, RoutedEventArgs e)
        {
            recoverWholeBackup();
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