using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Threading;

namespace ProgettoClient
{
    static class 
        MyLogger
    {
        private static MainWindow mainWindow;
        
        internal static void init(MainWindow mainWindowp)
        {
            MyLogger.mainWindow = mainWindowp;
        }


        public static void print(string message)
        {
            Trace.WriteLine(message);
            Trace.Flush();
            mainWindow.Dispatcher.BeginInvoke(mainWindow.DelWriteLog, message);
        }

        public static void print(Object o)
        {
            MyLogger.print(o.ToString());
        }

        public static void line()
        {
            MyLogger.print("-----------------------------------------------\n");
        }

        internal static void debug(string v)
        {
            throw new NotImplementedException();
        }
    }
}
