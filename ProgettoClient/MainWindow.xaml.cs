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
using System.Threading;
using System.Windows.Threading;
using Ookii.Dialogs.Wpf;


namespace ProgettoClient
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void AddLog_dt(string log);
        public AddLog_dt DelWriteLog;

        private const string SETTINGS_FILE_PATH = "Settings.bin";
        //TODO change this
        private const string DEFAULT_FOLDERROOT_PATH = "C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test";
        private const double DEFAULT_CYCLE_TIME = 1;
        private const bool DEFAULT_AUTOSYNCTOGGLE = false;
        private const string AUTOSYNC_OFF_TEXT = "Start";
        private const string AUTOSYNC_ON_TEXT = "Stop";

        DirMonitor d;
        DispatcherTimer timerTest;
        Thread logicThread;

        EventWaitHandle SyncNowEvent;
        bool SyncNowEventSignaled;


        bool TerminateLogicThread = false;

        private Settings settings;
        

        private string _rootFolder;
        private string RootFolder
        {
            get { return _rootFolder; } //_rootFolder; }
            set
            {
                this.textboxPathSyncDir.Text = value;
                _rootFolder = value;//_rootFolder = value;
            }
        }

        private double _cycleTime;
        private double CycleTime
        {
            get { return _cycleTime; }
            set
            {
                if (value <= 0)
                    value = 1;
                int intpart = (int)(Math.Floor(value));
                ScanInterval = new TimeSpan(0, intpart, (int)(Math.Floor((value-intpart)*60)));
                this.textboxCycleTime.Text = value.ToString();
                _cycleTime = value; 
            }
        }

        private bool _autoSyncToggle;
        private bool AutoSync
        {
            get { return _autoSyncToggle; }
            set
            {
                if (value) {
                    buttStartStopAutoSync.Content = AUTOSYNC_ON_TEXT;
                    timerTest.Start();
                    MyLogger.add("AutoSync started\n");
                }
                else { 
                    buttStartStopAutoSync.Content = AUTOSYNC_OFF_TEXT;
                    timerTest.Stop();
                    MyLogger.add("AutoSync stopped\n");
                }

                _autoSyncToggle = value;
            }
        }


        

        /// <summary>
        ///modificandone il valore il timer si blocca e va fatto partire.
        /// </summary>
        public TimeSpan ScanInterval
        {
            set
            {
                if (value.Equals(TimeSpan.Zero))
                    value.Add(new TimeSpan(0, 1, 0));
                timerTest.Stop();
                timerTest.Interval = value;
                //timerTest.Start();
            }
        }


        public MainWindow()
        {
            //init UI
            InitializeComponent();
            
            //init delegates
            DelWriteLog = writeInLog_RichTextBox;

            //init accessory classes
            MyLogger.init(this);

            //load last settings from file
            LoadSettings();

            
            this.SyncNowEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            timerTest = new System.Windows.Threading.DispatcherTimer();
            timerTest.Tick += new EventHandler(TimerHandler);
            //ScanInterval = new TimeSpan(0, 0, 3);

            ApplySettings();

            //let's start
            MyLogger.add("si comincia\n");

            //sgancio thread secondario
            logicThread = new Thread(logicThreadStart);
            logicThread.Start();
        }


        private void ManualSync()
        {
            MyLogger.add("Sync in corso...\n");
            timerTest.Stop();
            SyncNowEventSignaled = true;
            SyncNowEvent.Set(); //permette al logicThread di procedere.
            //TODO: possibile stesso problema di autosync (timer scatta prima che sync finisca?
            timerTest.Start();
        }


        private void ApplySettings()
        {
            RootFolder = settings.RootFolder;
            CycleTime = settings.CycleTime;
            AutoSync = settings.AutoSyncToggle;
        }

        private void LoadSettings()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream fin;
            try
            {
                fin = new FileStream(SETTINGS_FILE_PATH, FileMode.Open);
                settings = (Settings)formatter.Deserialize(fin);
                fin.Close();
            }
            catch (Exception e)
            {
                //se non esiste o non riesco a caricare settings:
                MyLogger.add("Impossibile trovare impostanzioni precedenti");
                settings = new Settings(DEFAULT_FOLDERROOT_PATH, DEFAULT_CYCLE_TIME, DEFAULT_AUTOSYNCTOGGLE );
            }
        }


        private void SaveSettings(){
            settings.RootFolder = RootFolder;
            settings.CycleTime = CycleTime;
            settings.AutoSyncToggle = AutoSync;

            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                FileStream fout = new FileStream(SETTINGS_FILE_PATH, FileMode.Create);
                formatter.Serialize(fout, settings);
                fout.Close();
            }
            catch (Exception e)
            {
                MyLogger.add("impossibile salvare file settings. le impostazioni attuali non saranno salvate");
                //MyLogger.add(e.Message);
            }
        }

        private void LogicThreadShutDown(){
            //per chiudere il LogicThread in modo ordinato
            lock(this)
            {
                 TerminateLogicThread = true;
                 SyncNowEventSignaled = true;
                 SyncNowEvent.Set(); //permette al logicThread di procedere.
            }
            logicThread.Join();
        }


        private void TimerHandler(object sender, EventArgs e)
        {
            MyLogger.add("AutoSync in corso\n");
            SyncNowEventSignaled = true;
            SyncNowEvent.Set(); //permette al logicThread di procedere.
            
            //TODO: possibile problema per timer troppo corto -> thread secondario non riesce a stare dietro a tutte le richieste?
            //possib soluzione: far riprendere il timer dopo che thread secondario ha finito il sync
            //ricomincia
            timerTest.Start();
        }



        private void logicThreadStart() 
        {
            //siamo nel secondo thread, quello che non gestisce la interfaccia grafica.
            try
            {
                //inserire chiamata a gestione del login qui.
                //TODO
                while(true){
                    //creo un DirMonitor
                    d = new DirMonitor(RootFolder);

                    //aspetto evento timer o sincronizzazione manuale.
                    while(!SyncNowEventSignaled) //evita spurie
                        SyncNowEvent.WaitOne();
                    SyncNowEventSignaled = false;

                    lock(this)
                    {
                        //verifico se devo terminare il thread
                        if (TerminateLogicThread)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.line();
                MyLogger.line();
                MyLogger.line();
                MyLogger.add(e.Message);
                MyLogger.line();
                MyLogger.line();
                MyLogger.line();
                throw;
            }
            MyLogger.add("logicThreadStart sta per uscire");
            //TODO ??? 
            //il logic thread si sta chiudendo (magari perchè utente ha chiuso il programma, eventualmente chiudere connessioni varie.
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(buttSelSyncSir))
            {
                VistaFolderBrowserDialog folderDiag = new VistaFolderBrowserDialog();
                folderDiag.ShowDialog();
                RootFolder = folderDiag.SelectedPath;
            }
        }

        private void buttStartStopSync_Click(object sender, RoutedEventArgs e)
        {
            AutoSync = !AutoSync;
            //TODO: aggiungere attivazione timer
        }

        private void writeInLog_RichTextBox(String message)
        {
            Log_RichTextBox.AppendText(message);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            LogicThreadShutDown();
            SaveSettings();
        }

        private void textboxCycleTime_LostFocus(object sender, RoutedEventArgs e)
        {
            double num;
            if (!Double.TryParse(textboxCycleTime.Text, out num))
            {
                //errore
                CycleTime = DEFAULT_CYCLE_TIME;
            }
            else {
                CycleTime = num;
            }
            MyLogger.add("cycle time = " + CycleTime);

        }

        private void textboxCycleTime_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                textboxCycleTime.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void buttStartStopSync_Copy_Click(object sender, RoutedEventArgs e)
        {
            ManualSync();
        }

        private void Log_RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Log_RichTextBox.ScrollToEnd();
        }


    }
}

