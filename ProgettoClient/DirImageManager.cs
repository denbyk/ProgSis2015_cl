using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Security;


namespace ProgettoClient
{


    /// <summary>
    /// si occupa di tenere traccia della ultima versione conosciuta della cartella,
    /// scaricarla dal server e effettuare i confronti con record nuovi.
    /// </summary>
    class DirImageManager
    {
        private const string IMAGE_FILE_PATH = "DirImage.bin";

        //contiene immagine della ultima cartella sincronizzata nell forma <nome, RecordFile>.
        //questa è l'immagine come vista dal server, non dal client. Se un file vecchio non è mai stato
        //sincronizzato con il server apparirà cmq come nuovo.
        private Dictionary<string, RecordFile> dirImage;

        private System.IO.DirectoryInfo myDir;

        private BinaryFormatter formatter = new BinaryFormatter();

        private bool Updating = false;
        private HashSet<string> deletedFileNames;

        public DirImageManager(System.IO.DirectoryInfo myDir, SessionManager sm)
        {
            this.myDir = myDir;
            dirImage = new Dictionary<string, RecordFile>();
            loadDirImage(sm);
        }


        //~DirImageManager()
        //{
        //    storeDirImage();
        //}

        /// <summary>
        /// Verify if the RecordFile passed is new/updated/old by comparing with the last calculated state of the directory.
        /// Can't return deleted files. Use getDeleted() at the end of all the updates.
        /// </summary>
        /// <param name="rf"></param>
        /// <returns></returns>
        public FileStatus UpdateStatus(RecordFile rf)
        {
            if (!Updating)
            {
                Updating = true;
                //crea elenco dei deleted pieno inizialmente
                //deletedRecord = new Dictionary<string, RecordFile>(dirImage);
                deletedFileNames = new HashSet<string>(dirImage.Keys);
            }

            //cerca tra i già presenti
            RecordFile match;
            if (dirImage.TryGetValue(rf.nameAndPath, out match))
            {
                //se lo trovi elimina la voce dalla lista dei deletedFiles
                deletedFileNames.Remove(rf.nameAndPath);
                //RecordFile newRf = new RecordFile(rf);
                if (rf.Equals(match))
                    //i due file sono identici, non cambio nulla
                    return FileStatus.Old;
                else
                    //è aggiornato
                    dirImage.Remove(rf.nameAndPath);
                    //dirImage.Add(rf.nameAndPath, rf); add è fatto solo tramite ConfirmSync (quando ho finito di sincronizzarlo con il server)
                    return FileStatus.Updated;
            }
            else
            {
                //se non lo trovi è new
                //aggiungilo a DirImage
                //dirImage.Add(rf.nameAndPath, rf); add è fatto solo tramite ConfirmSync (quando ho finito di sincronizzarlo con il server)
                return FileStatus.New;
            }
        }

        internal void confirmSync(RecordFile rf)
        {
            dirImage.Add(rf.nameAndPath, rf);
        }

        public HashSet<RecordFile> getDeleted() //?? posso tornare l'oggetto private??
        {
            if (!Updating)
                throw new InvalidOperationException("you can't request deleted records while not updating file status");
            Updating = false;
            var res = new HashSet<RecordFile>();
            
            foreach (var item in deletedFileNames)
            {
                RecordFile temp;
                dirImage.TryGetValue(item, out temp);
                res.Add(temp);
                dirImage.Remove(item);
            }
            return res;
        }

        private void loadDirImage(SessionManager sm)
        {
            RecoverInfos recInfo;

            //richiede al server le info di recover
            recInfo = sm.askForRecoverInfo();

            //tiene solo ultima versione (stato attuale cartella su server)
            List<RecoverRecord> lastVersionInfos = recInfo.getVersionSpecificRecoverList(recInfo.getLastBackupVersionNumber());
            foreach (var rr in lastVersionInfos)
            {
                dirImage.Add(rr.rf.nameAndPath, rr.rf);
            }
        }

        /*VERSIONE VECCHIA, SALVATAGGIO SU DISCO.*/
        ////carica la loadDirImage da disco. se riceve un eccezione carica la versione _old. 
        ////se non riesce allora crea una imageDir vuota.
        ////differenziare per varie cartelle ?? se cambio cartella perdo i dati della cartella prima?
        //private void loadDirImage()
        //{
        //    //controllo di caricare la versione nuova e non un eventuale versione vecchia.
        //    FileStream fin;
        //    try
        //    {
        //        fin = new FileStream(IMAGE_FILE_PATH, FileMode.Open);
        //        dirImage = (Dictionary<string, RecordFile>)formatter.Deserialize(fin);
        //        fin.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        //in caso di file danneggiato o simili considero che precedente stato della cartella fosse tutta vuota.
        //        MyLogger.print("impossibile accedere a " + IMAGE_FILE_PATH + ". " + e.Message);
        //        dirImage = new Dictionary<string, RecordFile>();
        //        //throw;
        //    }
        //}

        public void storeDirImage()
        {
            //TODO:?prima scrivo quella nuova, poi elimino quella vecchia dal disco.
            //throw new NotImplementedException();

            try
            {
                FileStream fout = new FileStream(IMAGE_FILE_PATH, FileMode.Create);
                formatter.Serialize(fout, dirImage);
                fout.Close();
            }
            catch (Exception e)
            {
                MyLogger.print("impossibile scrivere immagine della directory su disco nel percorso: "
                    + IMAGE_FILE_PATH);
                MyLogger.print(e.Message);
                //TODO? è meglio fare rethrow o fare no???
                throw;
            }


        }
    }

}
