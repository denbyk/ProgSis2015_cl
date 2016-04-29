using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Sockets;

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
        private const int commLength = 8;
        private const string commLogin_str = "LOGIN___";
        private const string commLogout_str = "LOGOUT__";
        private const string commSetFold_str = "SET_FOLD";
        private const string commClrFolder_str = "SET_FOLD";
        private const string commDeleteFile = "DEL_FILE";
        private const string commNewFile = "NEW_FILE";
        private const string commUpdFile = "UPD_FILE";

        private const string commFolderOk = "FOLDEROK";
        private const string commloggedok = "LOGGEDOK";
        private const string commloginerr = "LOGINERR";
        private const string commCmdAckFromServer = "CMND_REC";
        private const string commInfoAckFromServer = "INFO_OK_";
        private const string commDataAckFromServer = "DATA_OK_";
        

        //todo: inserire commDBError

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

        private Dictionary<byte[], commandsEnum> commands;

        private MainWindow mainWindow;

        public SessionManager(string serverIP, int serverPort, MainWindow mainWindow)
        {
            this.serverIP = serverIP;
            this.serverPort = serverPort;
            this.mainWindow = mainWindow;

            utf8 = new UTF8Encoding();
            separator_r_n = utf8.GetBytes("\r\n");
        }

        public void setRootFolder(string rootFolder)
        {
            this.rootFolder = utf8.GetBytes(rootFolder);
            sendToServer(commSetFold_str);
            sendToServer(this.rootFolder);
            if (strRecFromServer().Equals(commFolderOk)) //dovrebbe ricevere sempre FOLDEROK
            {
                MyLogger.add("cartella selezionata correttamente.\n");
            }
            else
            {
                throw new UnknownServerResponseException();
            }
        }

        public void clearRootFolder()
        {
            sendToServer(commClrFolder_str);
            if (strRecFromServer().Equals(commFolderOk)) 
            { 
                this.rootFolder = null;
            }
            else
            {
                throw new UnknownServerResponseException();
            }
        }

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
            switch (commloggedok)//strRecFromServer()) TODO:ripristinare chiamata
            {
                case commloggedok:
                    logged = true;
                    break;
                case commloginerr:
                    //create_ac?
                    MyLogger.add("errore nel login\n");
                    bool wantNewAcc = (bool) mainWindow.Dispatcher.Invoke(mainWindow.DelAskNewAccount);
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
            try
            {
                clientSocket.Connect(serverIP, serverPort);
                serverStream = clientSocket.GetStream();
            }
            catch(SocketException)
            {
                MyLogger.add("Collegamento al server fallito");
                throw;
            }
        }

        private string strRecFromServer() //TODO: idea: se qui controllo e se il server mi ha inviato DB_ERROR lanciassi una eccezione?
        {
            return utf8.GetString(receiveFromServer());
        }

        private byte[] receiveFromServer()
        {
            byte[] res = new byte[commLength];
            serverStream.Read(res, 0, res.Length); //TODO: e se la connessione si interrompe?
            return res;
        }


        private void sendToServer(byte[] toSend)
        {
            serverStream.Write(toSend, 0, toSend.Length);
            serverStream.Flush();
        }

        private void waitForAck(string ackExpected)
        {
            string ack = strRecFromServer();
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
            MyLogger.add("disconnessione in corso...");

            waitForAck(commCmdAckFromServer);

            MyLogger.add("disconnessione effettuata\n");

            logged = false;
        
        }


        internal void syncDeletedFile(RecordFile rf) 
        {
            sendToServer(commDeleteFile);
            waitForAck(commCmdAckFromServer);

            sendToServer(rf.toSendFormat());
            waitForAck(commInfoAckFromServer);

            //throw new NotImplementedException();
        }

        internal void syncUpdatedFile(RecordFile rf)
        {
            sendToServer(commUpdFile);
            waitForAck(commCmdAckFromServer);
            SendWholeFileToServer(rf);
        }

        internal void syncNewFiles(RecordFile rf)
        {
            sendToServer(commNewFile);
            waitForAck(commCmdAckFromServer);

            SendWholeFileToServer(rf);
        }

        private void SendWholeFileToServer(RecordFile rf)
        {
            int tryes = 0;
            while (tryes < 6) { 
                //invio info del file
                sendToServer(rf.toSendFormat());
                waitForAck(commInfoAckFromServer); //TODO: da controllare che non esca un SNDAGAIN già qui.
                
                //invio contenuto del file
                sendFileContent(rf);

                /////DA CONTROLLARE, INTERROTTO QUI
                if (strRecFromServer() == "SNDAGAIN") {
                    tryes++;
                    continue;
                }

                    
            }

        }

        private void sendFileContent(RecordFile f)
        {
            throw new NotImplementedException();
        }
    }

    class LoginFailedException : Exception
    {
    }

    class AckErrorException : Exception
    { }

    class UnknownServerResponseException : Exception
    { }
}


//TODO:test all SessionManager