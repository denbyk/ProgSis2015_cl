using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;

namespace ProgettoClient
{
    enum commandsEnum
    {
        login,
        logout,
        loggedok,
        loginerr
    }

    //classe che si occupa di dialogare con il server.
    class SessionManager
    {
        private bool DEBUGGING_NO_SERVER = false; //TODO: remove


        //timeout (-1 = infinito):
        private int cnstReadTimeout = 5000; //ms /*5000000;*/
        private const int commLength = 8;
        private const string commLogin_str = "LOGIN___";
        private const string commAcc_str = "CREATEAC";
        private const string commLogout_str = "LOGOUT__";
        private const string commSetFold_str = "SET_FOLD";
        private const string commClrFolder_str = "SET_FOLD";
        private const string commDeleteFile = "DEL_FILE";
        private const string commNewFile = "NEW_FILE";
        private const string commUpdFile = "UPD_FILE";
        private const string commRecoverInfo = "FLD_STAT";
        private const string commRecoverFile = "FILE_SND";
        private const string commRecoverBackup = "SYNC_SND";
        private const string commDataRec = "DATA_REC";
        private const string commAlreadyLogged = "ALRTHERE";

        private const string commInitialBackup = "SYNC_RCV";
        private const string commIBNextFile = "REC_FILE";
        private const string commIBSyncEnd = "SYNC_END";


        private const string commFolderOk = "FOLDEROK";
        private const string commloggedok = "LOGGEDOK";
        private const string commloginerr = "LOGINERR";
        private const string commCmdAckFromServer = "CMND_REC";
        private const string commInfoAckFromServer = "INFO_OK_";
        private const string commDataAck = "DATA_OK_";
        private const string commMissBackupFromServer = "MISS_BCK";
        private const string commBackupOkFromServer = "BACKUPOK";
        private const string commSndAgain = "SNDAGAIN";
        private const string commDBERROR = "DB_ERROR";
        private const string commDELETED = "DELETED_";
        private const string commNOTDEL = "NOT_DEL_";
        private const string commNameTooLongPARTIAL = "MAXC_"; //"MAXC_---" dove --- sono il numero max di byte

        private string serverIP;
        private int serverPort;
        private byte[] user;
        private byte[] hashPassword;
        private byte[] separator_r_n;
        private byte[] rootFolder;

        private UTF8Encoding utf8;

        private System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        private NetworkStream serverStream;

        //private Dictionary<byte[], commandsEnum> commands;

        private MainWindow mainWindow;


        public SessionManager(string serverIP, int serverPort, MainWindow mainWindow)
        {
            this.serverIP = serverIP;
            this.serverPort = serverPort;
            this.mainWindow = mainWindow;

            utf8 = new UTF8Encoding();
            separator_r_n = utf8.GetBytes("\r\n");
        }

        public bool setRootFolder(string rootFolder)
        {
            this.rootFolder = utf8.GetBytes(rootFolder);
            sendToServer(commSetFold_str);
            waitForAck(commCmdAckFromServer);
            sendToServer(this.rootFolder);
            if (strRecCommFromServer().Equals(commFolderOk)) //dovrebbe ricevere sempre FOLDEROK
            {
                MyLogger.debug("cartella selezionata correttamente.\n");
                return true;
            }
            else
            {
                throw new UnknownServerResponseException();
            }
        }

        public void clearRootFolder()
        {
            sendToServer(commClrFolder_str);
            if (strRecCommFromServer().Equals(commFolderOk))
            {
                this.rootFolder = null;
            }
            else
            {
                throw new UnknownServerResponseException();
            }
        }

        /// <summary>
        /// restitrusce true se login ha avuto successo. se no false.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public void login(string user, string password)
        {
            if (mainWindow.logged)
                logout();

            if (!clientSocket.Connected)
                newConnection();

            //string -> utf8
            this.user = utf8.GetBytes(user);

            SHA256 mySha256 = SHA256Managed.Create();
            byte[] utf8psw = utf8.GetBytes(password);
            //hash(utf8(psw))
            byte[] hashPswByte = mySha256.ComputeHash(utf8psw, 0, utf8psw.Length);
            string hex = BitConverter.ToString(hashPswByte).Replace("-", string.Empty); //rappresentazione in esadecimale -> 32 caratteri.
            this.hashPassword = utf8.GetBytes(hex);

            sendToServer(commLogin_str);

            //invio "[username]\r\n[sha-256_password]\r\n"
            byte[] userPassword = ConcatByte(this.user, separator_r_n);
            userPassword = ConcatByte(userPassword, this.hashPassword);
            userPassword = ConcatByte(userPassword, this.separator_r_n);
            sendToServer(userPassword);
            switch (/*commloggedok)*/strRecCommFromServer())
            {
                case commloggedok:
                    mainWindow.logged = true;
                    break;
                case commloginerr:
                    //create_ac?
                    MyLogger.print("errore nel login\n");
                    bool wantNewAcc = (bool)mainWindow.Dispatcher.Invoke(mainWindow.DelAskNewAccount);
                    if (wantNewAcc)
                    {
                        //newConnection();
                        createAccount(user, password); //login automatico
                        mainWindow.logged = true;
                        return;
                    }
                    else
                    {
                        mainWindow.logged = false;
                        throw new LoginFailedException();
                    }
                    break;
                case commAlreadyLogged:
                    MyLogger.print("utente già connesso");
                    mainWindow.logged = false;
                    throw new LoginFailedException();
                    break;
                default:
                    mainWindow.logged = false;
                    throw new LoginFailedException();
                    break;
            }

        }

        public void createAccount(string user, string password)
        {
            //string -> utf8
            this.user = utf8.GetBytes(user);


            //per riferimento, così faccio con md5
            //byte[] x = md5.ComputeHash(stream); //char di 16 caratteri.
            //string hex = BitConverter.ToString(x).Replace("-", string.Empty); //rappresentazione in esadecimale -> 32 caratteri.
            //return hex;

            SHA256 mySha256 = SHA256Managed.Create();
            byte[] utf8psw = utf8.GetBytes(password);
            //hash(utf8(psw))
            byte[] hashPswByte = mySha256.ComputeHash(utf8psw, 0, utf8psw.Length);
            string hex = BitConverter.ToString(hashPswByte).Replace("-", string.Empty); //rappresentazione in esadecimale -> 32 caratteri.
            this.hashPassword = utf8.GetBytes(hex);

            sendToServer(commAcc_str);

            //invio "[username]\r\n[sha-256_password]\r\n"
            byte[] userPassword = ConcatByte(this.user, separator_r_n);
            userPassword = ConcatByte(userPassword, this.hashPassword);
            userPassword = ConcatByte(userPassword, this.separator_r_n);

            sendToServer(userPassword);
            string risp = strRecCommFromServer();

            switch (risp)
            {
                case commloggedok:
                    MyLogger.print("Nuovo utente creato con successo");
                    return;
                    break;
                case commDBERROR:
                    MyLogger.print("Utente non valido, ritentare con un'altro utente");
                    throw new AbortLogicThreadException();
                    break;
                default:
                    if (risp.Substring(0, 5) == commNameTooLongPARTIAL)
                    {
                        //nome utente troppo lungo.
                        int maxLength = Int32.Parse(risp.Substring(5, 3));
                        mainWindow.Dispatcher.Invoke(mainWindow.DelShowOkMsg, "Errore, nome utente troppo lungo. Max: " + maxLength, MessageBoxImage.Error);
                        throw new AbortLogicThreadException();
                    }
                    else
                        throw new UnknownServerResponseException();
                    break;
            }

        }

        public void closeConnection()
        {
            clientSocket.Close();
        }

        private void newConnection()
        {
            MyLogger.print("Tentativo di connessione in corso...");
            try
            {
                //se già connesso abort del thread logico
                if (clientSocket.Connected)
                {
                    throw new DoubleConnectionException();
                }
                //System.Net.IPAddress address = System.Net.IPAddress.Parse(serverIP);
                clientSocket.Connect(serverIP, serverPort);
                serverStream = clientSocket.GetStream();
                serverStream.ReadTimeout = cnstReadTimeout;
            }
            catch (SocketException se)
            {
                MyLogger.print("Collegamento al server fallito\n");
                MyLogger.debug(se);
                throw;
            }
            MyLogger.print("Connesso\n");
        }

        private string strRecCommFromServer() 
        {
            return utf8.GetString(recCommFromServer());
        }

        private byte[] recCommFromServer()
        {
            byte[] res = new byte[commLength];
            try
            {
                serverStream.Read(res, 0, res.Length);
            }
            catch (Exception e) when (e is IOException || e is ObjectDisposedException)
            {
                //L'oggetto Socket sottostante è chiuso.
                //-oppure -
                //La classe NetworkStream è chiusa.
                //-oppure -
                //Si è verificato un errore durante la lettura dalla rete.
                MyLogger.print("Errore nella comunicazione con il server");
                MyLogger.debug(e.ToString());
                throw new SocketException();
                //mainWindow.
            }
            catch (Exception e)
            {
                MyLogger.debug(e.ToString());
                throw;
            }
            return res;
        }

        private void sendToServer(byte[] toSend)
        {
            //todo
            if (DEBUGGING_NO_SERVER)
                return;
            serverStream.Write(toSend, 0, toSend.Length);
            serverStream.Flush();
        }

        private void waitForAck(string ackExpected)
        {
            //todo
            if (DEBUGGING_NO_SERVER)
                return;
            string ack = strRecCommFromServer();
            if (ack == ackExpected)
                return;
            throw new AckErrorException();
        }

        //conversione se uso string
        private void sendToServer(string toSend)
        {
            sendToServer(utf8.GetBytes(toSend.ToCharArray()));
        }

        private byte[] ConcatByte(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            System.Buffer.BlockCopy(a, 0, c, 0, a.Length);
            System.Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }

        internal void logout()
        {
            try
            {
                sendToServer(commLogout_str);
                MyLogger.print("disconnessione in corso...");

                //todo: da qui esce una socketexception da catchare
                waitForAck(commCmdAckFromServer);

                MyLogger.print("disconnessione effettuata\n");

                mainWindow.logged = false;
            }
            catch(Exception e)
            {
                MyLogger.debug(e);
            }
        }


        public void sendInitialBackup(List<RecordFile> RecordFileList)
        {
            //TODO! nota: durante l'upload di un file grosso deve aspettare la fine dell'upload per chiudersi. 
            //forse è meglio che il main process non faccia join ma si chiuda brutalmente?
            sendToServer(commInitialBackup);
            waitForAck(commCmdAckFromServer);

            MyLogger.print("Primo Backup in corso...");
            foreach (var rf in RecordFileList)
            {
                if (mainWindow.shouldIClose())
                    throw new AbortLogicThreadException();
                MyLogger.debug("invio del file " + rf.nameAndPath);
                sendToServer(commIBNextFile);
                SendWholeFileToServer(rf);
                MyLogger.debug("completato\n");
            }
            sendToServer(commIBSyncEnd);

            MyLogger.print("Primo Backup eseguito con successo\n");
        }


        internal void syncDeletedFile(RecordFile rf)
        {
            MyLogger.debug("deleting file: " + rf.nameAndPath);
            sendToServer(commDeleteFile);
            waitForAck(commCmdAckFromServer);

            sendToServer(rf.nameAndPath);

            waitForAck(commInfoAckFromServer);
            MyLogger.debug("deleted\n");
            string res = strRecCommFromServer();
            if (res == commDELETED)
            {
                MyLogger.debug("deleted\n");
            }
            if (res == commNOTDEL)
            {
                MyLogger.debug("file inesistente.\n");
            }
        }

        internal void syncUpdatedFile(RecordFile rf)
        {
            MyLogger.debug("updating file: " + rf.nameAndPath);
            sendToServer(commUpdFile);
            waitForAck(commCmdAckFromServer);
            //aspettare eventuale MISS_BCK o BACKUPOK
            if (strRecCommFromServer() == commBackupOkFromServer)
            {
                SendWholeFileToServer(rf);
                MyLogger.debug("updated\n");
            }
            else
                throw new AckErrorException();
        }

        internal void syncNewFiles(RecordFile rf)
        {
            MyLogger.debug("newing file: " + rf.nameAndPath);
            sendToServer(commNewFile);
            waitForAck(commCmdAckFromServer);
            //aspettare eventuale MISS_BCK o BACKUPOK
            if (strRecCommFromServer() == commBackupOkFromServer)
            {
                SendWholeFileToServer(rf);
                MyLogger.debug("newed\n");
            }
            else
                throw new AckErrorException();
        }


        public RecoverInfos askForRecoverInfo()
        {
            
            //todo: remove
            if (DEBUGGING_NO_SERVER)
            {
                RecoverInfos risdbg = new RecoverInfos();
                risdbg.addRawRecord("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test\\1\r\n000000111111111100000000000000000000000000000000", 1);
                risdbg.addRawRecord("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test\\2\r\n000000111111111100000000000000000000000000000000", 2);
                risdbg.addRawRecord("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test\\3\r\n000000111111111100000000000000000000000000000000", 3);
                risdbg.addRawRecord("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test\\4\r\n000000111111111100000000000000000000000000000000", 4);
                risdbg.addRawRecord("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test\\5\r\n000000111111111100000000000000000000000000000000", 5);
                risdbg.addRawRecord("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test\\6\r\n000000111111111100000000000000000000000000000000", 5);
                risdbg.addRawRecord("C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test\\7\r\n000000111111111100000000000000000000000000000000", 7);
                risdbg.addRawRecord("percorso\r\n000000111111111100000000000000000000000000000000", 1);
                return risdbg;
            }
            //*/
            sendToServer(commRecoverInfo);
            waitForAck(commCmdAckFromServer);

            RecoverInfos ris = new RecoverInfos();
            try
            {
                //leggi numero di versioni
                int numVers = Convert.ToInt32(socketReadline());
                if (numVers == 0)
                {
                    MyLogger.debug("primo backup necessario");
                    return null;
                }
                int nFile;
                int nVersCurr;
                //per ogni versione
                for (int bv = 1; bv <= numVers; bv++)
                {
                    nVersCurr = Convert.ToInt32(socketReadline());
                    if (nVersCurr != bv)
                        throw new Exception("nVersCurr != bv !!!!");
                    nFile = Convert.ToInt32(socketReadline());
                    //per ogni file
                    for (int f = 0; f < nFile; f++)
                    {
                        // [Percorso completo]\r\n[Ultima modifica -> 16byte][Hash -> 32char]\r\n
                        ris.addRawRecord(socketReadline() + "\r\n" +  socketReadline(), bv);
                        sendToServer(commDataRec);
                    }
                }

            }
            catch (Exception e)
            {
                MyLogger.debug("errore in askForRecoverInfo\n");
                MyLogger.debug(e.ToString());
                throw;
            }
            return ris;
        }

        /// <summary>
        /// non restituisce \r\n finale
        /// </summary>
        /// <returns></returns>
        private string socketReadline()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            byte[] buf = new byte[1];
            char c = 'a';
            bool Rreceived = false;
            while (true)
            {
                serverStream.Read(buf, 0, buf.Length);
                c = Convert.ToChar(buf[0]);
                if (c == '\r')
                {
                    Rreceived = true;
                }
                //se ho ricevuto \r\n:
                if (c == '\n' && Rreceived)
                {
                    //elimina il \r gi# memorizzato in sb
                    sb.Remove(sb.Length - 1, 1);
                    break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        
        private void sendFileContent(RecordFile f)
        {
            /*    int progressValue = 0;
                do
                {
                    bytesRead = serverStream.Read(buffer, 0, buffer.Length);
                    fout.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    //aggiorna progress bar
                    if (totalBytesRead % stepSize == 0)
                    {
                        progressValue += 10;
                        mainWindow.Dispatcher.BeginInvoke(mainWindow.DelSetProgressValue, progressValue);
                    }
                }
                while (totalBytesRead < sizeFile);

            mainWindow.Dispatcher.BeginInvoke(mainWindow.DelSetProgressValue, 100);
            */
            try
            {
                               
                const int bufsize = 1024;
                var buffer = new byte[bufsize];
                int actuallyRead = 0;
                long sizeFile = 0;
                long totalBytesRead = 0;
                int progressValue = 0;
                long stepSize = (sizeFile / 1024) / 10;

                using (var s = File.OpenRead(f.nameAndPath))
                {
                    sizeFile = s.Length;
                    
                    while ((actuallyRead = s.Read(buffer, 0, bufsize)) > 0)
                    {
                        serverStream.Write(buffer, 0, actuallyRead);
                        
                        //aggiorna progress bar
                        totalBytesRead += actuallyRead;
                        if (totalBytesRead % stepSize == 0)
                        {
                            progressValue += 10;
                            mainWindow.Dispatcher.BeginInvoke(mainWindow.DelSetProgressValue, progressValue);
                        }
                    }
                }
                serverStream.Flush();
                
            }
            catch (Exception ex)
            {
                MyLogger.debug(ex.ToString());
                MyLogger.print("errore leggendo il file");
                throw;
            }
        }

        public void askForSingleFile(RecoverRecord rr)
        {
            FileStream fout;
            string localFileName;
            MyLogger.print("Ripristino file in corso...");
            sendToServer(commRecoverFile);
            waitForAck(commCmdAckFromServer);
            try
            {
                if (File.Exists(rr.rf.nameAndPath))
                {
                    //chiede se sovrascrivere
                    string message = "file già esistente. si desidera sovrascriverlo?";
                    string caption = "Attenzione!";

                    bool wantOverwrite = (bool)mainWindow.recoverW.Dispatcher.Invoke(mainWindow.recoverW.DelYesNoQuestion, message, caption);
                    if (!wantOverwrite)
                    {
                        //salvare con nome
                        throw new IOException("need to save with name");
                    }
                }
                else
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(rr.rf.nameAndPath));
                }
                localFileName = rr.rf.nameAndPath;
                fout = File.Open(localFileName, FileMode.OpenOrCreate);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                //file omonimo esiste già o altri errori nell'aprire il file. apro una dialog di salvataggio
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.InitialDirectory = mainWindow.settings.getRootFolder();
                sfd.FileName = rr.rf.getJustName();
                Nullable<bool> result = sfd.ShowDialog();
                localFileName = sfd.FileName;
                if (result == true)
                {
                    try
                    {
                        fout = File.Open(localFileName, FileMode.Create);
                    }
                    catch (Exception)
                    {
                        //mainWindow.Dispatcher.Invoke(mainWindow.DelShowOkMsg, "Impossibile aprire il file", MessageBoxImage.Error);
                        MyLogger.popup("Impossibile aprire il file. Operazione Annullata\n", MessageBoxImage.Error);
                        MyLogger.print("Impossibile aprire il file. Operazione Annullata\n");
                        //annullo richiesta recupero di questo file
                        return;
                    }
                }
                else
                {
                    MyLogger.print("Operazione Annullata\n");
                    //annullo richiesta recupero di questo file
                    return;
                }
            }

            //invio nome singolo file
            sendToServer(rr.rf.nameAndPath + "\r\n" + rr.backupVersion.ToString() + "\r\n");


            System.DateTime LastModifyDate;
            try
            {
                //ricevi contenuto file
                LastModifyDate = RecFileContent(localFileName, fout);
            }
            catch (CancelFileRequestException)
            {
                MyLogger.print("Operazione Annullata\n");
                fout.Close();
                deleteFile(localFileName);
                return;
            }
            catch (IOException)
            {
                MyLogger.popup("Impossibile accedere al file. Operazione annullata.", MessageBoxImage.Error);
                fout.Close();
                deleteFile(localFileName);
                return;
            }

            fout.Close();

            FileInfo fi = new FileInfo(localFileName);
            fi.LastWriteTime = LastModifyDate;
            MyLogger.print("completato.\n");
        }

        private void deleteFile(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch(Exception e)
            {
                MyLogger.print("impossibile eliminare il file errato: " + fileName);
                MyLogger.debug(e);
            }
        }

        private void SendWholeFileToServer(RecordFile rf)
        {
            int tryes = 0;
            while (tryes < 6)
            {
                //invio info del file
                sendToServer(rf.toSendFormat());
                //check ack
                string resp = strRecCommFromServer();
                if (resp != commInfoAckFromServer)
                {
                    if (resp == commSndAgain)
                    {
                        tryes++;
                        continue;
                    }
                    else
                    {
                        throw new AckErrorException();
                    }
                }

                //invio contenuto del file
                sendFileContent(rf);

                //check ack
                resp = strRecCommFromServer();
                if (resp != commDataAck)
                {
                    if (resp == commSndAgain)
                    {
                        tryes++;
                        continue;
                    }
                    else
                    {
                        throw new AckErrorException();
                    }
                }
                break;
            }
        }


        /// <summary>
        /// return true se ricezione corretta, false altrimenti
        /// </summary>
        /// <param name="fout">already opened output fileStream</param>
        /// <returns>last modify date of the file</returns>
        private System.DateTime RecFileContent(string FileNameAndPath, FileStream fout)
        {
            //ricezione hash
            byte[] hashReceived = new byte[32];
            var count = serverStream.Read(hashReceived, 0, 32);
            string strHash = System.Text.Encoding.UTF8.GetString(hashReceived);
            
            //remove \r\n
            socketReadline();

            //legge dimensione del file
            long sizeFile = Convert.ToInt64(socketReadline(), 16);

            //legge data_ultima_modifica file
            var lmfile = MyConverter.UnixTimestampToDateTime(Convert.ToInt64(socketReadline(),16));

            //data_rec
            sendToServer(commDataRec);

            int attempt = 0;
            do
            {
                //ricezione e salvataggio su disco.
                var buffer = new byte[1024];
                int bytesRead;
                long totalBytesRead = 0;
                fout.Seek(0, SeekOrigin.Begin);
                //step della progress bar
                long stepSize = (sizeFile / 1024) / 10;
                int progressValue = 0;
                do
                {
                    bytesRead = serverStream.Read(buffer, 0, buffer.Length);
                    fout.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    //aggiorna progress bar
                    if (totalBytesRead % stepSize == 0)
                    {
                        progressValue += 10;
                        mainWindow.Dispatcher.BeginInvoke(mainWindow.DelSetProgressValue, progressValue);
                    }
                }
                while (totalBytesRead < sizeFile/*serverStream.DataAvailable*/);

                mainWindow.Dispatcher.BeginInvoke(mainWindow.DelSetProgressValue, 100);

                if (totalBytesRead != sizeFile)
                {
                    sendToServer(commSndAgain);
                    attempt++;
                    if (attempt < 5)
                        continue;
                    else
                        break;
                }

                //calcolo e confronto hash
                var computedHash = RecordFile.calcHash(FileNameAndPath, fout);
                if (computedHash == strHash)
                {
                    sendToServer(commDataAck);
                    return lmfile;
                }
                else
                {
                    sendToServer(commSndAgain);
                    attempt++;
                }
            }
            while (attempt < 5);
            
            MyLogger.print("errore nel download del file. impossibile effettuare il ripristino.\n");
            //todo: catchare questa eccezione nelle funzioni chiamanti
            throw new CancelFileRequestException();
        }



        internal void AskForSelectedBackupVersion(MainWindow.RecoveringQuery_st recQuery)
        {
            int version = recQuery.versionToRecover;
            MyLogger.print("Ripristino della versione " + recQuery.versionToRecover +"... ");
            sendToServer(commRecoverBackup);
            waitForAck(commCmdAckFromServer);
            //seleziono versione
            sendToServer(version.ToString());

            int fileCount = recQuery.recInfos.getVersionSpecificCount(version);

            try
            {
                for (int i = 0; i < fileCount; i++)
                {

                    //legge nome del file
                    string fileName = socketReadline();
                    string newPathAndName;
                    //definisce percorso dove salvare il file
                    if (recQuery.recoveringFolderPath != "")
                    {
                        //path da usare: quello specificato da utente
                        System.Diagnostics.Debug.Assert(fileName.Contains(mainWindow.settings.getRootFolder()));
                        //elimina la rootFolder. lascia // iniziale
                        string localPath = fileName.Substring(mainWindow.settings.getRootFolder().Length);
                        newPathAndName = recQuery.recoveringFolderPath.TrimEnd(Path.AltDirectorySeparatorChar) + localPath;
                    }
                    else
                    {
                        //path da usare: quello originale del file
                        newPathAndName = fileName;
                    }

                    //apro il file
                    FileStream fout;
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newPathAndName));
                    try
                    {
                        fout = new FileStream(newPathAndName, FileMode.Create);
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        //se file è protetto ne crea una copia a fianco
                        newPathAndName += "-restoredCopy";
                        fout = new FileStream(newPathAndName, FileMode.Create);
                        MyLogger.print("Impossibile ripristinare il file " + newPathAndName + ", salvo con suffisso \"restoredCopy\"\n");
                    }

                    System.DateTime LastModifyDate;
                    try
                    {
                        LastModifyDate = RecFileContent(newPathAndName, fout);
                        fout.Close();
                        FileInfo fi = new FileInfo(newPathAndName);
                        fi.LastWriteTime = LastModifyDate;
                        
                    }
                    catch (CancelFileRequestException)
                    {
                        MyLogger.print("Operazione Annullata\n");
                        fout.Close();
                        deleteFile(newPathAndName);
                        return;
                    }
                    catch(IOException)
                    {
                        MyLogger.popup("Impossibile accedere al file " + newPathAndName + " Operazione interrotta.", MessageBoxImage.Error);
                        fout.Close();
                        deleteFile(newPathAndName);
                        return;
                    }

                }

                //mainWindow.Dispatcher.Invoke(mainWindow.DelShowOkMsg, "Ripristino versione completato!", MessageBoxImage.Information);
                //MyLogger.print("Ripristino versione " + recQuery.versionToRecover.ToString() + " riuscito");
                MyLogger.popup("Ripristino versione " + recQuery.versionToRecover.ToString() + " riuscito", MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                MyLogger.print("ripristino fallito");
                mainWindow.Dispatcher.Invoke(mainWindow.DelShowOkMsg, "Ripristino versione fallita", MessageBoxImage.Error);
                MyLogger.debug(e.ToString());
                return;
            }
        }

        //internal bool checkSocketStillOpen()
        //{
        //    return clientSocket.Connected;
        //}
    }

    class DoubleConnectionException : Exception { }

    class CancelFileRequestException : Exception { }

    class LoginFailedException : Exception { }

    class AckErrorException : Exception { }

    class UnknownServerResponseException : Exception { }

    class RootSetErrorException : Exception { }

    class InitialBackupNeededException : Exception { }

    
}
