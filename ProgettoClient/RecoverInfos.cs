using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient
{
    class RecoverInfos
    {

        HashSet<RecoverRecord> recInfos = new HashSet<RecoverRecord>();
        
        public RecoverInfos(string stream)
        {
            //fa parsing dello stream e popola recInfos di RecoverRecord eliminando duplicati
            //throw new NotImplementedException();
        }


    }

    public struct RecoverRecord
    {
        RecordFile rf;
        int backupVersion;
    }


}
