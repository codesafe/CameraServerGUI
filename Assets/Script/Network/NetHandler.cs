using System.Collections.Generic;

namespace TCG
{
    public class NetHandler
    {
        class _sPacket
        {
            public int packetID;
            public byte[] data;
        }

        static readonly object _lock = new object();
        public delegate void Handler(byte[] buffer);
        List<_sPacket> recvPacketList = new List<_sPacket>();
        Dictionary<int, Handler> recvPacketHandler = new Dictionary<int, Handler>();

        public void OnRecvPacket(int packetid, byte[] buffer)
        {
            lock (_lock)
            {
                //Debug.Log("Recv packet : {0}", packetid);
                _sPacket packet = new _sPacket() { packetID = packetid, data = buffer };
                recvPacketList.Add(packet);
            }
        }

        public void RegisterRecvCallback(int packetid, Handler callback)
        {
            if(recvPacketHandler.ContainsKey(packetid) == false)
                recvPacketHandler.Add(packetid, callback);
        }

        public void UnRegisterRecvCallback(int packetid)
        {
            if (recvPacketHandler.ContainsKey(packetid))
                recvPacketHandler.Remove(packetid);
        }

        public void ClearRecvCallback()
        {
            recvPacketHandler.Clear();
        }

        public void ParsePackets()
        {
            lock (_lock)
            {
                for (int i = 0; i < recvPacketList.Count; i++)
                {
                    int packetid = recvPacketList[i].packetID;
                    if (recvPacketHandler.ContainsKey(packetid))
                    {
                        // Call packet handler func
                        recvPacketHandler[packetid](recvPacketList[i].data);
                    }
                    else
                    {
                        //Log.LogError("--> No Handler Error!! : {0}", packetid);
                    }
                }

                recvPacketList.Clear();
            }
        }
    }
}