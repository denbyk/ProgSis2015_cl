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

        public Settings(string RootFolder) 
        {
            this.RootFolder = RootFolder;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RootFolder", RootFolder, typeof(string));
        }

        /// <summary>
        /// costruttore per la deserializzazione
        /// </summary>
        public Settings(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            this.RootFolder = (string)info.GetValue("RootFolder", typeof(string));
        }
    }
}
