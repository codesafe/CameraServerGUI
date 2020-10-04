using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

public class ServerSocket
{
    static private ServerSocket _instance = null;

    Thread ServerSocket_Thread = null;

    const int MAX_CLIENT = 77;
    const int TCP_BUFFER = 8;

    private Socket tcpserver;
    private Socket udpserver;

    private object lockObject = new object();

    private List<Socket> acceptedlist = new List<Socket>();

    ServerSocket()
    {
    }

    public static ServerSocket getInstance()
    {
        if (_instance == null)
            _instance = new ServerSocket();
        return _instance;
    }

    public void Start()
    {
        // create udp socket
        //         IPEndPoint ep = new IPEndPoint(IPAddress.Any, udpport);
        //         udpserver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //         udpserver.Bind(ep);

        // accept socket
        //IPAddress ipAd = IPAddress.Parse("127.0.0.1");
        //tcpListener = new TcpListener(ipAd, tcpport);
        //tcpListener.Start();

        ServerSocket_Thread = new Thread(AcceptWorker);
        ServerSocket_Thread.Start();

        //         //클라이언트의 데이터를 읽고, 쓰기 위한 스트림을 만든다.
        //         stream = new NetworkStream(clientsocket);
        //         Encoding encode = System.Text.Encoding.GetEncoding("ks_c_5601-1987");
        //         reader = new StreamReader(stream, encode);
        //         while (true)
        //         {
        // 
        //             string str = reader.ReadLine();
        //             Console.WriteLine(str);
        //         }

    }

    public void Destroy()
    {
        ServerSocket_Thread.Abort();
        ServerSocket_Thread.Join();
    }

    private void AcceptWorker()
    {
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, Predef.tcpport);
        tcpserver = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        tcpserver.Bind(ipep);
        tcpserver.Listen(10);

        while(true)
        {
            Socket clientsocket = tcpserver.Accept();
            clientsocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            clientsocket.NoDelay = true;
            clientsocket.Blocking = true;

            lock(lockObject)
            {
                acceptedlist.Add(clientsocket);
            }

            //CameraManager.getInstance().AddCamera(clientsocket);
        }

        /*
                IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
                NetworkStream recvStm = new NetworkStream(client);



                while (Socket_Thread_Flag)
                {
                    byte[] receiveBuffer = new byte[1024 * 80];
                    try
                    {
                        recvStm.Read(receiveBuffer, 0, receiveBuffer.Length);
                        string Test = Encoding.Default.GetString(receiveBuffer);
                        Debug.Log(Test);
                    }

                    catch (Exception e)
                    {
                        Socket_Thread_Flag = false;
                        client.Close();
                        SeverSocket.Close();
                        continue;
                    }

                }
        */

    }

    public void GetAcceptedSocket(ref List<Socket> socketlist)
    {
        lock(lockObject)
        {
            if( acceptedlist.Count > 0 )
            {
                socketlist = new List<Socket>(acceptedlist);
                acceptedlist.Clear();
            }
        }
    }

    public void SendPacket(char packet, int who)
    {
//         if(clientlist[who].camnum != -1 && clientlist[who].socket != null )
//         {
//             byte[] buffer = new byte[TCP_BUFFER];
//             clientlist[who].socket.Send(buffer, SocketFlags.None);
//         }
    }




}

