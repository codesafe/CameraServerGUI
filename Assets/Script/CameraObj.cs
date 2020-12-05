using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Client
{
    private int camnum;
    private Socket socket;
    private Thread Socket_Thread = null;
    private bool loop = true;

    private object lockObject = new object();
    public List<byte[]> packetList = new List<byte[]>();

    public Client(int _camnum, Socket sock)
    {
        camnum = _camnum;
        socket = sock;

        Socket_Thread = new Thread(ReadWorker);
        Socket_Thread.Start();
    }

    private void ReadWorker()
    {
        //IPEndPoint clientep = (IPEndPoint)socket.RemoteEndPoint;
        //NetworkStream recvStm = new NetworkStream(socket);

        // 카메라 번호를 알려줌
        byte[] sendBuf = new byte[Predef.TCP_BUFFER];
        sendBuf[0] = Convert.ToByte(camnum);
        socket.Send(sendBuf, Predef.TCP_BUFFER, SocketFlags.None);

        while (loop)
        {
            try
            {
                byte[] receiveBuffer = new byte[Predef.TCP_BUFFER];

                //int recvn = recvStm.Read(receiveBuffer, 0, Predef.TCP_BUFFER);
                int recvn = socket.Receive(receiveBuffer, 0, Predef.TCP_BUFFER, SocketFlags.None);

                if (recvn == 0)
                {
                    Debug.Log("Close Socket");
                    socket.Close();
                    loop = false;
                    continue;
                }

                lock (lockObject)
                {
                    Debug.Log("Recv Packet");
                    packetList.Add(receiveBuffer);
                }
                //string Test = Encoding.Default.GetString(receiveBuffer);
                //Debug.Log(Test);
            }

            catch (Exception e)
            {
                loop = false;
                socket.Close();
                continue;
            }
        }
    }

    public void Destroy()
    {
        loop = false;
        socket.Close();
        Socket_Thread.Abort();
        Socket_Thread.Join();
    }

    public byte [] GetRecvPacket()
    {
        byte[] packet = null;

        lock (lockObject)
        {
            if (packetList.Count > 0)
            {
                packet = packetList[0];
                packetList.RemoveAt(0);
            }
        }

        return packet;
    }

}

// 일단 Client Disconnect은 생각하지 않는다
public class CameraObj : MonoBehaviour
{
    [SerializeField] Sprite normal;
    [SerializeField] Sprite normal_gray;

    [SerializeField] Image bg;
    [SerializeField] Image icon;
    [SerializeField] Image progress;
    [SerializeField] Text id;
    [SerializeField] RawImage preview;

    private Vector2 progressSize;
    private Vector2 previewSize;
    private Client clientNetThread = null;
    public int cameranum;

    private string ipAddress;
    private Socket udpSocket;
    private IPEndPoint ipep;

    void Start()
    {
        SetFocused(false);
        //id.text = "";
        progressSize = progress.rectTransform.sizeDelta;
        previewSize = preview.rectTransform.sizeDelta;
        preview.enabled = false;

        EventTrigger trigger = GetComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerEnter;
        pointerDown.callback.AddListener((e) => ServerObj.getInstance().OnHover(this));
        trigger.triggers.Add(pointerDown);
    }

    private void OnDestroy()
    {
        if (clientNetThread != null)
            clientNetThread.Destroy();
    }

    public void Init(int camnum, Socket clientsocket)
    {
        cameranum = camnum;
        id.text = camnum.ToString();
        clientNetThread = new Client(camnum, clientsocket);


        string address = clientsocket.RemoteEndPoint.ToString();
        string[] array = address.Split(new char[] { ':' });
        ipAddress = array[0];

        ipep = new IPEndPoint(IPAddress.Parse(ipAddress), Predef.udpport + camnum);
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    public void SetDownloadProgress(int percent)
    {
        if( percent == 0 )
        {
            progress.gameObject.SetActive(false);
        }
        else
        {
            progress.gameObject.SetActive(true);
            Single width = progressSize.x * ((percent * 10.0f) / 100.0f);
            progress.rectTransform.sizeDelta = new Vector2(width, progressSize.y);
        }
    }

    public void SetFocused(bool focused)
    {
        icon.sprite = focused ? normal : normal_gray;
    }

    private void Update()
    {
        // 여기에서 받은 Packet Parse
        if(clientNetThread != null)
        {
            byte[] packet = clientNetThread.GetRecvPacket();
            if (packet != null)
            {
                Debug.Log(string.Format("Camera num {0} Recv packet\n", cameranum));

                if(packet[0] == Predef.PACKET_AUTOFOCUS_RESULT)
                {
                    if(packet[1] == Predef.RESPONSE_OK)
                        SetFocused(true);
                    else
                    {
                        Debug.Log(string.Format("Camera {0} Error.", cameranum));
                        SetFocused(false);
                    }
                }
                else if (packet[0] == Predef.PACKET_UPLOAD_PROGRESS)
                {
                    progress.enabled = true;
                    progress.color = new Color32(160, 0, 255, 255);

                    char p = Convert.ToChar(packet[1]);
                    SetDownloadProgress(p);
                }
                else if (packet[0] == Predef.PACKET_UPLOAD_DONE)
                {
                    //progress.enabled = false;
                    SetDownloadProgress(10);
                    progress.color = new Color32(0, 255, 0, 255);
                    ShowPreview();
                }
            }

        }
    }

    public void SendPacket(char packet)
    {
        byte[] data = new byte[Predef.UDP_BUFFER];
        data[0] = Convert.ToByte(packet);
        udpSocket.SendTo(data, Predef.UDP_BUFFER, SocketFlags.None, ipep);

        if (packet == Predef.PACKET_HALFPRESS)
            SetFocused(false);
    }

    void ShowPreview()
    {
        preview.texture = null;
        string path = string.Format("E:/ftp/name-{0}.jpg", cameranum);
        StartCoroutine(load_image_preview(path));
    }

    public void OnClick()
    {
//         Vector2 screenpos = RectTransformUtility.WorldToScreenPoint(null, preview.transform.position);
// 
//         float fixx = 0;
//         float fixy = 0;
//         if ( screenpos.y + (previewSize.y/2) >= Screen.height )
//         {
//             fixy = -previewSize.y + (Screen.height - screenpos.y);
//         }
// 
//         if (screenpos.x + (previewSize.x / 2) >= Screen.width)
//         {
//             fixx = -previewSize.x + (Screen.width - screenpos.x);
//         }
//         preview.transform.localPosition = new Vector3(fixx, fixy, 0);
// 
//         preview.enabled = true;
//         string path = string.Format("E:/ftp/name-{0}.jpg", cameranum);
//         StartCoroutine(load_image_preview(path));
    }

    private IEnumerator load_image_preview(string _path)
    {
        yield return new WaitForSeconds(1.0f);
        WWW www = new WWW(_path);
        yield return www;
        Texture2D texTmp = new Texture2D(100, 100, TextureFormat.RGB24, false);

        www.LoadImageIntoTexture(texTmp);
        preview.texture = texTmp;
        preview.enabled = true;
    }


    public void OnHoverout()
    {
//        preview.enabled = false;
    }

    public void Reset()
    {
        preview.enabled = false;
        progress.enabled = false;
        SetFocused(false);
    }
}
