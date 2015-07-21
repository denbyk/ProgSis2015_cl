using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ProgettoClient
{
    static class MyLogger
    {
        public static void add(string message)
        {
            Trace.WriteLine(message);
            Trace.Flush();
        }

        public static void add(Object o)
        {
            MyLogger.add(o.ToString());
        }

    }
}
