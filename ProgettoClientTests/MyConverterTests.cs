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
    public class MyConverterTests
    {
        [TestMethod()]
        public void toFixedLengthByteArrayTest()
        {
            long x = -1;
            byte[] y = MyConverter.toFixedLengthByteArray(x);
            Assert.AreEqual(x.ToString(), y.ToString());
        }
    }
}