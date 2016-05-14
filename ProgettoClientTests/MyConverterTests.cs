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
        public void extractNameAndFolderTest()
        {
            string path = "C:\\dir1\\dir 2\\nome.txt";
            string[] res = MyConverter.extractNameAndFolder(path);
            Assert.AreEqual("C:\\dir1\\dir 2\\", res[0]);
            Assert.AreEqual("nome.txt", res[1]);
        }
    }
}