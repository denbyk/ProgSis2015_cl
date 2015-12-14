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
using System.Net.Sockets;


namespace ProgettoClient
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void AddLog_dt(string log);
        public AddLog_dt DelWriteLog;

        public delegate bool AskNewAccount_dt();
        public AskNewAccount_dt DelAskNewAccount;

        private const string SETTINGS_FILE_PATH = "Settings.bin";
        //TODO change this
        private const string DEFAULT_FOLDERROOT_PATH = "C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test";
        private const double DEFAULT_CYCLE_TIME = 1;
        private const bool DEFAULT_AUTOSYNCTOGGLE = false;
        private const string AUTOSYNC_OFF_TEXT = "Start";
        private const string AUTOSYNC_ON_TEXT = "Stop";
        private const string DEFAULT_USER = "default";
        private const string DEFAULT_PASSW = "default";
        private const int HARDCODED_SERVER_PORT = 8888;

        private TimeSpan checkForAbortTimeSpan = new TimeSpan(0, 0, 3);

        //todo: not hardcode
        private const string HARDCODED_SERVER_IP = "127.0.0.1";

        DirMonitor d;
        DispatcherTimer SyncTimer;
        DispatcherTimer AbortTimer;
        Thread logicThread;

        //event handles
        EventWaitHandle SyncNowEvent;

        private bool _syncNowEventSignaled;
        private bool SyncNowEventSignaled
        {
            get
            {
                lock (this)
                {
                    return _syncNowEventSignaled;
                }
            }
            set
            {
                lock (this)
                {
                    _syncNowEventSignaled = value;
                }
            }
        }

        private bool _checkForAbortSignaled;
        private bool CheckForAbortSignaled
        {
            get
            {
                lock (this)
                {
                    return _checkForAbortSignaled;
                }
            }
            set
            {
                lock (this)
                {
                    _checkForAbortSignaled = value;
                }
            }
        }

        private bool _terminateLogicThread = false;
        private bool TerminateLogicThread
        {
            get
            {
                lock (this)
                {
                    return _terminateLogicThread;
                }
            }
            set
            {
                lock (this)
                {
                    _terminateLogicThread = value;
                }
            }
        }

        private Settings settings;
        private SessionManager sm;

        //i metodi apply* modificano l'interfaccia grafica per adattarla alle settings. non sono da usare per recepire le modifiche DA interfaccia.
        private void applyScanInterval(double CycleTime)
        {
            if (CycleTime <= 0)
                CycleTime = 1;
            int intpart = (int)(Math.Floor(CycleTime));
            ScanInterval = new TimeSpan(0, intpart, (int)(Math.Floor((CycleTime - intpart) * 60)));
            this.textboxCycleTime.Text = CycleTime.ToString();
        }

        private void setAutoSync(bool autoSync)
        {
            if (autoSync)
            {
                buttStartStopAutoSync.Content = AUTOSYNC_ON_TEXT;
                SyncTimer.Start();
                MyLogger.add("AutoSync started\n");
            }
            else
            {
                buttStartStopAutoSync.Content = AUTOSYNC_OFF_TEXT;
                SyncTimer.Stop();
                MyLogger.add("AutoSync stopped\n");
            }
        }


        private void applyUser(string User)
        {
            this.textboxUtente.Text = User;
        }


        private void applyPassw(string Password)
        {
            textboxPassword.Text = Password;
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
                SyncTimer.Stop();
                SyncTimer.Interval = value;
                //SyncTimer.Start();
            }
        }


        public MainWindow()
        {
            //init UI
            InitializeComponent();

            //init delegates
            DelWriteLog = writeInLog_RichTextBox;
            DelAskNewAccount = askNewAccount;

            //init accessory classes
            MyLogger.init(this);

            //load last settings from file
            LoadSettings();


            this.SyncNowEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            //this.CheckForAbortEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            SyncTimer = new System.Windows.Threading.DispatcherTimer();
            SyncTimer.Tick += new EventHandler(SyncTimerHandler);
            AbortTimer = new System.Windows.Threading.DispatcherTimer();
            AbortTimer.Tick += new EventHandler(AbortTimerHandler);
            AbortTimer.Interval = checkForAbortTimeSpan;

            ApplySettings();

            //let's start
            MyLogger.add("si comincia\n");

            //sgancio thread secondario
            logicThread = new Thread(logicThreadStart);
            logicThread.Start();
        }



        private bool askNewAccount()
        {
            // Configure the message box to be displayed
            string messageBoxText = "User inesistente. Si desidera crearlo?";
            string caption = "Word Processor";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;

            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    return true;
                    break;
                case MessageBoxResult.No:
                    return false;
                    break;
            }

            return false;
        }


        private void ManualSync()
        {
            MyLogger.add("Sync in corso...\n");
            SyncTimer.Stop();

            SyncNowEventSignaled = true;

            SyncNowEvent.Set(); //permette al logicThread di procedere.
            //TODO: possibile stesso problema di autosync (timer scatta prima che sync finisca?
            SyncTimer.Start();
        }


        private void ApplySettings()
        {
            this.textboxPathSyncDir.Text = settings.getRootFolder();
            this.applyScanInterval(settings.getCycleTime());
            this.setAutoSync(settings.getAutoSyncToggle());
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

            TerminateLogicThread = true;
            CheckForAbortSignaled = true;
            SyncNowEvent.Set(); //permette al logicThread di procedere.

            logicThread.Join();
        }


        private void SyncTimerHandler(object sender, EventArgs e)
        {
            MyLogger.add("AutoSync in corso\n");
            SyncNowEventSignaled = true;

            SyncNowEvent.Set(); //permette al logicThread di procedere.

            //TODO: possibile problema per timer troppo corto -> thread secondario non riesce a stare dietro a tutte le richieste?
            //possib soluzione: far riprendere il timer dopo che thread secondario ha finito il sync
            //ricomincia
            SyncTimer.Start();
        }

        private void AbortTimerHandler(object sender, EventArgs e)
        {
            MyLogger.add("DA CANCELLARE");
            CheckForAbortSignaled = true; //caso di checkForAbort, non di SyncNowEvent
            SyncNowEvent.Set(); //permette al logicThread di procedere.
            AbortTimer.Start();
        }

        private void logicThreadStart()
        {
            //siamo nel secondo thread, quello che non gestisce la interfaccia grafica.
            try
            {
                //avvio tmer per verifica abort signals
                AbortTimer.Start();

                //inizializzo oggetto per connessione con server
                sm = new SessionManager(HARDCODED_SERVER_IP, HARDCODED_SERVER_PORT, this);
                bool connected = false;

                while (!connected)
                {
                    try
                    {
                        //gestione del login
                        sm.login(settings.getUser(), settings.getPassw());
                        connected = true;

                        //selezione cartella
                        sm.setRootFolder(settings.getRootFolder());

                        //ciclo finchè la connessione è attiva. si esce solo con eccezione o con chiusura thread logico.
                        while (true)
                        {
                            //creo un DirMonitor che analizza la cartella
                            d = new DirMonitor(settings.getRootFolder());
                            SyncAll();
                            WaitForSyncTime();
                        }
                    }
                    catch (SocketException)
                    {
                        MyLogger.add("impossibile connettersi. Nuovo tentativo alla prossima sincronizzazione.");
                        connected = false; //ripete login e selezione cartella dopo attesa
                        WaitForSyncTime();
                    }
                    catch (LoginFailedException)
                    {
                        MyLogger.add("errore nel login. Correggere dati di accesso o creare nuovo utente.");
                        connected = false;
                        WaitForSyncTime();
                    }

                } //fine while(!connected)
            }
            catch (AbortLogicThreadException)
            {
                MyLogger.add("logicThreadStart sta per uscire");
                ///TODO ??? 
                ///il logic thread si sta chiudendo (magari perchè utente ha chiuso il programma,
                ///eventualmente chiudere connessioni varie.
                sm.logout(); //è sufficiente?
                return; //fine thread logico
            }
            catch (Exception e)
            {
                MyLogger.line();
                MyLogger.add(e.Message);
                MyLogger.line();
                throw;
            }

        }


        /// <summary>
        /// estraggo i vari record dei file e li sincronizzo con il server
        /// </summary>
        private void SyncAll()
        {
            //TODO: implementare un meccanismo di abort tra un file e l'altro almeno.
            //TODO: gestire caduta di connessione durante upload di un file, non deve credere di averlo sincronizzato correttamente!!!!
            HashSet<RecordFile> buffer;
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
        }


        /// <summary>
        /// aspetto evento timer AutoSync o sincronizzazione manuale.
        /// lancia eccezione se è stato settato generato un abort (check interno ogni checkForAbortTimeSpan).
        /// </summary>
        /// <exception cref="AbortLogicThreadException"></exception>
        private void WaitForSyncTime()
        {
            do
            {
                SyncNowEvent.WaitOne();
                if (CheckForAbortSignaled)
                {
                    CheckForAbortSignaled = false;
                    if (TerminateLogicThread)
                        throw new AbortLogicThreadException();
                }
            } while (!SyncNowEventSignaled); ////evita spurie di syncNow
            SyncNowEventSignaled = false;
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
            //attivazione timer
            setAutoSync(settings.getAutoSyncToggle());
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
            string text = textboxCycleTime.Text;
            text = text.Replace(".", ",");
            if (!Double.TryParse(text, out num))
            {
                //errore
                settings.setCycleTime(DEFAULT_CYCLE_TIME);
            }
            else
            {
                settings.setCycleTime(num);
            }
            applyScanInterval(settings.getCycleTime());
            MyLogger.add("cycle time = " + settings.getCycleTime());

        }

        private void textboxCycleTime_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                textboxCycleTime.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void buttManualStartStopSync_Click(object sender, RoutedEventArgs e)
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
 * oppure rendere non modificabile questi campi...
 * 
*/