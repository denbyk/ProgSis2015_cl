﻿using System;
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
        

        public HashSet<RecordFile> getNewFiles() { return newFiles; }
        public HashSet<RecordFile> getUpdatedFiles() { return updatedFiles; }
        public HashSet<RecordFile> getDeletedFiles() { return deletedFiles; }
        
        /// <summary>
        /// costruttore
        /// </summary>
        /// <param name="path">path della cartella da monitorare</param>
        public DirMonitor(string path)
        {
            myDir = new System.IO.DirectoryInfo(path);
            
            if (!myDir.Exists) 
                throw new System.IO.DirectoryNotFoundException(path);
            dim = new DirImageManager(myDir);
            doOnFile = checkFile;
            scanDir();
            
        }


        public void scanDir()
        {
            //costriamo le 4 categorie (4 hashset)
            WalkDirectoryTree(myDir, doOnFile);
            deletedFiles = dim.getDeleted();

            //dim.storeDirImage(); devo salvare SOLO le DirImage con i file effettivamente sincronizzati
            
            //TODO: FORSE questo if serviva solo per debug?
            if (deletedFiles.Count > 0) { 
                MyLogger.print("deleted files:");
                foreach (var item in deletedFiles)
                    MyLogger.print(item);
            }
            //MyLogger.add("scanDir done");
            MyLogger.line();
        }


        //TODO?: questo sistema funziona con i file RIMOSSI? Sì, DOVREBBE. da testare.
        internal void confirmSync(RecordFile f)
        {
            dim.confirmSync(f);
            dim.storeDirImage(); //così salvo ogni file sincronizzato
        }



        //public void Pause()
        //{
        //    timer.Stop();                                                                      
        //}

        //public void Continue()
        //{
        //    timer.Start();
        //}


        /// <summary>
        /// delegato che inserisce il file in questione nell'appropriato hashSet 
        /// </summary>
        /// <param name="fi"></param>
        private void checkFile(System.IO.FileInfo fi)
        {
            //MyLogger.add(fi.Name + "\n");
            RecordFile thisFile = new RecordFile(fi);
            FileStatus status = dim.UpdateStatus(thisFile);
            switch (status)
            {                                    
                case FileStatus.New:
                    MyLogger.print("new: ");
                    newFiles.Add(thisFile);
                    break;
                case FileStatus.Updated:
                    MyLogger.print("updated: ");
                    updatedFiles.Add(thisFile);
                    break;
                case FileStatus.Old:
                    MyLogger.print("old: ");
                    //nothing to do
                    break;
            }
            //MyLogger.add(thisFile);
            MyLogger.print(fi.Name + "\n");
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
}
