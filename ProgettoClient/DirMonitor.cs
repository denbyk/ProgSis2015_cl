using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient
{
    /// <summary>
    /// classe che si occupa di monitorare la cartella prescelta ad intervalli regolari
    /// </summary>
    class DirMonitor
    {
        delegate void doOnFile_d(System.IO.FileInfo fi);
        
        private doOnFile_d doOnFile;
        private System.IO.DirectoryInfo myDir;
        

        /// <summary>
        /// costruttore
        /// </summary>
        /// <param name="path"> path della cartella da monitorare</param>
        public DirMonitor(string path)
        {
            myDir = new System.IO.DirectoryInfo(path);
            if (!myDir.Exists) 
                throw new System.IO.DirectoryNotFoundException(path);
            doOnFile = checkFile;
        }
        
        public void scanDir()
        {
            WalkDirectoryTree(myDir, doOnFile);
        }


    
        private void checkFile(System.IO.FileInfo fi)
        {
            MyLogger.add(fi.FullName);
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
