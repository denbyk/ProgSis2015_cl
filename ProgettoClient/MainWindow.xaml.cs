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
        private const string DEFAULT_USER = "default";
        private const string DEFAULT_PASSW = "default";

        //todo: not hardcode
        private const string HARDCODED_SERVER_IP = "127.0.0.1";

        DirMonitor d;
        DispatcherTimer timerTest;
        Thread logicThread;

        EventWaitHandle SyncNowEvent;
        bool SyncNowEventSignaled;


        bool TerminateLogicThread = false;

        private Settings settings;
        private SessionManager sm;

        private void applyScanInterval(double CycleTime)
        {
            if (CycleTime <= 0)
                CycleTime = 1;
            int intpart = (int)(Math.Floor(CycleTime));
            ScanInterval = new TimeSpan(0, intpart, (int)(Math.Floor((CycleTime - intpart) * 60)));
            this.textboxCycleTime.Text = CycleTime.ToString();
        }

        private void applyAutoSync(bool autoSync)
        {
            if (autoSync)
            {
                buttStartStopAutoSync.Content = AUTOSYNC_ON_TEXT;
                timerTest.Start();
                MyLogger.add("AutoSync started\n");
            }
            else
            {
                buttStartStopAutoSync.Content = AUTOSYNC_OFF_TEXT;
                timerTest.Stop();
                MyLogger.add("AutoSync stopped\n");
            }
        }


        private void applyUser(string User)
        {
            this.textboxUtente.Text = User;
            sm.logout(); //TODO: attenzione, questo deve essere sincronizzato con il thread logico! ancora meglio se glielo faccio fare a lui.
        }


        private void applyPassw(string Password)
        {
            textboxPassword.Text = Password;
            sm.logout(); //TODO: attenzione, questo deve essere sincronizzato con il thread logico! ancora meglio se glielo faccio fare a lui.
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
            this.textboxPathSyncDir.Text = settings.getRootFolder();
            this.applyScanInterval(settings.getCycleTime());
            this.applyAutoSync(settings.getAutoSyncToggle());
            this.applyUser(settings.getUser());
            this.applyPassw(settings.getPassw());
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
                settings = new Settings(DEFAULT_FOLDERROOT_PATH, DEFAULT_CYCLE_TIME, DEFAULT_AUTOSYNCTOGGLE, DEFAULT_USER, DEFAULT_PASSW);
            }
        }


        private void SaveSettings()
        {
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

        private void LogicThreadShutDown()
        {
            //per chiudere il LogicThread in modo ordinato
            lock (this)
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
                //inizializzo oggetto per connessione con server
                sm = new SessionManager(HARDCODED_SERVER_IP);

                //gestione del login
                sm.login(settings.getUser(), settings.getPassw());

                //selezione cartella
                sm.setRootFolder(settings.getRootFolder());

                while (true)
                {
                    //creo un DirMonitor che analizza la cartella
                    d = new DirMonitor(settings.getRootFolder());
                    HashSet<RecordFile> buffer;

                    //estraggo i vari record dei file e li sincronizzo con il server
                    buffer = d.getUpdatedFiles();
                    foreach (var f in buffer)
                    {
                        sm.syncUpdatedFile(f);
                    }
                    buffer = d.getNewFiles();
                    foreach (var f in buffer)
                    {
                        sm.syncNewFiles(f);
                    }
                    buffer = d.getDeletedFiles();
                    foreach (var f in buffer)
                    {
                        sm.syncDeletedFile(f);
                    }

                    //aspetto evento timer o sincronizzazione manuale.
                    while (!SyncNowEventSignaled) //evita spurie
                        SyncNowEvent.WaitOne();
                    SyncNowEventSignaled = false;

                    lock (this)
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
            sm.logout(); //è sufficiente?
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(buttSelSyncSir))
            {
                VistaFolderBrowserDialog folderDiag = new VistaFolderBrowserDialog();
                folderDiag.ShowDialog();
                settings.setRootFolder(folderDiag.SelectedPath);
            }
        }

        private void buttStartStopSync_Click(object sender, RoutedEventArgs e)
        {
            settings.setAutoSyncToggle(!settings.getAutoSyncToggle());
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
                settings.setCycleTime(DEFAULT_CYCLE_TIME);
            }
            else
            {
                settings.setCycleTime(num);
            }
            MyLogger.add("cycle time = " + settings.getCycleTime());

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

//TODO:
/* 
 * quando utente cambia qualcosa in user/psw/cartella il thread 
 * principale deve aggiornare i dati (cosa che fa) e dirlo al thread secondario!
 * attenzione a eventuali zone critiche!!!
 * 
*/