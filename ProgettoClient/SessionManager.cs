using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;

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
        //TODO: set a timeout sensato (-1 = infinito):
        private int cnstReadTimeout = -1; //ms
        private const int commLength = 8;
        private const string commLogin_str = "LOGIN___";
        private const string commLogout_str = "LOGOUT__";
        private const string commSetFold_str = "SET_FOLD";
        private const string commClrFolder_str = "SET_FOLD";
        private const string commDeleteFile = "DEL_FILE";
        private const string commNewFile = "NEW_FILE";
        private const string commUpdFile = "UPD_FILE";
        private const string commRecoverInfo = "FLD_STAT";
        private const string commRecoverFile = "FILE_SND";
        private const string commRecoverBackup = "SYNC_SND";

        private const string commFolderOk = "FOLDEROK";
        private const string commloggedok = "LOGGEDOK";
        private const string commloginerr = "LOGINERR";
        private const string commCmdAckFromServer = "CMND_REC";
        private const string commInfoAckFromServer = "INFO_OK_";
        private const string commDataAckFromServer = "DATA_OK_";
        private const string commSndAgain = "SNDAGAIN";


        //todo?: inserire commDBError

        private string serverIP;
        private int serverPort;
        private byte[] user;
        private byte[] hashPassword;
        private byte[] separator_r_n;
        private byte[] rootFolder;

        private UTF8Encoding utf8;

        private System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        private NetworkStream serverStream;

        private bool logged;

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
            sendToServer(this.rootFolder);
            if (strRecCommFromServer().Equals(commFolderOk)) //dovrebbe ricevere sempre FOLDEROK
            {
                MyLogger.print("cartella selezionata correttamente.\n");
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
            if (logged)
                logout();

            if (!clientSocket.Connected)
                newConnection();

            //string -> utf8
            this.user = utf8.GetBytes(user);

            SHA256 mySha256 = SHA256Managed.Create();
            byte[] utf8psw = utf8.GetBytes(password);
            //hash(utf8(psw))
            this.hashPassword = mySha256.ComputeHash(utf8psw, 0, utf8psw.Length);

            sendToServer(commLogin_str);

            //invio "[username]\r\n[sha-256_password]\r\n"
            byte[] userPassword = ConcatByte(this.user, separator_r_n);
            userPassword = ConcatByte(userPassword, this.hashPassword);
            userPassword = ConcatByte(userPassword, this.separator_r_n);
            sendToServer(userPassword);
            switch (commloggedok)//strRecCommFromServer())
            {
                case commloggedok:
                    logged = true;
                    break;
                case commloginerr:
                    //create_ac?
                    MyLogger.print("errore nel login\n");
                    bool wantNewAcc = (bool)mainWindow.Dispatcher.Invoke(mainWindow.DelAskNewAccount);
                    if (wantNewAcc)
                    {
                        createAccount();
                        login(user, password);
                        return;
                    }
                    else
                    {
                        throw new LoginFailedException();
                    }
                    break;
            }

        }

        private void createAccount()
        {
            throw new NotImplementedException();
        }

        private void newConnection()
        {
            MyLogger.print("Tentativo di connessione in corso...");
            try
            {
                clientSocket.Connect(serverIP, serverPort);
                serverStream = clientSocket.GetStream();
                serverStream.ReadTimeout = cnstReadTimeout;
            }
            catch (SocketException)
            {
                MyLogger.print("Collegamento al server fallito\n");
                throw;
            }
            MyLogger.print("Connesso\n");
        }

        private string strRecCommFromServer() //TODO: idea: se qui controllo e se il server mi ha inviato DB_ERROR lanciassi una eccezione?
        {
            return utf8.GetString(recCommFromServer());
        }

        private byte[] recCommFromServer()
        {
            byte[] res = new byte[commLength];
            try
            {
                serverStream.Read(res, 0, res.Length); //TODO: e se la connessione si interrompe?
            }
            catch(Exception e) when (e is IOException || e is ObjectDisposedException)
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
            serverStream.Write(toSend, 0, toSend.Length);
            serverStream.Flush();
        }

        private void waitForAck(string ackExpected)
        {
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

        internal void logout() //TODO: test it.
        {
            if (!logged)
                return;

            sendToServer(commLogout_str);
            MyLogger.print("disconnessione in corso...");

            waitForAck(commCmdAckFromServer);

            MyLogger.print("disconnessione effettuata\n");

            logged = false;
        }

        internal void syncDeletedFile(RecordFile rf)
        {
            MyLogger.debug("deleting file: " + rf.nameAndPath);
            sendToServer(commDeleteFile);
            waitForAck(commCmdAckFromServer);

            sendToServer(rf.toSendFormat());
            waitForAck(commInfoAckFromServer);
            MyLogger.debug("deleted\n");
        }

        internal void syncUpdatedFile(RecordFile rf)
        {
            MyLogger.debug("updating file: " + rf.nameAndPath);
            sendToServer(commUpdFile);
            waitForAck(commCmdAckFromServer);
            SendWholeFileToServer(rf);
            MyLogger.debug("updated");
        }

        internal void syncNewFiles(RecordFile rf)
        {
            MyLogger.debug("newing file: " + rf.nameAndPath);
            sendToServer(commNewFile);
            waitForAck(commCmdAckFromServer);

            SendWholeFileToServer(rf);
            MyLogger.debug("newed");
        }


        public RecoverInfos askForRecoverInfo()
        {
            sendToServer(commRecoverInfo);
            waitForAck(commCmdAckFromServer);
            RecoverInfos ris = new RecoverInfos();
            try
            {
                //leggi numero di versioni
                int numVers = Convert.ToInt32(readline());

                int nFile;
                //per ogni versione
                for (int bv=0; bv<numVers; bv++)
                {
                    nFile = Convert.ToInt32(readline());
                    //per ogni file
                    for (int f = 0; f < nFile; f++)
                    {
                        // [Percorso completo]\r\n[Ultima modifica -> 8byte][Hash -> 32char]\r\n
                        ris.addRawRecord(readline() + readline(), bv);
                    }
                }
                
            }
            catch (Exception)
            {
                //TODO: gestire errori in readline, in addRawRecord ecc...
                throw;
            }
            return ris;
        }

        /// <summary>
        /// non restituisce \r\n finale
        /// </summary>
        /// <returns></returns>
        private string readline()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            byte[] buf = new byte[1];
            char c = 'a';
            bool Rreceived = false;
            string recString;
            while (true)
            {
                serverStream.Read(buf, 0, buf.Length);
                c = Convert.ToChar(buf);
                if (c == '\r')
                {
                    Rreceived = true;
                    sb.Append(c);
                    continue;
                }
                //se ho ricevuto \r\n:
                if (c == '\n' && Rreceived)
                {
                    //elimina il \r gi# memorizzato in sb
                    sb.Remove(sb.Length - 1, 1);
                    break; 
                }
            }
            return sb.ToString();
        }


        private void sendFileContent(RecordFile f)
        {
            byte[] file = File.ReadAllBytes(f.nameAndPath);
            //byte[] fileBuffer = new byte[file.Length];
            //serverStream.Write(file.ToArray(), 0, fileBuffer.GetLength(0));
            serverStream.Write(file.ToArray(), 0, file.Length); //TODO:gestire casi di errore, tra cui impossibile aprire il file ecc...
        }

        public void askForSingleFile(RecoverRecord rr)
        {
            MyLogger.print("recovering file: " + rr.rf.nameAndPath);
            sendToServer(commRecoverFile);
            waitForAck(commCmdAckFromServer);

            try
            {
                RecFileContent(rr);
            }
            catch (CancelFileRequestException c)
            {
                return;
            }

            //TODO?: eliminare da recoverRecords. da recoverEntryList è già eliminato.
            MyLogger.print("received\n");
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

            }
        }

        private void RecFileContent(RecoverRecord rr)
        {
            FileStream fout;
            try
            {
                fout = File.Open(rr.rf.nameAndPath, FileMode.CreateNew);
            }
            catch(IOException e)
            {
                //file omonimo esiste già, oppure directory not found. apro una dialog di salvataggio
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.FileName = rr.rf.nameAndPath;
                Nullable<bool> result = sfd.ShowDialog();
                if (result == true)
                {
                   fout = File.Open(sfd.FileName, FileMode.CreateNew);
                }
                else
                {
                    //annullo richiesta recupero di questo file
                    throw new CancelFileRequestException();
                }
            }

            //invio al server della richiesta per il file specifico.
            //"file.txt\r\n121\r\n"
            sendToServer(rr.rf.nameAndPath + "\r\n" + rr.backupVersion.ToString() + "\r\n");

            //ricezione e salvataggio su disco.
            var buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = serverStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fout.Write(buffer, 0, bytesRead);
            }

            fout.Close();

        }

        //TODO: implementare.
        internal void RecoverBackupVersion(int versionToRecover)
        {
            throw new NotImplementedException();
            MyLogger.print("recovering backup version: " + versionToRecover);
            sendToServer(commRecoverBackup);
            waitForAck(commCmdAckFromServer);

            //try
            //{
            //    //RecBackup(versionToRecover);
            //    //invio numero versione
            //    //ricezione di un file alla volta ma in che ordine?
            //    throw new NotImplementedException;
            //}
            //catch ()
            //{
            //    return;
            //}

            ////TODO?: eliminare da recoverRecords. da recoverEntryList è già eliminato.
            MyLogger.print(" received\n");
        }
    }

    
    class CancelFileRequestException : Exception { }

    class LoginFailedException : Exception { }

    class AckErrorException : Exception { }

    class UnknownServerResponseException : Exception { }

    class RootSetErrorException : Exception { }
}


//TODO:test all SessionManager

//TODO?: quando usare funzione server di invio (client->server) backup completo?
