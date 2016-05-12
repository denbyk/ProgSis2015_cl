using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.IO;

namespace ProgettoClient
{
    /// <summary>
    /// record che racchiude nome+path, hash, size e lastModified del file
    /// </summary>
    [Serializable]
    public class RecordFile : ISerializable
    {
        public string nameAndPath;
        public byte[] hash;   
        public long size;    //in byte. se -1 l'informazione non è disponibile
        public System.DateTime lastModified;

        public RecordFile(string nameAndPath, byte[] hash, long size, System.DateTime lastModified)
        {
            this.nameAndPath = nameAndPath;
            this.hash = hash;
            this.size = size;
            this.lastModified = lastModified;
        }

        public RecordFile(System.IO.FileInfo fi)
        {
            nameAndPath = fi.FullName;
            hash = calcHash(); //TODO: TO IMPLEMENT
            size = fi.Length;
            lastModified = fi.LastWriteTime;
        }

        private byte[] calcHash()
        {
            var md5 = MD5.Create();
            var stream = File.OpenRead(this.nameAndPath);
            return md5.ComputeHash(stream); //TODO: lunghezza dell'hash non corretta!!!
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
        /// Nome completo file\r\n | Dimensione file (8 Byte) | Hash del file (32 Byte) | Timestamp (8 Byte)
        /// </summary>
        /// <returns></returns>
        public byte[] toSendFormat() //TODO: test it
        {
            byte[] sizeByteFormatted;
            byte[] hashByteFormatted;
            byte[] timeStampByteFormatted;

            //TODO: little endian??? gli zeri li metto all'inizio o alla fine?
            sizeByteFormatted = MyConverter.toFixedLengthByteArray(this.size);
            //datetime -> unix timestamp double -> byte array
            timeStampByteFormatted = MyConverter.toFixedLengthByteArray(MyConverter.DateTimeToUnixTimestamp(this.lastModified));
            hashByteFormatted = this.hash;

            //path + name + '\r\n'
            byte[] nameByteFormatted = MyConverter.UnicodeToByteArray(this.nameAndPath + Environment.NewLine);

            return nameByteFormatted.Concat(sizeByteFormatted).Concat(hashByteFormatted)
                .Concat(timeStampByteFormatted).ToArray();
        }


        private static void CopyAndAddPadding(byte[] initVett, byte[] finalVett)
        {
            int finalSize = finalVett.Length;
            int initSize = initVett.Length;

            if (initSize > finalSize) throw new Exception("info bigger than field");
            for (int i = 0; i < initSize; i++)
            {
                finalVett[i] = initVett[i];
            }
            for (int i = initSize; i < finalSize; i++)
            {
                finalVett[i] = 0;
            }
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
            this.hash = (byte[])info.GetValue("hash", typeof(byte[]));
            this.size = (long)info.GetValue("size", typeof(long));
            this.lastModified = (DateTime) info.GetValue("lastModified", typeof(DateTime));
        }


    

        public override string ToString()
        {
            return "RecordFile: " + nameAndPath + " " + hash + " " + size + " " + lastModified;
        }

        public static int TODOTODELETE(int a)
        {
            return 2 * a;
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