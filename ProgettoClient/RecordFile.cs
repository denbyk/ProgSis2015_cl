using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ProgettoClient
{
    /// <summary>
    /// record che racchiude nome+path, hash, size e lastModified del file
    /// </summary>
    [Serializable]
    class RecordFile : ISerializable
    {
        public string nameAndPath;
        public int hash;   //forse gli dovrò cambiare il tipo
        public long size;    //in byte
        public System.DateTime lastModified;


        public RecordFile(string nameAndPath, int hash, long size, System.DateTime lastModified)
        {
            this.nameAndPath = nameAndPath;
            this.hash = hash;
            this.size = size;
            this.lastModified = lastModified;
        }

        public RecordFile(System.IO.FileInfo fi)
        {
            nameAndPath = fi.FullName;
            hash = 1; //TO IMPLEMENT
            size = fi.Length;
            lastModified = fi.LastWriteTime;
        }

        public RecordFile(RecordFile rf)
        {
            this.nameAndPath = rf.nameAndPath;
            this.hash = rf.hash;
            this.size = rf.size;
            this.lastModified = rf.lastModified;
        }


        //però se ho tutto uguale e solo la lastModified diversa dovrei considerarla la stessa????
        //forse non dovrei avere la lastModified in memoria?
        public override bool Equals(object obj)
        {
            RecordFile r = obj as RecordFile;
            if (r == null)
                return false;            
            if (nameAndPath.Equals(r.nameAndPath) &&
                    hash.Equals(r.hash) &&
                    size.Equals(r.size) &&
                    lastModified.Equals(r.lastModified))
                    return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// restituisce il formato corretto del RecordFile per la spediziona sul socket
        /// Nome completo file\r\n | Dimensione file (8 Byte) | Hash del file (16 Byte) | Timestamp (8 Byte)
        /// </summary>
        /// <returns></returns>
        public string toSendFormat()
        {
            throw new NotImplementedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("nameAndPath", nameAndPath, typeof(string));
            info.AddValue("hash", hash);
            info.AddValue("size", size);
            info.AddValue("lastModified", lastModified, typeof(DateTime));
        }

        /// <summary>
        /// costruttore per la deserializzazione
        /// </summary>
        public RecordFile(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            this.nameAndPath = (string)info.GetValue("nameAndPath", typeof(string));
            this.hash = (int)info.GetValue("hash", typeof(int));
            this.size = (long)info.GetValue("size", typeof(long));
            this.lastModified = (DateTime) info.GetValue("lastModified", typeof(DateTime));
        }

        //probabilmente non serve
        //public RecordFile(string NameAndPath, Deleted notUsed){

        //}

    

        public override string ToString()
        {
            return "RecordFile: " + nameAndPath + " " + hash + " " + size + " " + lastModified;
        }

    }


    //probabilmente non serve
    ///// <summary>
    ///// just used to discriminate one the RecordFile constructor overloads
    ///// </summary>
    //static class Deleted{};
}


/** to delete*/
/*
[Serializable]
public class MyItemType : ISerializable
{
public MyItemType()
{
 // Empty constructor required to compile.
}

// The value to serialize. 
private string myProperty_value;

public string MyProperty
{
 get { return myProperty_value; }
 set { myProperty_value = value; }
}

// Implement this method to serialize data. The method is called  
// on serialization. 
public void GetObjectData(SerializationInfo info, StreamingContext context)
{
 // Use the AddValue method to specify serialized values.
 info.AddValue("props", myProperty_value, typeof(string));

}

// The special constructor is used to deserialize values. 
public MyItemType(SerializationInfo info, StreamingContext context)
{
 // Reset the property value using the GetValue method.
 myProperty_value = (string)info.GetValue("props", typeof(string));
}
}

/*to delete*/