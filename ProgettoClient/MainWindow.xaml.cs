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

        internal delegate void SetRecoverInfos_dt(RecoverInfos recInfo);
        internal SetRecoverInfos_dt DelSetRecoverInfos;

        internal delegate void DelSetInterfaceLoggedMode_dt(interfaceMode_t im);
        internal DelSetInterfaceLoggedMode_dt DelSetInterfaceLoggedMode;

        //public delegate bool DelYesNoQuestion_dt(string message, string caption, MessageBoxImage icon = MessageBoxImage.Question);
        //public DelYesNoQuestion_dt DelYesNoQuestion;

        public delegate void ShowOkMsg_dt(string s, MessageBoxImage icon);
        public ShowOkMsg_dt DelShowOkMsg;

        private const string SETTINGS_FILE_PATH = "Settings.bin";
        //todo?
        private const bool DEBUG_DONT_LOGIN = true;
        private const string AUTOSYNC_OFF_TEXT = "Start";
        private const string AUTOSYNC_ON_TEXT = "Stop";

        private TimeSpan checkForAbortTimeSpan = new TimeSpan(0, 0, 3);


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

        /// <summary>
        /// se recoveringFolderPath = "", i path da usare sono quelli originali
        /// </summary>
        public struct RecoveringQuery_st
        {
            public readonly int versionToRecover;
            public readonly string recoveringFolderPath;
            public readonly RecoverInfos recInfos;

            public RecoveringQuery_st(RecoverInfos recInfos, int versionToRecover, string recoveringFolderPath)
            {
                this.recInfos = recInfos;
                this.versionToRecover = versionToRecover;
                this.recoveringFolderPath = recoveringFolderPath;
            }
        }
        private RecoveringQuery_st _RecoveringQuery;
        public RecoveringQuery_st RecoveringQuery
        {
            get
            {
                lock (this)
                {
                    return _RecoveringQuery;
                }
            }
            set
            {
                lock (this)
                {
                    _RecoveringQuery = value;
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

        private bool _logged;
        public bool logged
        {
            get
            {
                lock (this)
                {
                    return _logged;
                }
            }
            set
            {
                lock (this)
                {
                    _logged = value;
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
        
        internal enum interfaceMode_t
        {
            logged,
            notLogged,
            busy
        };
        private interfaceMode_t _previousInterfaceMode = interfaceMode_t.notLogged;
        private interfaceMode_t _interfaceMode = interfaceMode_t.notLogged;
        private interfaceMode_t interfaceMode
        {
            get { lock (this) { return _interfaceMode; } }
            set
            {
                //solo il mainThread deve accedere qui.
                switch (value)
                {
                    case interfaceMode_t.logged:
                        textBoxIndIP.IsEnabled = false;
                        textboxPassword.IsEnabled = false;
                        textboxPathSyncDir.IsEnabled = false;
                        textboxUtente.IsEnabled = false;
                        textboxPassword.IsEnabled = false;
                        buttSelSyncSir.IsEnabled = false;

                        buttLogin.Content = "Logout";
                        buttStartStopAutoSync.IsEnabled = true;
                        buttManualStartStopSync.IsEnabled = true;
                        buttRecover.IsEnabled = true;
                        textboxCycleTime.IsEnabled = true;
                        buttLogin.IsEnabled = true;

                        this.setAutoSync(settings.getAutoSyncToggle());

                        if (recoverW != null)
                            recoverW.setBusyInterface(false);

                        _interfaceMode = value;
                        break;
                    case interfaceMode_t.notLogged:
                        textBoxIndIP.IsEnabled = true;
                        textboxPassword.IsEnabled = true;
                        textboxPathSyncDir.IsEnabled = true;
                        textboxUtente.IsEnabled = true;
                        textboxPassword.IsEnabled = true;
                        buttSelSyncSir.IsEnabled = true;
                        buttLogin.IsEnabled = true;
                        buttLogin.Content = "Login";

                        buttStartStopAutoSync.IsEnabled = false;
                        buttManualStartStopSync.IsEnabled = false;
                        buttRecover.IsEnabled = false;
                        textboxCycleTime.IsEnabled = false;

                        this.setAutoSync(false);

                        if (recoverW != null)
                            recoverW.setBusyInterface(false);

                        _interfaceMode = value;
                        break;
                    case interfaceMode_t.busy:
                        if (_interfaceMode == interfaceMode_t.busy)
                            break;
                        _previousInterfaceMode = _interfaceMode;

                        //textBoxIndIP.IsEnabled = false;
                        //textboxPassword.IsEnabled = false;
                        //textboxPathSyncDir.IsEnabled = false;
                        //textboxUtente.IsEnabled = false;
                        //textboxPassword.IsEnabled = false;
                        buttSelSyncSir.IsEnabled = false;

                        buttLogin.IsEnabled = false;
                        buttStartStopAutoSync.IsEnabled = false;
                        buttManualStartStopSync.IsEnabled = false;
                        buttRecover.IsEnabled = false;
                        //textboxCycleTime.IsEnabled = true;

                        //todo:disattivare tasti di recoverW
                        if (recoverW != null)
                            recoverW.setBusyInterface(true);

                        this.setAutoSync(settings.getAutoSyncToggle());

                        _interfaceMode = value;

                        break;
                    default:
                        break;
                }
            }
        }

        public void unBusyInterface()
        {
            _interfaceMode = _previousInterfaceMode;
            recoverW.setBusyInterface(false);
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
            }
        }

        public Settings settings;
        private SessionManager sm;
        public RecoverWindow recoverW;
        private bool firstConnSync;
        

        public MainWindow()
        {
            //init UI
            InitializeComponent();
            

            //init delegates
            DelWriteLog = writeInLog_RichTextBox;
            DelAskNewAccount = askNewAccount;
            DelSetRecoverInfos = setRecoverInfos;
            DelSetInterfaceLoggedMode = SetInterfaceLoggedMode;
            //DelYesNoQuestion = AskYesNoQuestion;
            DelShowOkMsg = ShowOkMsg;

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

            //imposta modalità interfaccia a not logged
            interfaceMode = interfaceMode_t.notLogged;

            //let's start
            MyLogger.debug("si comincia\n");
        }

        private void ShowOkMsg(string msg, MessageBoxImage icon)
        {
            //"Errore, nome utente troppo lungo"
            string messageBoxText = msg;
            string caption = "Info";
            MessageBoxButton button = MessageBoxButton.OK;

            // Display message box
            MessageBox.Show(messageBoxText, caption, button, icon);
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
                MyLogger.debug("AutoSync attivato\n");
            }
            else
            {
                buttStartStopAutoSync.Content = AUTOSYNC_OFF_TEXT;
                SyncTimer.Stop();
                MyLogger.debug("AutoSync disattivato\n");
            }
        }

        private void applyUser(string User)
        {
            this.textboxUtente.Text = User;
        }

        private void applyPassw(string Password)
        {
            textboxPassword.Password = Password;
            //textboxPassword.Text = Password;
        }

        private void applyIP(string indIP)
        {
            textBoxIndIP.Text = indIP;
        }

        private void applyPorta(int porta)
        {
            textBoxPorta.Text = porta.ToString();
        }

        private bool askNewAccount()
        {
            // Configure the message box to be displayed
            string messageBoxText = "User inesistente. Si desidera crearlo?";
            string caption = "Nuovo Account";
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


        //private void cleanRecoveredEntryList()
        //{
        //    recoverW.cleanRecovered();
        //}

        private void ManualSync()
        {
            MyLogger.print("Sincronizzazione in corso...");
            bool wasAutoSyncOn = SyncTimer.IsEnabled;

            if (wasAutoSyncOn)
                SyncTimer.Stop();


            needToSync = true;
            MakeLogicThreadCycle(); //permette al logicThread di procedere.

            if (wasAutoSyncOn)
                SyncTimer.Start();
        }

        /// <summary>
        /// per chiudere il LogicThread in modo ordinato.
        /// </summary>
        private void LogicThreadShutDown()
        {
            //myLogger usa delegati, troppo lenti alla chiusura del programma. uso direttamente append()
            //MyLogger.print("chiusura programma in corso...");
            Log_RichTextBox.AppendText("Chiusura programma in corso...\n");
            if (logicThread == null)
                return;
            TerminateLogicThread = true;
            CheckForAbortSignaled = true;
            MakeLogicThreadCycle();
            //attende chiusura del logicThread se non è già chiuso
            if (logicThread.IsAlive == true) logicThread.Join();
            //reset richieste logicthread
            resetLogicThreadNeeds();
        }

        private void resetLogicThreadNeeds()
        {
            needToAskForFileToRecover = false;
            needToAskRecoverInfo = false;
            needToRecoverWholeBackup = false;
            needToSync = false;
        }

        private void ApplySettings()
        {
            this.textboxPathSyncDir.Text = settings.getRootFolder();
            this.applyScanInterval(settings.getCycleTime());
            this.setAutoSync(settings.getAutoSyncToggle());
            this.applyUser(settings.getUser());
            this.applyPassw(settings.getPassw());
            this.applyIP(settings.getIP());
            this.applyPorta(settings.getPorta());
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
                settings = new Settings();
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
        public void MakeLogicThreadCycle()
        {
            System.Diagnostics.Debug.Assert(logicThread != null);
            if (!logicThread.IsAlive && !TerminateLogicThread)
            {
                try
                {
                    //TODO: ma questa riga che a volte crea problemi, serve mai? quando faccio partire il logicThread tramite MakeLogicThreadStart??
                    logicThread.Start();
                    return;
                }
                catch(System.Threading.ThreadStateException e)
                {
                    MyLogger.debug(e.ToString());
                }
            }
            CycleNowEventSignaled = true;
            CycleNowEvent.Set(); //permette al logicThread di procedere.
        }

        private void SyncTimerHandler(object sender, EventArgs e)
        {
            MyLogger.print("AutoSync in corso\n");
            needToSync = true;
            MakeLogicThreadCycle(); //permette al logicThread di procedere.

            //ricomincia
            SyncTimer.Start();
        }

        private void AbortTimerHandler(object sender, EventArgs e)
        {
            //MyLogger.print("DA CANCELLARE");
            CheckForAbortSignaled = true; //caso di checkForAbort, non di SyncNowEvent. no needToSync
            CycleNowEvent.Set(); //permette al logicThread di procedere.
            AbortTimer.Start();
        }


        /*private bool AskYesNoQuestion(string messageBoxText, string caption, MessageBoxImage icon = MessageBoxImage.Question)
        {
            // Configure the message box to be displayed
            MessageBoxButton button = MessageBoxButton.YesNo;

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
        */


        /*---event handlers & interface modifier------------*/

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(buttSelSyncSir))
            {
                VistaFolderBrowserDialog folderDiag = new VistaFolderBrowserDialog();
                folderDiag.ShowDialog();
                settings.setRootFolder(folderDiag.SelectedPath);
                textboxPathSyncDir.Text = settings.getRootFolder();
            }
        }

        private void buttStartStopSync_Click(object sender, RoutedEventArgs e)
        {
            bool toggleOn = !settings.getAutoSyncToggle();
            settings.setAutoSyncToggle(toggleOn);
            //attivazione timer
            setAutoSync(toggleOn);
            if(toggleOn)
                MyLogger.print("Sincronizzazione automatica attivata\n");
            else
                MyLogger.print("Sincronizzazione automatica disattivata\n");
        }

        private void writeInLog_RichTextBox(String message)
        {
            Log_RichTextBox.AppendText(message);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //TODO:non scrive
            MyLogger.print("Operazione in corso, attendere chiusura programma...");
            LogicThreadShutDown();
            //TODO: attende chiusura thread (?)
            //logicThread.Join();
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
                settings.setCycleTime(0); //numero non valido, verrà restituito il DEFAULT al prossimo get
            }
            else
            {
                settings.setCycleTime(num);
            }
            applyScanInterval(settings.getCycleTime());
            MyLogger.debug("cycle time = " + settings.getCycleTime());

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
            recoverW = new RecoverWindow(this);
            recoverW.Owner = this;

            needToAskRecoverInfo = true;
            MakeLogicThreadCycle();

            recoverW.ShowDialog();
        }

        private void setRecoverInfos(RecoverInfos recInfo)
        {
            recoverW.setRecoverInfos(recInfo);
        }

        private void buttLoginLogout_Click(object sender, RoutedEventArgs e)
        {
            if (interfaceMode == interfaceMode_t.notLogged)
            {
                //if (logicThread != null && logicThread.IsAlive)
                //{
                //    LogicThreadShutDown();
                //    logicThread.Join();
                //}
                //imposto thread secondario
                logicThread = new Thread(logicThreadStart);
                logicThread.Name = "Thread Logico";
                logicThread.Start();
            }
            else
            {
                if (logged)
                    sm.logout();
                SetInterfaceLoggedMode(interfaceMode_t.notLogged);
                LogicThreadShutDown();
            }
        }

        private void buttRecoverFolder_Click(object sender, RoutedEventArgs e)
        {
            needToRecoverWholeBackup = true;
            MakeLogicThreadCycle();
        }

        private void textboxPathSyncDir_LostFocus(object sender, RoutedEventArgs e)
        {
            settings.setRootFolder(textboxPathSyncDir.Text);
        }

        private void textboxUtente_LostFocus(object sender, RoutedEventArgs e)
        {
            settings.setUser(textboxUtente.Text);
        }

        private void textboxPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            //settings.setPassw(textboxPassword.Text);
            settings.setPassw(textboxPassword.Password);
        }

        private void textBoxPorta_LostFocus(object sender, RoutedEventArgs e)
        {
            int res;
            try
            {
                res = Int32.Parse(textBoxPorta.Text);
            }
            catch (FormatException fe)
            {
                MyLogger.debug(fe.ToString());
                settings.setPorta(0); //valore non valido, con il get sarà restituito il default
                textBoxPorta.Text = settings.getPorta().ToString();
                return;
            }
            settings.setPorta(res);
        }

        private void textBoxIndIP_LostFocus(object sender, RoutedEventArgs e)
        {
            settings.setIP(textBoxIndIP.Text);
        }


        /*-------------------------------------------------------------------------------------------------------------*/
        /*---logic Tread methods---------------------------------------------------------------------------------------*/
        /*-------------------------------------------------------------------------------------------------------------*/

        //siamo nel secondo thread, quello che non gestisce la interfaccia grafica.
        private void logicThreadStart()
        {
            MyLogger.debug("LogicThread starting");
            
            //todo: prox riga solo per debug
            this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.busy);

            try //catch errori non recuperabili per il thread
            {
                //reset richieste eventualmente vecchie per il logic thread
                resetLogicThreadNeeds();

                //avvio tmer per verifica abort signals periodicamente
                AbortTimer.Start();

                //inizializzo oggetto per connessione con server
                sm = new SessionManager(settings.getIP(), settings.getPorta(), this);
                //bool connected = false;

                //gestione del login
                sm.login(settings.getUser(), settings.getPassw());
                logged = true;

                //selezione cartella
                sm.setRootFolder(settings.getRootFolder());

                //attiva modalità logged nella UI
                this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.logged);

                //voglio iniziare con una sync
                needToSync = true;

                //è la prima sync per questa connessione
                firstConnSync = true;

                //ciclo finchè la connessione è attiva. si esce solo con eccezione o con chiusura thread logico (anch'essa un'eccezione).
                while (true)
                {
                    //verifica se deve sincronizzare
                    if (needToSync)
                    {
                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.busy);

                        // se è la prima sincronizzazione di questa connessione al server, crea DirMonitor
                        if (firstConnSync)
                        {
                            firstConnSync = false;
                            try
                            {
                                //init del dirMonitor
                                d = new DirMonitor(settings.getRootFolder(), sm);
                            }
                            catch (EmptyDirException)
                            {
                                //se arrivo qui è perchè c'è stata la prima connessione, l'initial backup di una cartella vuota e il download delle recoverInfo sempre vuote.
                                firstConnSync = true;
                            }
                        }
                        else //non è la prima connessione
                        {
                            //scandisco root folder
                            d.scanDir();

                            //sincronizzo tutte le modifiche
                            SyncAll();
                        }
                        needToSync = false;
                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.logged);
                        MyLogger.print("Completata\n");
                    }

                    //verifica se deve richiedere l'intero ultimo backup
                    if (needToRecoverWholeBackup)
                    {
                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.busy);

                        sm.AskForSelectedBackupVersion(RecoveringQuery);
                        needToRecoverWholeBackup = false;

                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.logged);
                    }

                    //verifica se deve richiedere dati per ripristino di file vecchi
                    if (needToAskRecoverInfo)
                    {
                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.busy);

                        var recInfos = sm.askForRecoverInfo();
                        needToAskRecoverInfo = false;

                        System.Diagnostics.Debug.Assert(recoverW != null);

                        recoverW.Dispatcher.Invoke(DelSetRecoverInfos, recInfos);

                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.logged);
                    }

                    //recupera recoverRecord
                    if (needToAskForFileToRecover)
                    {
                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.busy);

                        //recupera file
                        sm.askForSingleFile(fileToRecover);

                        needToAskForFileToRecover = false;

                        this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.logged);
                    }

                    WaitForSyncTime();
                }

            } //fine try esterno
            catch (SocketException)
            {
                MyLogger.print("impossibile connettersi. Server non ragiungibile");
            }
            catch (LoginFailedException)
            {
                MyLogger.print("errore nel login. Correggere dati di accesso o creare nuovo utente.");
            }
            catch (RootSetErrorException)
            {
                MyLogger.print("errore nella selezione della cartella. Correggere il path");
            }
            catch (AbortLogicThreadException)
            {
                MyLogger.print("Connessione Interrotta\n");
                MyLogger.debug("LogicThread closing per abort logic thread exception");
            }
            catch(DirectoryNotFoundException)
            {
                MyLogger.print("impossibile trovare directory specificata. Selezionarne un'altra");
            }
            catch (Exception e) //eccezione sconosciuta.
            {
                MyLogger.line();
                MyLogger.debug(e.Message);
                MyLogger.line();
                MyLogger.debug(e.ToString());
                MyLogger.debug("LogicThread closing");
            }

            if (!TerminateLogicThread)
            {
                //setta la UI in modalità unlocked a meno che non sia TerminateLogicThread settata, 
                //se no mainThread va in join e va in deadlock (non esegue invoke())
                this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.notLogged);
            }

            sm.closeConnection();
            //disattivo il timer che sblocca periodicamente il logicThread affinchè controlli se deve abortire
            AbortTimer.Stop();
            //this.Dispatcher.Invoke(DelSetInterfaceLoggedMode, interfaceMode_t.notLogged);
            logged = false;
            //TODO: ma quando chiudo recoverW diventa null?
            if (recoverW != null)
                recoverW.Dispatcher.BeginInvoke(recoverW.DelCloseWindow);
            return; //logic thread termina qui
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
            HashSet<RecordFile> buffer;
            MyLogger.print("Sincronizzazione file aggiornati...");
            buffer = d.getUpdatedFiles();
            foreach (var f in buffer)
            {
                sm.syncUpdatedFile(f);
                d.confirmSync(f);
            }
            MyLogger.print("Ok.\n");
            MyLogger.print("Sincronizzazione nuovi file...");
            buffer = d.getNewFiles();
            foreach (var f in buffer)
            {
                sm.syncNewFiles(f);
                d.confirmSync(f);
            }
            MyLogger.print("Ok.\n");
            MyLogger.print("Sincronizzazione file cancellati...");
            buffer = d.getDeletedFiles();
            foreach (var f in buffer)
            {
                sm.syncDeletedFile(f);
                d.confirmSync(f, true);
            }
            MyLogger.print("Ok.\n");
            MyLogger.print("Sincronizzazione completata.\n");
        }

        /// <summary>
        /// logic thread checks if main thread wants him to close
        /// </summary>
        public bool shouldIClose()
        {
            return TerminateLogicThread;
        }

        private void buttShowPasswButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            textBoxClearPassword.Visibility = Visibility.Visible;
            textBoxClearPassword.Text = textboxPassword.Password;
        }

        private void buttShowPasswButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            textBoxClearPassword.Visibility = Visibility.Hidden;
            textBoxClearPassword.Text = "";
        }
    }
}
