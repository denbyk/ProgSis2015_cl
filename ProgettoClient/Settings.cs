using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;

namespace ProgettoClient
{
    [Serializable]
    public class Settings : ISerializable
    {
        //cartella di sincronizzazione
        private string RootFolder;
        //tempo di ciclo. quando finisce viene fatta la sincronizzazione automatica se attiva.
        private double CycleTime;
        //stato on/off della sincronizzazione automatica
        private bool AutoSyncToggle;
        //user/psw
        private string User;
        private string Passw;
        //IP e porta
        private string indIP;
        private int porta;

        //costanti di default
        //TODO change this
        private const string DEFAULT_FOLDERROOT_PATH = "C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test";
        private const double DEFAULT_CYCLE_TIME = 1;
        private const bool DEFAULT_AUTOSYNCTOGGLE = false;
        private const string DEFAULT_USER = "user";
        private const string DEFAULT_PASSW = "password";
        private const string DEFAULT_IP = "192.168.0.101";//"127.0.0.1";
        private const int DEFAULT_PORTA = 1200;

        public string getRootFolder() { return RootFolder == "" ? DEFAULT_FOLDERROOT_PATH : RootFolder; }
        public double getCycleTime() { return CycleTime == 0 ? DEFAULT_CYCLE_TIME : CycleTime; }
        public bool getAutoSyncToggle() { return AutoSyncToggle; }
        public string getUser() { return User == "" ? DEFAULT_USER : User; }
        public string getPassw() { return Passw == "" ? DEFAULT_PASSW : Passw; }
        public string getIP() { return indIP == "" ? DEFAULT_IP : indIP; }
        public int getPorta() { return porta == 0? DEFAULT_PORTA : porta; }

        public void setRootFolder(string RootFolder) { this.RootFolder = RootFolder.TrimEnd(Path.DirectorySeparatorChar); }
        public void setCycleTime(double CycleTime) { this.CycleTime = CycleTime; }
        public void setAutoSyncToggle(bool AutoSyncToggle) { this.AutoSyncToggle = AutoSyncToggle; }
        public void setUser(string User) { this.User = User; }
        public void setPassw(string Passw) { this.Passw = Passw; }
        public void setIP(string indIP) { this.indIP = indIP; }
        public void setPorta(int porta) { this.porta = porta; }

        public Settings(string RootFolder, double CycleTime, bool AutoSyncToggle, string User, string Passw, string indIP, int porta)
        {
            setRootFolder(RootFolder);
            this.CycleTime = CycleTime;
            this.AutoSyncToggle = AutoSyncToggle;
            this.User = User;
            this.Passw = Passw;
            this.indIP = indIP;
            this.porta = porta;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RootFolder", RootFolder, typeof(string));
            info.AddValue("CycleTime", CycleTime, typeof(double));
            info.AddValue("AutoSyncToggle", AutoSyncToggle, typeof(bool));
            info.AddValue("User", User, typeof(string));
            info.AddValue("Passw", Passw, typeof(string));
            info.AddValue("IndIP", indIP, typeof(string));
            info.AddValue("porta", porta, typeof(int));
        }

        /// <summary>
        /// costruttore per la deserializzazione
        /// </summary>
        public Settings(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            this.RootFolder = (string)info.GetValue("RootFolder", typeof(string));
            this.CycleTime = (double)info.GetValue("CycleTime", typeof(double));
            this.AutoSyncToggle = (bool)info.GetValue("AutoSyncToggle", typeof(bool));
            this.User = (string)info.GetValue("User", typeof(string));
            this.Passw = (string)info.GetValue("Passw", typeof(string));
            this.indIP = (string)info.GetValue("IndIP", typeof(string));
            this.porta = (int)info.GetValue("porta", typeof(int));
        }

        public Settings(): this(DEFAULT_FOLDERROOT_PATH, DEFAULT_CYCLE_TIME, DEFAULT_AUTOSYNCTOGGLE, DEFAULT_USER, DEFAULT_PASSW, DEFAULT_IP, DEFAULT_PORTA)
        {}


        /*
        private void checkForNullSettings(Settings s)
        {
            if (settings.getCycleTime == 0)
                settings.setCycleTime(DEFAULT_CYCLE_TIME);
            if (settings.getIP)
                settings.set
            if(settings.getPassw)
            if (settings.getRootFolder)
            if(settings.getUser)
        }
        */
    }
}
