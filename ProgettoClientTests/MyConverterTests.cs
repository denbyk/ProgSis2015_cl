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
    public class MyConverterTests
    {
        [TestMethod()]
        public void extractNameAndFolderTest()
        {
            //TOREMOVE
            FileStream fout;
            string path = "C:\\DATI\\poli\\Programmazione di Sistema\\progetto_client\\cartella_test";
            string name = "alreadyopened.txt";
            byte[] toBytes = Encoding.ASCII.GetBytes("abc");
            fout = File.Open(path + "\\" + name, FileMode.Create);
            //unauthorizedaccessexception
            //IOException
            fout.Write(toBytes, 0, toBytes.Count());
            return;
            //

            string path2 = "C:\\dir1\\dir 2\\nome.txt";
            string[] res = MyConverter.extractNameAndFolder(path2);
            Assert.AreEqual("C:\\dir1\\dir 2\\", res[0]);
            Assert.AreEqual("nome.txt", res[1]);
        }
    }
}