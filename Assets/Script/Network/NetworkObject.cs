//#define USE_TCP_METHOD_1
#define USE_TCP_METHOD_2

using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TCG
{
    public class SocketBuffer
    {
        private const int READ_BUFFER_SIZE = 1024 * 4;
        public int packetsize;
        public int currentsize;
        public byte[] buffer = new byte[READ_BUFFER_SIZE];

        public SocketBuffer()
        {
            reset();
        }

        public void reset()
        {
            packetsize = 0;
            currentsize = 0;
        }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public char signature;      // 서버가 보낸것?(SERVER_SIG) 클라이언트가 보낸것? (CLIENT_SIG)
        public int packetsize;      // packet size는 header + data 포함한 전체 길이
        public int packetserial;    // 생성 번호 = wCommandID^dwSize+index(패키지당 자동 성장 색인); 환원 번호 = pHeader->dwPacketNo - pHeader->wCommandID^pHeader->dwSize;
        public int packetID;        // msg ID
    };

    /////////////////////////////////////////////////////////////////////////////////////////////


#if USE_TCP_METHOD_1

public class TcpNetwork
{
    private Socket socket;
    private const int READ_BUFFER_SIZE = 1024 * 4;

    public Action<PacketHeader, byte[]> OnReceived;
    public Action<bool> OnConnect;
    public Action OnDisconnect;
    public Action<Exception> OnError;

    private byte[] buffer = new byte[READ_BUFFER_SIZE];

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public TcpNetwork()
    {
    }

    public bool IsConnected
    {
        get
        {
            if (socket == null) return false;
            else if (socket.Poll(0, SelectMode.SelectRead) && socket.Available.Equals(0))
            {
                Disconnect(true);
                return false;
            }
            else return true;
        }
    }

    private IPAddress GetIP(string IPString)
    {
        if (!IPAddress.TryParse(IPString, out IPAddress IP))
            throw new ArgumentException("Invalid IPv4/IPv6 address.");
        return IP;
    }

    public bool Connect(string serverip, int port, TimeSpan timeout)
    {
        IPAddress IP = GetIP(serverip);
        socket = new Socket(IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.NoDelay = true;

        EndPoint ep = new IPEndPoint(IPAddress.Parse(serverip), port);
        socket.Connect(ep);

        try
        {
            socket.BeginConnect(IP, port, null, null).AsyncWaitHandle.WaitOne(timeout);
        }
        catch
        {
            socket = null;
            return false;
        }

        if (socket.Connected)
        {
            buffer = new byte[2];
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceiveLength, socket);
            return true;
        }
        else
        {
            socket = null;
            return false;
        }
    }

    public void Disconnect(bool notifyOnDisconnect = false)
    {
        if (socket == null) return;

        try
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket = null;
        }
        catch (Exception ex)
        {
            NotifyOnError(ex);
        }

        if (notifyOnDisconnect)
            OnDisconnect?.Invoke(this, this);
    }

    public void Send(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException("Could not send data: Data is null.");
        else if (socket == null)
            throw new Exception("Could not send data: Socket not connected.");

//         byte[] message = new byte[data.Length + 2];
//         Buffer.BlockCopy(BitConverter.GetBytes((ushort)data.Length), 0, message, 0, 2);
//         Buffer.BlockCopy(data, 0, message, 2, data.Length);

        // 이것은 기본형
        SocketAsyncEventArgs e = new SocketAsyncEventArgs();

        byte[] message = new byte[data.Length + 2];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)data.Length), 0, message, 0, 2);
        Buffer.BlockCopy(data, 0, message, 2, data.Length);
        e.SetBuffer(message, 0, message.Length);
        // Async라 결과는 항상 true
        socket.SendAsync(e); // Write async

        // Async로 보낸 결과를 받으려면 이렇게
        //sendAsyncevent.SetBuffer(message, 0, message.Length);
        //socket.SendAsync(sendAsyncevent);
    }

    private void Send_Completed(object sender, SocketAsyncEventArgs ar)
    {
        if( ar.SocketError == SocketError.Success )
        {
            return;
        }
        else
        {
            Disconnect(true);
            throw new SocketException((int)ar.SocketError);
        }
    }

    private void OnReceiveLength(IAsyncResult ar)
    {
        Socket socket = ar.AsyncState as Socket;

        try
        {
            if (socket.Poll(0, SelectMode.SelectRead) && socket.Available.Equals(0))
            {
                Disconnect(true);
                return;
            }

            ushort DataLength = BitConverter.ToUInt16(buffer, 0);

            if (DataLength <= 0 || DataLength > maxDataSize)
            {
                Disconnect(true);
                return;
            }
            else
                socket.BeginReceive(buffer = new byte[DataLength], 0, DataLength, SocketFlags.None, OnReceiveData, socket);
        }
        catch (SocketException)
        {
            Disconnect(false);
            OnDisconnect?.Invoke(this, this);
        }
        catch (Exception ex)
        {
            NotifyOnError(ex);
        }
    }

    private void OnReceiveData(IAsyncResult ar)
    {
        Socket socket = ar.AsyncState as Socket;
        try
        {
            if (socket.Poll(0, SelectMode.SelectRead) && socket.Available.Equals(0))
            {
                Disconnect(true);
                return;
            }

            //DataReceived?.Invoke(this, new Message(buffer, socket, Encryption, encoding));//Trigger event

            if (_messageDelegate != null)
            {
                //string rcvstring = encoding.GetString(buffer);
                _messageDelegate(buffer);
            }
            socket.BeginReceive(buffer = new byte[2], 0, buffer.Length, SocketFlags.None, OnReceiveLength, socket);
        }
        catch (SocketException)
        {
            Disconnect(true);
            return;
        }
        catch (Exception ex)
        {
            NotifyOnError(ex);
        }
    }

    private void NotifyOnError(Exception ex)
    {
        if (OnError != null)
            OnError(this, ex);
        else throw ex;
    }
}

#elif USE_TCP_METHOD_2


    public class TcpNetwork
    {
        private Socket socket;
        private const int READ_BUFFER_SIZE = 1024 * 4;

        public Action<PacketHeader, byte[]> OnReceived;
        public Action<bool> OnConnect;
        public Action OnDisconnect;
        public Action<Exception> OnError;

        private byte[] buffer = new byte[READ_BUFFER_SIZE];

        private SocketBuffer socketbuffer = new SocketBuffer();
        private Encoding encoding = Encoding.UTF8;

        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private bool isConnected = false;

        private AsyncCallback recvHandler;
        private AsyncCallback sendHandler;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public TcpNetwork()
        {
        }

        public void Start()
        {
            Init();
        }

        public void Stop()
        {
            Disconnect(false);
        }

        /*
            public bool IsConnected
            {
                get
                {
                    if (socket == null) return false;
                    else if (socket.Poll(0, SelectMode.SelectRead) && socket.Available.Equals(0))
                    {
                        Disconnect(true);
                        return false;
                    }
                    else return true;
                }
            }
        */

        private IPAddress GetIP(string IPString)
        {
            if (!IPAddress.TryParse(IPString, out IPAddress IP))
                throw new ArgumentException("Invalid IPv4/IPv6 address.");
            return IP;
        }

        void Init()
        {
            sendHandler = new AsyncCallback(SendCallback);
            recvHandler = new AsyncCallback(ReceiveCallback);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            socket.NoDelay = true;
            //socket.Blocking = false;
            socket.SendTimeout = 10;

        }

        public void Connect(string serverip, int port, TimeSpan timeout)
        {
            IPAddress IP = GetIP(serverip);
            try
            {
                socket.BeginConnect(IP, port, new AsyncCallback(ConnectCallback), null);
                //connectDone.WaitOne(timeout);
            }
            catch
            {
                socket = null;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndConnect(ar);
                //connectDone.Set();
                if (OnConnect != null)
                    OnConnect(true);

                isConnected = true;

                // Recv 시작
                socket.BeginReceive(buffer, 0, READ_BUFFER_SIZE, 0, recvHandler, null);

            }
            catch (Exception e)
            {
                if (OnConnect != null)
                    OnConnect(false);
                //Log.LogError("--> Exception {0}", e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (isConnected)
                {
                    int recvsize = socket.EndReceive(ar);
                    int buffpos = 0;
                    while (recvsize > 0)
                    {
                        int copysize = socketbuffer.currentsize + recvsize <= READ_BUFFER_SIZE ? recvsize : READ_BUFFER_SIZE - socketbuffer.currentsize;

                        Buffer.BlockCopy(buffer, buffpos, socketbuffer.buffer, socketbuffer.currentsize, copysize);
                        buffpos += copysize;

                        socketbuffer.currentsize += copysize;
                        recvsize -= copysize;
                        RecvDone();
                    }

                    socket.BeginReceive(buffer, 0, READ_BUFFER_SIZE, 0, recvHandler, null);
                }
            }
            catch (Exception e)
            {
                Disconnect(true);
            }
        }

        private void RecvDone()
        {
            while (true)
            {
                int headersize = Marshal.SizeOf(typeof(PacketHeader));

                if (socketbuffer.currentsize > headersize)
                {
//                     PacketHeader header = (PacketHeader)Utilities.BytesToStructure(socketbuffer.buffer, typeof(PacketHeader));
// 
//                     if (socketbuffer.currentsize >= header.packetsize)
//                     {
//                         byte[] buffer = new byte[header.packetsize - headersize];
//                         Buffer.BlockCopy(socketbuffer.buffer, headersize, buffer, 0, header.packetsize - headersize);
// 
//                         OnReceived?.Invoke(header, buffer);
//                         //                     if (OnReceived != null)
//                         //                         OnReceived(buffer);
// 
//                         socketbuffer.currentsize -= header.packetsize;
// 
//                         if (socketbuffer.currentsize > 0)
//                         {
//                             Buffer.BlockCopy(socketbuffer.buffer, header.packetsize, socketbuffer.buffer, 0, socketbuffer.currentsize);
//                         }
//                     }
//                     else
//                         break;

                }
                else
                    break;
            }
        }

        public void Disconnect(bool notifyOnDisconnect = false)
        {
            if (socket == null && isConnected == false) return;

            try
            {
                if (isConnected)
                    socket.Shutdown(SocketShutdown.Both);

                isConnected = false;
                socket.Close();
                socket = null;
            }
            catch (Exception ex)
            {
                NotifyOnError(ex);
            }

            if (notifyOnDisconnect)
                if (OnDisconnect != null)
                    OnDisconnect();
        }

        public void Send(string msg)
        {
            byte[] ptr = encoding.GetBytes(msg);
            Send(ptr);
        }

        public void Send(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("Could not send data: Data is null.");
            else if (socket == null)
                throw new Exception("Could not send data: Socket not connected.");

            byte[] message = new byte[data.Length + 2];
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)data.Length), 0, message, 0, 2);
            Buffer.BlockCopy(data, 0, message, 2, data.Length);
            socket.BeginSend(message, 0, message.Length, 0, sendHandler, socket);
        }

        public void SendProtoBuf(byte[] data)
        {
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, sendHandler, socket);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int bytesSent = client.EndSend(ar);
                //Debug.Log("--> Sent {0}", bytesSent);
            }
            catch (Exception e)
            {
                NotifyOnError(e);
            }
        }

        private void NotifyOnError(Exception ex)
        {
            if (OnError != null)
                OnError(ex);
            else throw ex;
        }
    }

#endif

    /////////////////////////////////////////////////////////////////////////////////////////////



    public class NetworkObject : MonoBehaviour
    {

        private static NetworkObject _instance;
        private TcpNetwork tcpclient;
        private Encoding encoding = Encoding.UTF8;

        private Dictionary<int, Action<int, byte[]>> recvPacketHandler = new Dictionary<int, Action<int, byte[]>>();

        private Action<bool> onConnected = null;

        void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            _instance = null;
        }

        void OnDisable()
        {
            tcpclient.Stop();
        }

        public static NetworkObject getInstance()
        {
            return _instance;
        }

        private void Start()
        {
            tcpclient = new TcpNetwork();
            tcpclient.OnReceived = OnReceivePacket;
            tcpclient.OnConnect = OnConnect;
            tcpclient.OnDisconnect = OnDisconnect;
            tcpclient.OnError = OnError;
            tcpclient.Start();
        }

        private void OnReceivePacket(PacketHeader header, byte[] buffer)
        {
#if (false)
        string message = encoding.GetString(buffer);
        Debug.Log("--> Recv Packet : {0} / {1}", message, buffer.Length);

        try
        {
            NetMessage msg = JsonUtility.FromJson<NetMessage>(message);
            if (msg.senderId != GlobalValue.playerID)
            {
                recvPacketlist.Add(msg);
            }
        }
        catch (Exception ex)
        {
            Log.LogError("--> Exception : {0}", (object)ex);
        }
#else
            if (recvPacketHandler.ContainsKey(header.packetID))
            {
                recvPacketHandler[header.packetID](header.packetID, buffer);
            }
            else
            {
                //Debug.LogError("--> No Handler Error!! : {0}", header.packetID);
            }
#endif
        }

        public void RegisterConnectedCallback(Action<bool> callback)
        {
            onConnected = callback;
        }

        public void UnRegisterConnectedCallback()
        {
            onConnected = null;
        }

        public void RegisterRecvCallback(int packetid, Action<int, byte[]> callback)
        {
            if (recvPacketHandler.ContainsKey(packetid) == false)
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

        private void OnConnect(bool success)
        {
            if (success)
                Debug.Log("--> Connected");
            else
                Debug.Log("--> Connect Fail");

            onConnected?.Invoke(success);
        }

        private void OnDisconnect()
        {
            Debug.Log("--> Disconnected");
        }

        private void OnError(Exception ex)
        {
            //Debug.Log("--> Exception : {0}", ex.ToString());
        }

        public void ConnectToServer()
        {
            //tcpclient.Connect(GlobalValue.serverIp, GlobalValue.port, TimeSpan.FromSeconds(2));
        }

//         public override void ManagedUpdate()
//         {
//         }


        public void SendProtoBuf(int packetID, byte[] buf)
        {
//            int headersize = Marshal.SizeOf(typeof(PacketHeader));
//             PacketHeader header = new PacketHeader();
//             header.packetID = packetID;
//             header.packetserial = 0;
//             header.signature = (char)GlobalValue.CLIENT_SIG;
//             header.packetsize = headersize + buf.Length;
// 
//             byte[] sendbuffer = new byte[header.packetsize];
// 
//             byte[] _h = Utilities.StructureToBytes(header);
//             Buffer.BlockCopy(_h, 0, sendbuffer, 0, headersize);
//             Buffer.BlockCopy(buf, 0, sendbuffer, headersize, buf.Length);

//            tcpclient.SendProtoBuf(sendbuffer);
        }

    }


}

