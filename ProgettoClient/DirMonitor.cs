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

        private HashSet<RecordFile> newFiles = new HashSet<RecordFile>();
        private HashSet<RecordFile> updatedFiles = new HashSet<RecordFile>();
        private HashSet<RecordFile> deletedFiles = new HashSet<RecordFile>();

        private DispatcherTimer timer;
        private TimeSpan defaultInterval = new TimeSpan(0,1,0);

        /// <summary>
        /// modificandone il valore il timer [ automaticamente resettato.
        /// </summary>
        public TimeSpan Interval
        {
            set
            {
                if (value.Equals(TimeSpan.Zero))
                    value.Add(new TimeSpan(0, 1, 0));
                timer.Stop();
                timer.Interval = value;
                timer.Start();
            }
        }

        
        
        /// <summary>
        /// costruttore
        /// </summary>
        /// <param name="path">path della cartella da monitorare</param>
        /// <param name="interval">periodo tra una scansione e l'altra. se impostato a null viene messo a 1 minuto</param> 
        public DirMonitor(string path, TimeSpan interval)
        {
            myDir = new System.IO.DirectoryInfo(path);
            
            if (!myDir.Exists) 
                throw new System.IO.DirectoryNotFoundException(path);
            dim = new DirImageManager(myDir);
            doOnFile = checkFile;
            
            if (interval == TimeSpan.Zero)
                interval = new TimeSpan(0,0,1,0); //1 minuto
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(TimerHandler);
            
            //appena aperto faccio subito una scansione
            scanDir();
            //poi faccio partire il timer impostando l'intervallo
            this.Interval = interval;
        }

        private void TimerHandler(object sender, EventArgs e)
        {
            scanDir();
            //ricomincia
            timer.Start();
        }


        public void scanDir()
        {
            WalkDirectoryTree(myDir, doOnFile);
            deletedFiles = dim.getDeleted();
            dim.storeDirImage();
            MyLogger.add("deleted files:");
            foreach (var item in deletedFiles)
                MyLogger.add(item);
            MyLogger.add("scanDir done");
            MyLogger.line();
        }

        public void Pause()
        {
            timer.Stop();                                                                      
        }

        public void Continue()
        {
            timer.Start();
        }


    
        private void checkFile(System.IO.FileInfo fi)
        {
            MyLogger.add(fi.FullName);
            RecordFile thisFile = new RecordFile(fi);
            FileStatus status = dim.UpdateStatus(thisFile);
            switch (status)
            {                                    
                case FileStatus.New:
                    MyLogger.add("new: ");
                    newFiles.Add(thisFile);
                    break;
                case FileStatus.Updated:
                    MyLogger.add("updated: ");
                    updatedFiles.Add(thisFile);
                    break;
                case FileStatus.Old:
                    MyLogger.add("old: ");
                    //nothing to do
                    break;
            }
            MyLogger.add(thisFile);
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
                MyLogger.add(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                MyLogger.add(e.Message);
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
}
