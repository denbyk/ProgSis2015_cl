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

        internal delegate void ShowRecoverInfos_dt(RecoverInfos recInfo);
        internal ShowRecoverInfos_dt DelShowRecoverInfos;

        internal delegate void DelSetInterfaceLoggedMode_dt(interfaceMode_t im);
        internal DelSetInterfaceLoggedMode_dt DelSetInterfaceLoggedMode;

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

        //TODO: not hardcode
        private const string HARDCODED_SERVER_IP = "127.0.0.1";

        DirMonitor d;
        DispatcherTimer SyncTimer;
        DispatcherTimer AbortTimer;
        Thread logicThread;

        //event handles
        public EventWaitHandle CycleNowEvent;

        private bool _CycleNowEventSignaled;
        private bool CycleNowEventSignaled
        {
            get
            {
                lock (this)
                {
                    return _CycleNowEventSignaled;
                }
            }
            set
            {
                lock (this)
                {
                    _CycleNowEventSignaled = value;
                }
            }
        }

        private bool _needToSync;
        private bool needToSync
        {
            get
            {
                lock (this)
                {
                    return _needToSync;
                }
            }
            set
            {
                lock (this)
                {
                    _needToSync = value;
                }
            }
        }

        private bool _needToRecover;
        internal bool needToAskRecoverInfo
        {
            get
            {
                lock (this)
                {
                    return _needToRecover;
                }
            }
            set
            {
                lock (this)
                {
                    _needToRecover = value;
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

        private bool _needToAskForFileToRecover;
        internal bool needToAskForFileToRecover
        {
            get
            {
                lock (this)
                {
                    return _needToAskForFileToRecover;
                }
            }
            set
            {
                lock (this)
                {
                    _needToAskForFileToRecover = value;
                }
            }
        }

        private bool _needToRecoverWholeBackup;
        internal bool needToRecoverWholeBackup
        {
            get
            {
                lock (this)
                {
                    return _needToRecoverWholeBackup;
                }
            }
            set
            {
                lock (this)
                {
                    _needToRecoverWholeBackup = value;
                }
            }
        }

        private RecoverRecord _fileToRecover;
        internal RecoverRecord fileToRecover
        {
            get
            {
                lock (this)
                {
                    return _fileToRecover;
                }
            }
            set
            {
                lock (this)
                {
                    _fileToRecover = value;
                }
            }
        }

        private int _versionToRecover;
        internal int versionToRecover
        {
            get
            {
                lock (this)
                {
                    return _versionToRecover;
                }
            }
            set
            {
                lock (this)
                {
                    _versionToRecover = value;
                }
            }
        }

        internal enum interfaceMode_t
        {
            logged,
            notLogged
        };
        private interfaceMode_t _interfaceMode = interfaceMode_t.notLogged;
        private interfaceMode_t interfaceMode
        {
            get { lock (this) { return _interfaceMode; } }
            set
            {
                //solo il mainThread deve accedere qui.
                if (value == interfaceMode_t.logged)
                {
                    textboxPathSyncDir.IsEnabled = false;
                    textboxUtente.IsEnabled = false;
                    textboxPassword.IsEnabled = false;
                    buttSelSyncSir.IsEnabled = false;

                    buttLogin.Content = "Logout";
                    buttStartStopAutoSync.IsEnabled = true;
                    buttManualStartStopSync.IsEnabled = true;
                    buttRecover.IsEnabled = true;
                    textboxCycleTime.IsEnabled = true;

                    _interfaceMode = value;
                }
                else
                {
                    textboxPathSyncDir.IsEnabled = true;
                    textboxUtente.IsEnabled = true;
                    textboxPassword.IsEnabled = true;
                    buttSelSyncSir.IsEnabled = true;

                    buttLogin.Content = "Login";
                    buttStartStopAutoSync.IsEnabled = false;
                    buttManualStartStopSync.IsEnabled = false;
                    //buttRecover.IsEnabled = false;
                    textboxCycleTime.IsEnabled = false;

                    _interfaceMode = value;
                }
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
                SyncTimer.Stop();
                SyncTimer.Interval = value;
                //SyncTimer.Start();
            }
        }

        private Settings settings;
        private SessionManager sm;
        private RecoverWindow recoverW;

        public MainWindow()
        {
            //init UI
            InitializeComponent();
            interfaceMode = interfaceMode_t.notLogged;

            //init delegates
            DelWriteLog = writeInLog_RichTextBox;
            DelAskNewAccount = askNewAccount;
            DelShowRecoverInfos = showRecoverInfos;
            DelSetInterfaceLoggedMode = SetInterfaceLoggedMode;

            //init accessory classes
            MyLogger.init(this);

            //load last settings from file
            LoadSettings();

            this.CycleNowEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            //this.CheckForAbortEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            SyncTimer = new System.Windows.Threading.DispatcherTimer();
            SyncTimer.Tick += new EventHandler(SyncTimerHandler);
            AbortTimer = new System.Windows.Threading.DispatcherTimer();
            AbortTimer.Tick += new EventHandler(AbortTimerHandler);
            AbortTimer.Interval = checkForAbortTimeSpan;

            ApplySettings();

            //let's start
            MyLogger.print("si comincia\n");

            //imposto thread secondario
            logicThread = new Thread(logicThreadStart);

            //DEBUG: lo fa partire il tasto login in realtà
            //logicThread.Start(); 
        }

        private void SetInterfaceLoggedMode(interfaceMode_t im)
        {
            interfaceMode = im;
        }

        ///i metodi apply* modificano l'interfaccia grafica per adattarla alle settings. 
        ///non sono da usare per recepire le modifiche DA interfaccia.
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
                MyLogger.print("AutoSync started\n");
            }
            else
            {
                buttStartStopAutoSync.Content = AUTOSYNC_OFF_TEXT;
                SyncTimer.Stop();
                MyLogger.print("AutoSync stopped\n");
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
                case MessageBoxResult.No:
                    return false;
            }

            return false;
        }

        private void ManualSync()
        {
            MyLogger.print("Sync in corso...\n");
            bool wasAutoSyncOn = SyncTimer.IsEnabled;

            if (wasAutoSyncOn)
                SyncTimer.Stop();


            needToSync = true;
            MakeLogicThreadCycle(); //permette al logicThread di procedere.

            //TODO: possibile stesso problema di autosync (timer scatta prima che sync finisca?
            if (wasAutoSyncOn)
                SyncTimer.Start();
        }

        /// <summary>
        /// //per chiudere il LogicThread in modo ordinato. TODO: non fa logout, chiude e basta.
        /// </summary>
        private void LogicThreadShutDown()
        {
            TerminateLogicThread = true;
            CheckForAbortSignaled = true;
            MakeLogicThreadCycle();
            //attende chiusura del logicThread se non è già chiuso
            if (logicThread.IsAlive == true) logicThread.Join();
            ////reimposta interfaccia grafica in modalità not logged
            //interfaceMode = interfaceMode_t.notLogged; dovrebbe già farla il logicThread
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
                MyLogger.print("Impossibile trovare impostanzioni precedenti");
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
                MyLogger.print("impossibile salvare file settings. le impostazioni attuali non saranno salvate");
                //MyLogger.add(e.Message);
            }
        }

        /// <summary>
        /// permette al logic thread di procedere
        /// </summary>
        private void MakeLogicThreadCycle()
        {
            if (!logicThread.IsAlive)
            {
                logicThread.Start();
                return;
            }
            CycleNowEventSignaled = true;
            CycleNowEvent.Set(); //permette al logicThread di procedere.
        }

        private void SyncTimerHandler(object sender, EventArgs e)
        {
            MyLogger.print("AutoSync in corso\n");
            needToSync = true;
            MakeLogicThreadCycle(); //permette al logicThread di procedere.

            //TODO?: possibile problema per timer troppo corto -> thread secondario non riesce a stare dietro a tutte le richieste?
            //possib soluzione: far riprendere il timer dopo che thread secondario ha finito il sync
            //ricomincia
            SyncTimer.Start();
        }

        private void AbortTimerHandler(object sender, EventArgs e)
        {
            MyLogger.print("DA CANCELLARE");
            CheckForAbortSignaled = true; //caso di checkForAbort, non di SyncNowEvent. no needToSync
            CycleNowEvent.Set(); //permette al logicThread di procedere.
            AbortTimer.Start();
        }

        /*---event handlers & interface modifier------------*/

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
            MyLogger.print("cycle time = " + settings.getCycleTime());

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

        private void buttRecover_Click(object sender, RoutedEventArgs e)
        {
            needToAskRecoverInfo = true;
            MakeLogicThreadCycle();

            recoverW = new RecoverWindow(this);
            recoverW.Owner = this;
            recoverW.ShowDialog();
        }

        private void showRecoverInfos(RecoverInfos recInfo)
        {
            recoverW.showRecoverInfos(recInfo);
        }

        private void buttLoginLogout_Click(object sender, RoutedEventArgs e)
        {
            if (interfaceMode == interfaceMode_t.notLogged)
            {
                logicThread.Start();
            }
            else
            {
                LogicThreadShutDown();
            }
        }

        private void buttRecoverFolder_Click(object sender, RoutedEventArgs e)
        {
            needToRecoverWholeBackup = true;
            MakeLogicThreadCycle();
        }

        /*-------------------------------------------------------------------------------------------------------------*/
        /*---logic Tread methods---------------------------------------------------------------------------------------*/
        /*-------------------------------------------------------------------------------------------------------------*/

        //siamo nel secondo thread, quello che non gestisce la interfaccia grafica.
        private void logicThreadStart()
        {
            MyLogger.debug("LogicThread starting");
            //TODO:DEBUG, DA TOGLIERE LA PROSSIMA RIGA
            //this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.logged); //DEBUG

            try //catch errori non recuperabili per il thread
            {
                //avvio tmer per verifica abort signals periodicamente
                AbortTimer.Start();

                //inizializzo oggetto per connessione con server
                sm = new SessionManager(HARDCODED_SERVER_IP, HARDCODED_SERVER_PORT, this);
                //bool connected = false;

                //while (!connected)
                //{
                //try //catch errori che richiedono riconnessione
                //{ NON CI SONO ECCEZIONI CHE MERITINO DI RICONNETTERSI SENZA DARE modo a utente di cambiare qualche info di login
                //gestione del login
                sm.login(settings.getUser(), settings.getPassw());
                //connected = true;

                //selezione cartella
                sm.setRootFolder(settings.getRootFolder());

                //attiva modalità logged nella UI
                this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.logged);

                //voglio iniziare con una sync
                needToSync = true;

                //ciclo finchè la connessione è attiva. si esce solo con eccezione o con chiusura thread logico (anch'essa un'eccezione).
                while (true)
                {
                    //verifica se deve sincronizzare
                    if (needToSync)
                    {
                        //creo un DirMonitor che analizza la cartella
                        d = new DirMonitor(settings.getRootFolder());
                        SyncAll();
                        needToSync = false;
                    }
                    //verifica se deve richiedere l'intero ultimo backup
                    if (needToRecoverWholeBackup)
                    {
                        sm.RecoverBackupVersion();
                        needToRecoverWholeBackup = false;
                    }
                    //verifica se deve richiedere dati per ripristino di file vecchi
                    if (needToAskRecoverInfo)
                    {
                        recInfos = sm.askForRecoverInfo();
                        needToAskRecoverInfo = false;
                        if (recoverW.IsVisible)
                        {
                            recoverW.Dispatcher.Invoke(DelShowRecoverInfos, recInfos);
                        }
                    }
                    //recupera recoverRecord
                    if (needToAskForFileToRecover)
                    {
                        needToAskForFileToRecover = false;

                        //recupera file
                        sm.askForSingleFile(fileToRecover);
                    }
                    WaitForSyncTime();
                }
                //} NON CI SONO ECCEZIONI CHE MERITINO DI RICONNETTERSI SENZA DARE modo a utente di cambiare qualche info di login

                //} //fine while(!connected)
            } //fine try esterno
            catch (SocketException)
            {
                MyLogger.print("impossibile connettersi. Server non ragiungibile");
                //connected = false; //ripete login e selezione cartella dopo attesa
                //consento a utente di modificare dati di accesso
                //TODO: DEBUG: riattivare riga dopo!!
                this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.notLogged);
                //disattivo il timer che sblocca periodicamente il logicThread affinchè controlli se deve abortire
                AbortTimer.Stop();
                return; //il thread logico si chiude.
            }
            catch (LoginFailedException)
            {
                MyLogger.print("errore nel login. Correggere dati di accesso o creare nuovo utente.");
                //connected = false;
                //consento a utente di modificare dati di accesso
                this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.notLogged);
                //disattivo il timer che sblocca periodicamente il logicThread affinchè controlli se deve abortire
                AbortTimer.Stop();
                return; //il thread logico si chiude.
            }
            catch (RootSetErrorException)
            {
                MyLogger.print("errore nella selezione della cartella. Correggere il path");
                sm.logout();
                //connected = false;
                //consento a utente di modificare dati di accesso
                this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.notLogged);
                //disattivo il timer che sblocca periodicamente il logicThread affinchè controlli se deve abortire
                AbortTimer.Stop();
                return; //il thread logico si chiude.
            }
            catch (AbortLogicThreadException)
            {
                ///TODO ??? 
                ///il logic thread si sta chiudendo (magari perchè utente ha chiuso il programma,
                ///eventualmente chiudere connessioni varie.
                sm.logout(); //è sufficiente?
                //consento a utente di modificare dati di accesso
                this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.notLogged);
                //disattivo il timer che sblocca periodicamente il logicThread affinchè controlli se deve abortire
                AbortTimer.Stop();
                MyLogger.debug("LogicThread closing");
                return; //fine thread logico
            }
            catch (Exception e) //eccezione critica.
            {
                MyLogger.line();
                MyLogger.print(e.Message);
                MyLogger.line();
                //disattivo il timer che sblocca periodicamente il logicThread affinchè controlli se deve abortire
                AbortTimer.Stop();
                MyLogger.debug("LogicThread closing");
                throw;
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
                CycleNowEvent.WaitOne();
                if (CheckForAbortSignaled)
                {
                    CheckForAbortSignaled = false;
                    if (TerminateLogicThread)
                        throw new AbortLogicThreadException();
                }
            } while (!CycleNowEventSignaled); ////evita spurie di syncNow
            CycleNowEventSignaled = false;
        }

        /// <summary>
        /// estraggo i vari record dei file e li sincronizzo con il server
        /// </summary>
        private void SyncAll()
        {
            //TODO:? implementare un meccanismo di abort tra un file e l'altro almeno.
            //TODO: gestire caduta di connessione durante upload di un file, non deve credere di averlo sincronizzato correttamente!!!!
            //^ FATTO MA DA TESTARE
            HashSet<RecordFile> buffer;
            buffer = d.getUpdatedFiles();
            foreach (var f in buffer)
            {
                sm.syncUpdatedFile(f);
                d.confirmSync(f);
            }
            buffer = d.getNewFiles();
            foreach (var f in buffer)
            {
                sm.syncNewFiles(f);
                d.confirmSync(f);
            }
            buffer = d.getDeletedFiles();
            foreach (var f in buffer)
            {
                sm.syncDeletedFile(f);
                d.confirmSync(f);
            }
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
