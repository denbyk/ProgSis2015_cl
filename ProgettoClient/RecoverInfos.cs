using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient
{
    class RecoverInfos
    {
        private struct RecoverRecord
        {
            RecordFile rf;
            int backupVersion;
        }

        HashSet<RecoverRecord> recInfos = new HashSet<RecoverRecord>();
        private string stream;
        
        public RecoverInfos(string stream)
        {
            
        }
    }


}
