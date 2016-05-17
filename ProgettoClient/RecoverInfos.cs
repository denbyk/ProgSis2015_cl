using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient
{
    public class RecoverInfos
    {
        myRecoverRecordEqComparer mrrEqCmp;
        List<RecoverRecord> recInfos;
        List<int> BackupVersionNumbers;
        
        public RecoverInfos(string stream)
        {
            this.init();
            //fa parsing dello stream e popola recInfos chiamando addRecoverRecord
            //throw new NotImplementedException();
        }
        private void init()
        {
            mrrEqCmp = new myRecoverRecordEqComparer();
            recInfos = new List<RecoverRecord>();
        }

        public RecoverInfos()
        {
            this.init();
            //RecordFile r1 = new RecordFile("primo", 32, -1, new DateTime(10,10,10));
            //RecordFile r2 = new RecordFile("secondo", 33, -2, new DateTime(20, 10, 10));
            //RecordFile r3 = new RecordFile("primo", 32, -3, new DateTime(10, 10, 10));

            //addRecoverRecord(r1, 1);
            //addRecoverRecord(r2, 2);
            //addRecoverRecord(r3, 3);
        }

        /// <summary>
        /// </summary>
        /// <param name="line">formato: // Invio: [Percorso completo]\r\n[Ultima modifica (16 char)][Hash (32 char)]\r\n </param>
        public void addRawRecord(string line, int backupVersion)
        {
            //separo campi di line
            string[] stringSeparators = new string[] { "\r\n" };
            string[] part = line.Split(stringSeparators, StringSplitOptions.None);

            //estraggo lastModify string
            long lmString = Convert.ToInt64(part[1].Substring(0, 16));
            string hashStr = part[1].Substring(16, 32);

            addRecoverRecord(
                new RecordFile(part[0], hashStr, -1, MyConverter.UnixTimestampToDateTime(lmString)),
                backupVersion);
        }

        public void removeRecoverRecord(RecoverRecord rr)
        {
            this.recInfos.Remove(rr);
            //TODO?: se rimuovo ultimo di una versione con quel numero potrei avere problemi se non tolgo il numero dalla recInfosVersionNumbers?
        }

        // aggiunge i recordFile e la sua versione alle RecoverInfos eliminando eventuali
        // ripetizioni di recordFile (stesso nome && stesso hash && stessa ultima mod).
        // poichè la stessa versione dello stesso file può comparire più volte sul server 
        //con numero di backup diverso,
        // la addRecoverRecord mantiene una sola entry in questo caso, la prima che riceve.
        private void addRecoverRecord(RecordFile rf, int backupVersion)
        {
            recInfos.Add(new RecoverRecord(rf, backupVersion));
            BackupVersionNumbers.Add(backupVersion);
        }

        /// <summary>
        /// restituisce la lista in cui sono rimossi record di file identici a meno della versione
        /// </summary>
        /// <returns></returns>
        public List<RecoverRecord> getRecoverUniqueList()
        {
            var uniqueRecInfos = recInfos.Distinct<RecoverRecord>(new myRecoverRecordEqComparer());
            return uniqueRecInfos.ToList<RecoverRecord>();
        }

        /// <summary>
        /// restituisce lista di recoverRecord di tutti e soli i record con versione = version
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public List<RecoverRecord> getVersionSpecificRecoverList(int version)
        {
            return recInfos.Where<RecoverRecord>(rr => rr.backupVersion == version).ToList<RecoverRecord>();
        }

        public List<int> getBackupVersionNumbers()
        {
            return BackupVersionNumbers;
        }

    }



    public class RecoverRecord
    {
        public RecordFile rf;
        public int backupVersion;

        public RecoverRecord(RecordFile rf, int backupVersion)
        {
            this.rf = rf;
            this.backupVersion = backupVersion;
        }
    }

    /// <summary>
    /// classe che confronta 2 recoverRecord dentro recInfos e li considera uguali se 
    /// hanno stesso rf.nameAndPath, rf.hash, rf.lastModify. Non confronta rf.size (info non disponibile)
    /// e nemmeno backupVersion (informazione non sempre significativa)
    /// </summary>
    class myRecoverRecordEqComparer : IEqualityComparer<RecoverRecord>
    {
        public bool Equals(RecoverRecord x, RecoverRecord y)
        {
            return x.rf.nameAndPath == y.rf.nameAndPath &&
                x.rf.hash == y.rf.hash &&
                x.rf.lastModified == y.rf.lastModified;
        }

        //restituisce hash di rf.nameAndPath, rf.hash, rf.lastModified. Le stesse variabili su cui è
        //effettuato il controllo Equals.
        public int GetHashCode(RecoverRecord obj)
        {
            int hCode = obj.rf.nameAndPath.GetHashCode() ^
                obj.rf.hash.GetHashCode() ^
                obj.rf.lastModified.GetHashCode();
            return hCode.GetHashCode();
        }
    }


}
