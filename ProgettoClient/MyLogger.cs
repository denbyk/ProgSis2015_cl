using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;

namespace ProgettoClient
{
    static class 
        MyLogger
    {
        private static MainWindow mainWindow;
        private const bool DEBUG_TEXT_ACTIVE = true;
        
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

        public static void popup(string message, MessageBoxImage mbi)
        {
            mainWindow.Dispatcher.Invoke(mainWindow.DelShowOkMsg, message, mbi);
            mainWindow.Dispatcher.BeginInvoke(mainWindow.DelWriteLog, message);
            Trace.WriteLine(message);
            Trace.Flush();
        }

        public static void print(Object o)
        {
            MyLogger.print(o.ToString());
        }

        public static void line()
        {
            MyLogger.debug("-----------------------------------------------\n");
        }

        internal static void debug(string mess)
        {
            if (!DEBUG_TEXT_ACTIVE)
                return;
            mess = "!!! " + mess;
            Trace.WriteLine(mess);
            Trace.Flush();
            mainWindow.Dispatcher.BeginInvoke(mainWindow.DelWriteLog, mess);
        }
        public static void debug(Object o)
        {
            MyLogger.debug(o.ToString());
        }
    }
}
