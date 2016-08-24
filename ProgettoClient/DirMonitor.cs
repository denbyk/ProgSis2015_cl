using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ProgettoClient
{
    enum FileStatus { New, Updated, Old, Deleted }; //rispetto alla versione precedente della cartella nota al clinet

    /// <summary>
    /// classe che si occupa di monitorare la cartella prescelta ad intervalli regolari
    /// </summary>
    class DirMonitor
    {
        delegate void doOnFile_d(System.IO.FileInfo fi);

        private doOnFile_d doOnFile;
        private System.IO.DirectoryInfo myDir;
        private DirImageManager dim;

        private HashSet<RecordFile> newFiles;
        private HashSet<RecordFile> deletedFiles;
        private HashSet<RecordFile> updatedFiles;

        private List<RecordFile> completeFileList;

        public HashSet<RecordFile> getNewFiles() { return newFiles; }
        public HashSet<RecordFile> getUpdatedFiles() { return updatedFiles; }
        public HashSet<RecordFile> getDeletedFiles() { return deletedFiles; }

        /// <summary>
        /// costruttore
        /// </summary>
        /// <param name="path">path della cartella da monitorare</param>
        public DirMonitor(string path, SessionManager sm)
        {
            myDir = new System.IO.DirectoryInfo(path);

            if (!myDir.Exists)
                throw new System.IO.DirectoryNotFoundException(path);

            try
            {
                dim = new DirImageManager(myDir, sm);
            }
            catch(InitialBackupNeededException ibne)
            {
                //TODO! ATTENZIONE, COME INTERROMPO L'APPLICAZIONE METRE STA FACENDO L'INITIAL BACKUP ????? al momento solo tra un file e l'altro
                
                //non c'è ancora nessun backup sul server, impossibile scaricare una dirImage. 
                //devo fare un initial backup completo. terminato quello bisogna riscaricare la dirImage
                completeFileList = new List<RecordFile>();
                
                //salvo vecchio delegato
                var buf = this.doOnFile;
                this.doOnFile = addAllFiles;
                
                //crea elenco completo file in root dir e subdirs
                WalkDirectoryTree(myDir, doOnFile);

                //rimetto delegato di prima
                this.doOnFile = buf;
                
                //effettua backup completo di tutti i file
                sm.sendInitialBackup(completeFileList);
                completeFileList = null;

                //se la cartella è vuota la new DirImageManager continua a fallire, viene rilanciata l'eccezione InitialBackupNeededException
                try
                {
                    //riscarico la dirImage
                    dim = new DirImageManager(myDir, sm);
                }
                catch (InitialBackupNeededException) { throw new EmptyDirException(); }
            }
            doOnFile = checkFile;
            init();
        }

        private void init()
        {
            newFiles = new HashSet<RecordFile>();
            updatedFiles = new HashSet<RecordFile>();
            deletedFiles = new HashSet<RecordFile>();
        }

        //va chiamata quando desidero eseguire un controllo sulle modifiche effettuate nella dir
        public void scanDir()
        {
            //resetto le 4 categorie
            init();

            //costriamo le 4 categorie (4 hashset)
            WalkDirectoryTree(myDir, doOnFile);
            deletedFiles = dim.getDeleted();

            if (deletedFiles.Count > 0)
            {
                MyLogger.print("deleted files:");
                foreach (var item in deletedFiles)
                    MyLogger.print(item);
            }
            //MyLogger.line();
        }


        internal void confirmSync(RecordFile f, bool deleting = false)
        {
            dim.confirmSync(f, deleting);
            //dim.storeDirImage(); non salvo + niente online
        }


        /// <summary>
        /// delegato che inserisce il file in questione nell'appropriato hashSet 
        /// </summary>
        /// <param name="fi"></param>
        private void checkFile(System.IO.FileInfo fi)
        {
            //MyLogger.add(fi.Name + "\n");
            RecordFile thisFile = new RecordFile(fi);
            if (fi.Length == 0)
                return;
            FileStatus status = dim.UpdateStatus(thisFile);
            switch (status)
            {
                case FileStatus.New:
                    MyLogger.print("nuovo: ");
                    newFiles.Add(thisFile);
                    MyLogger.print(fi.Name + "\n");
                    break;
                case FileStatus.Updated:
                    MyLogger.print("aggiornato: ");
                    updatedFiles.Add(thisFile);
                    MyLogger.print(fi.Name + "\n");
                    break;
                case FileStatus.Old:
                    MyLogger.debug("vecchio: ");
                    MyLogger.debug(fi.Name + "\n");
                    //nothing to do
                    break;
            }
            //MyLogger.add(thisFile);
            
        }

        /// <summary>
        /// delegato che inserisce tutti i file in una lista
        /// </summary>
        /// <param name="fi"></param>
        private void addAllFiles(System.IO.FileInfo fi)
        {
            if (fi.Length != 0)
                completeFileList.Add(new RecordFile(fi));
        }


        /// <summary>
        /// classe ricorsiva che scandisce tutto il tree di directory e chiama un delegato sui singoli file
        /// </summary>
        /// <param name="root"></param>
        private void WalkDirectoryTree(System.IO.DirectoryInfo root, doOnFile_d doOnFile)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder 
            try
            {
                //files = root.GetFiles("*.*");
                files = root.GetFiles();
            }
            // This is thrown if even one of the files requires permissions greater 
            // than the application provides. 
            catch (UnauthorizedAccessException e)
            {
                // da gestire il caso che non ho privilegi sufficienti ???
                MyLogger.print(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                MyLogger.print(e.Message);
                throw;
            }


            //e se file fosse null ma ci fossero sotto cartelle?? non devo ritornare!!
            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    // In this example, we only access the existing FileInfo object. If we 
                    // want to open, delete or modify the file, then 
                    // a try-catch block is required here to handle the case 
                    // where the file has been deleted since the call to TraverseTree().
                    //Console.WriteLine(fi.FullName);
                    doOnFile(fi);
                }
            }

            // Now find all the subdirectories under this directory.
            subDirs = root.GetDirectories();

            foreach (System.IO.DirectoryInfo dirInfo in subDirs)
            {
                // Resursive call for each subdirectory.
                WalkDirectoryTree(dirInfo, doOnFile);
            }

        }


    }
    class EmptyDirException : Exception { }
}
