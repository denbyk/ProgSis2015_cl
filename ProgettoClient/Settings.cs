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
        public string RootFolder;
        public double CycleTime;

        public Settings(string RootFolder, double CycleTime) 
        {
            this.RootFolder = RootFolder;
            this.CycleTime = CycleTime;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RootFolder", RootFolder, typeof(string));
            info.AddValue("CycleTime", CycleTime, typeof(double));
        }

        /// <summary>
        /// costruttore per la deserializzazione
        /// </summary>
        public Settings(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            this.RootFolder = (string)info.GetValue("RootFolder", typeof(string));
            this.CycleTime = (double)info.GetValue("CycleTime", typeof(double));
        }
    }
}
