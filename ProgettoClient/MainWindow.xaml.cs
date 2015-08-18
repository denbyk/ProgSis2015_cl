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



namespace ProgettoClient
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DirMonitor d;
        DispatcherTimer timerTest;
        Thread logicThread;

        EventWaitHandle SycnNowEvent;
        bool SyncNowEventSignaled;


        bool TerminateLogicThread = false;


        /// <summary>
        /// modificandone il valore il timer è automaticamente resettato.
        /// </summary>
        public TimeSpan ScanInterval
        {
            set
            {
                if (value.Equals(TimeSpan.Zero))
                    value.Add(new TimeSpan(0, 1, 0));
                timerTest.Stop();
                timerTest.Interval = value;
                timerTest.Start();
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            MyLogger.add("si comincia");
            
            this.SycnNowEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            timerTest = new System.Windows.Threading.DispatcherTimer();
            timerTest.Tick += new EventHandler(TimerHandler);
            ScanInterval = new TimeSpan(0, 0, 3);

            //sgancio thread secondario
            logicThread = new Thread(logicThreadStart);
            logicThread.Start();
        }

        private void LogicThreadShutDown(){
            //per chiudere il LogicThread in modo ordinato
            lock(this)
            {
                 TerminateLogicThread = true;
                 SyncNowEventSignaled = true;
                 SycnNowEvent.Set(); //permette al logicThread di procedere.
            }
            logicThread.Join();
        }


        private void TimerHandler(object sender, EventArgs e)
        {
            MyLogger.add("Timer scaduto");
            SyncNowEventSignaled = true;
            SycnNowEvent.Set(); //permette al logicThread di procedere.
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
                    d = new DirMonitor("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test");

                    //aspetto evento timer o sincronizzazione manuale.
                    while(!SyncNowEventSignaled) //evita spurie
                        SycnNowEvent.WaitOne();
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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(buttSelSyncSir))
            {
                ///folderbrowser
            }
        }

        private void buttStartStopSync_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

