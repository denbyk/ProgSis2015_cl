﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ProgettoClient
{
    [Serializable]
    class Settings : ISerializable
    {
        //cartella di sincronizzazione
        private string RootFolder;
        //tempo di ciclo. quando finisce viene fatta la sincronizzazione automatica se attiva.
        private double CycleTime;
        //stato on/off della sincronizzazione automatica
        private bool AutoSyncToggle;
        //user/psw
        private string User;
        private string Passw; //TODO: ? passw salvata ~ in chiaro su hd. è un problema?

        public string getRootFolder() { return RootFolder; }
        public double getCycleTime() { return CycleTime; }
        public bool getAutoSyncToggle() { return AutoSyncToggle; }
        public string getUser() { return User; }
        public string getPassw() { return Passw; }

        public void setRootFolder(string RootFolder) { this.RootFolder = RootFolder; }
        public void setCycleTime(double CycleTime) { this.CycleTime = CycleTime; }
        public void setAutoSyncToggle(bool AutoSyncToggle) { this.AutoSyncToggle = AutoSyncToggle; }
        public void setUser(string User) { this.User = User; }
        public void setPassw(string Passw) { this.Passw = Passw; }


        public Settings(string RootFolder, double CycleTime, bool AutoSyncToggle, string User, string Passw)
        {
            this.RootFolder = RootFolder;
            this.CycleTime = CycleTime;
            this.AutoSyncToggle = AutoSyncToggle;
            this.User = User;
            this.Passw = Passw;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RootFolder", RootFolder, typeof(string));
            info.AddValue("CycleTime", CycleTime, typeof(double));
            info.AddValue("AutoSyncToggle", AutoSyncToggle, typeof(bool));
            info.AddValue("User", User, typeof(string));
            info.AddValue("Passw", Passw, typeof(string));
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
        }
    }
}
