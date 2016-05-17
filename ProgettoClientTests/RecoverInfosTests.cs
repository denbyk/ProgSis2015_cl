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
    public class RecoverInfosTests
    {
        [TestMethod()]
        public void RecoverInfosTest()
        {
            RecoverInfos ris = new RecoverInfos();
            ris.addRawRecord("C:\\ciao.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 1);
            var res = ris.getRecoverUniqueList();
            RecoverRecord rr = res[0];
            Assert.AreEqual("C:\\ciao.txt", rr.rf.nameAndPath);
            Assert.AreEqual("abcdefghabcdefghabcdefghabcdefgh", rr.rf.hash);
            Assert.AreEqual(-1, rr.rf.size);
            Assert.AreEqual(new DateTime(1970, 1, 1, 1, 0, 0), rr.rf.lastModified);
        }

        [TestMethod()]
        public void getVersionSpecificRecoverListTest()
        {
            RecoverInfos ris = new RecoverInfos();
            ris.addRawRecord("C:\\ciao1.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 1);
            ris.addRawRecord("C:\\ciao2a.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 2);
            ris.addRawRecord("C:\\ciao3.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 3);
            ris.addRawRecord("C:\\ciao2b.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 2);
            var res3 = ris.getVersionSpecificRecoverList(3);
            Assert.AreEqual(res3.Count, 1);
            Assert.AreEqual(res3[0].rf.nameAndPath, "C:\\ciao3.txt");
            var res2 = ris.getVersionSpecificRecoverList(2);
            Assert.AreEqual(res2.Count, 2);
            Assert.AreEqual(res2[0].rf.nameAndPath, "C:\\ciao2a.txt");
            Assert.AreEqual(res2[1].rf.nameAndPath, "C:\\ciao2b.txt");
        }

        [TestMethod()]
        public void getRecoverUniqueListTest()
        {
            RecoverInfos ris = new RecoverInfos();
            ris.addRawRecord("C:\\ciao1.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 1);
            ris.addRawRecord("C:\\ciao2.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 2);
            ris.addRawRecord("C:\\ciao3.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 3);
            ris.addRawRecord("C:\\ciao2.txt\r\n0000000000000000abcdefghabcdefghabcdefghabcdefgh\r\n", 2);
            var res = ris.getRecoverUniqueList();
            Assert.AreEqual(res.Count, 3);
            Assert.AreEqual(res[0].rf.nameAndPath, "C:\\ciao1.txt");
            Assert.AreEqual(res[1].rf.nameAndPath, "C:\\ciao2.txt");
            Assert.AreEqual(res[2].rf.nameAndPath, "C:\\ciao3.txt");
        }
    }
}