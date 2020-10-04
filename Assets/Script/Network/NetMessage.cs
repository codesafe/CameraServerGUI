#if (false)

using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;
using UnityEngine;


namespace TCG
{

    public class NetMessage
    {
        public int senderId;
        public int targetObjID;
        public string command;
        public string[] data;

        public NetMessage(MonoObject obj)
        {
            senderId = GlobalValue.playerID;
            targetObjID = obj.GetObjectId();
        }

        public string GetString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}

#endif