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
        public void TODOTODELETETest()
        {
            Assert.AreEqual(4, RecordFile.TODOTODELETE(2));
        }

        [TestMethod()]
        public void toSendFormatTest()
        {
            byte[] hash = { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10,
                             0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10};
            var rf = new RecordFile("C:\\ciao.txt", hash, 10, new DateTime(10, 9, 8, 7, 6, 5));
            Assert.Fail();
        }

        //[TestMethod()]
        //public void toSendFormatTest1()
        //{
        //    RecordFile rf = new RecordFile("C:\\ciao.txt", null, 1, new DateTime(1970, 1, 1, 0, 0, 0));
        //    Assert.Equals(rf.toSendFormat(), "");
        //}
    }
}