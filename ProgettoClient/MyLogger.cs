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


        public static void add(string message)
        {
            Trace.WriteLine(message);
            Trace.Flush();
            mainWindow.Dispatcher.BeginInvoke(mainWindow.DelWriteLog, message);
        }

        public static void add(Object o)
        {
            MyLogger.add(o.ToString());
        }

        public static void line()
        {
            MyLogger.add("-----------------------------------------------\n");
        }


    }
}
