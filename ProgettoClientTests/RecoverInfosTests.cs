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
            var res = ris.getRecoverList();
            RecoverRecord rr = res[0];
            Assert.AreEqual("C:\\ciao.txt", rr.rf.nameAndPath);
            Assert.AreEqual("abcdefghabcdefghabcdefghabcdefgh", rr.rf.hash);
            Assert.AreEqual(-1, rr.rf.size);
            Assert.AreEqual(new DateTime(1970, 1, 1, 1, 0, 0), rr.rf.lastModified);
        }

    }
}