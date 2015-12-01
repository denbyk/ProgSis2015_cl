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
        private const string commloggedok = "LOGGEDOK";
        private const string commloginerr = "LOGINERR";

        private string serverIP;
        private int serverPort;
        private byte[] user;
        private byte[] hashPassword;
        private byte[] separator_r_n;
        
        private UTF8Encoding utf8;

        private System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        private NetworkStream serverStream;

        private Dictionary<byte[], commandsEnum> commands;

        public SessionManager(string serverIP)
        {
            utf8 = new UTF8Encoding();
            this.serverIP = serverIP;

            separator_r_n = utf8.GetBytes("\r\n");
            //commands = new Dictionary<byte[],commandsEnum>();
            //commands.Add(utf8.GetBytes(commLogin_str), commandsEnum.login);
        }

        public void login(string user, string password)
        {
            //string -> utf8
            this.user = utf8.GetBytes(user);

            SHA256 mySha256 = SHA256Managed.Create();
            byte[] utf8psw = utf8.GetBytes(password);
            //hash(utf8(psw))
            this.hashPassword = mySha256.ComputeHash(utf8psw, 0, utf8psw.Length);

            if (!clientSocket.Connected)
                newConnection();
            
            sendToServer(commLogin_str);
            
            //invio "[username]\r\n[sha-256_password]\r\n"
            byte[] userPassword = ConcatByte(this.user, separator_r_n);
            userPassword = ConcatByte(userPassword, this.hashPassword);
            userPassword = ConcatByte(userPassword, this.separator_r_n);
            sendToServer(userPassword);
            switch (strRecFromServer())
            {
                case commloggedok:
                    
                    break;
                case commloginerr:
                    //create_ac?
                    break;
            }
            
        }

        private void newConnection()
        {
            try
            {
                clientSocket.Connect(serverIP, serverPort);
                serverStream = clientSocket.GetStream();
            }
            catch()
            {
                //TODO
            }
        }

        private string strRecFromServer() 
        {
            return utf8.GetString(receiveFromServer());
        }

        private byte[] receiveFromServer()
        {
            byte[] res = new byte[commLength];
            serverStream.Read(res, 0, res.Length);
            return res;
        }


        private void sendToServer(byte[] toSend)
        {
            serverStream.Write(toSend, 0, toSend.Length);
            serverStream.Flush();

            //TODO: dopo un comando devo aspettare un qualche ack dal server?
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
    }
}
