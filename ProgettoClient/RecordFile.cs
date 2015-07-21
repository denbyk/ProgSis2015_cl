using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient
{
    /// <summary>
    /// record che racchiude nome+path, hash, size e lastModified del file
    /// </summary>
    class RecordFile
    {
        public string nameAndPath;
        public int hash;   //forse gli dovrò cambiare il tipo
        public int size;
        public System.DateTime lastModified;


        public RecordFile(string nameAndPath, int hash, int size, System.DateTime lastModified)
        {
            this.nameAndPath = nameAndPath;
            this.hash = hash;
            this.size = size;
            this.lastModified = lastModified;
        }

        //però se ho tutto uguale e solo la lastModified diversa dovrei considerarla la stessa????
        //forse non dovrei avere la lastModified in memoria?
        public override bool Equals(object obj)
        {
            RecordFile r = obj as RecordFile;
            if (r == null)
                return false;            
            if (nameAndPath.Equals(r.nameAndPath) &&
                    hash.Equals(hash) &&
                    size.Equals(size) &&
                    lastModified.Equals(lastModified))
                    return true;
            return false;
        }

        /// <summary>
        /// restituisce il formato corretto per la spediziona sul socket
        /// </summary>
        /// <returns></returns>
        public string toSendFormat()
        {
            throw new NotImplementedException();
        }



    }
}
