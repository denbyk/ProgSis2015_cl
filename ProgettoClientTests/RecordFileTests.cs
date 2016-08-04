using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProgettoClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient.Tests
{
    [TestClass()]
    public class RecordFileTests
    {

        [TestMethod()]
        public void toSendFormatTest()
        {
            RecordFile rf = new RecordFile("C:\\ciao.txt", "abcdefghabcdefghabcdefghabcdefgh", 15, new DateTime(1970, 1, 1, 1, 0, 0));
            string s = rf.toSendFormat();
            string rs = "C:\\ciao.txt\r\n00000000000Fabcdefghabcdefghabcdefghabcdefgh0000000000000000\r\n";
            Assert.AreEqual(s, rs);
        }

        [TestMethod()]
        public void getJustNameTest()
        {
            RecordFile rf = new RecordFile("C:\\cartella\\prova\\ciao.txt", "abcdefghabcdefghabcdefghabcdefgh", 15, new DateTime(1970, 1, 1, 1, 0, 0));
            Assert.AreEqual("ciao.txt", rf.getJustName());
        }
    }

}