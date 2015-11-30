using System;
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
        public string RootFolder;
        //tempo di ciclo. quando finisce viene fatta la sincronizzazione automatica se attiva.
        public double CycleTime;
        //stato on/off della sincronizzazione automatica
        public bool AutoSyncToggle;


        public Settings(string RootFolder, double CycleTime, bool AutoSyncToggle) 
        {
            this.RootFolder = RootFolder;
            this.CycleTime = CycleTime;
            this.AutoSyncToggle = AutoSyncToggle;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RootFolder", RootFolder, typeof(string));
            info.AddValue("CycleTime", CycleTime, typeof(double));
            info.AddValue("AutoSyncToggle", AutoSyncToggle, typeof(bool));
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
        }
    }
}
