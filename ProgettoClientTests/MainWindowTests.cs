using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProgettoClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient.Tests
{
    [TestClass()]
    public class MainWindowTests
    {
        public void test0()
        {
            FileStream fout;
            string path = "C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test";
            string name = "alreadyopened.txt";
            fout = File.Open(path + "\\" + name, FileMode.Create);
            byte[] toBytes = Encoding.ASCII.GetBytes("abc");
            fout.Write(toBytes, 0, toBytes.Count());
        }
    }
}