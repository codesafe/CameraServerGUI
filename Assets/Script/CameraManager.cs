using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;



public class CameraManager : MonoBehaviour
{
    [SerializeField] CameraObj prefab;

    List<CameraObj> cameraobjList = new List<CameraObj>();
    private static CameraManager _instance;

    Vector2 pivot = new Vector2(-430, 300);
    Vector2 stride = new Vector2(100, -100);

    int cameraNum = 0;

    public static CameraManager getInstance()
    {
        return _instance;
    }

    private void Awake()
    {
        _instance = this;
    }

    void Update()
    {
        //         if (Input.GetKeyDown("space"))
        //         {
        //             AddCamera();
        //         }

        List<Socket> socketlist = new List<Socket>();
        ServerSocket.getInstance().GetAcceptedSocket(ref socketlist);
        
        for(int i=0; i<socketlist.Count; i++)
        {
            AddCamera(socketlist[i]);
        }
    }

    // 77개가 MAX
    public void AddCamera(Socket clientsocket)
    {
        CameraObj obj = Instantiate(prefab, transform);
        obj.Init(cameraNum++, clientsocket);
        cameraobjList.Add(obj);

        Refresh();
    }

    void Refresh()
    {
        int x = 0;
        int y = 0;

        for(int i=0; i<cameraobjList.Count; i++)
        {
            if( x > 10)
            {
                x = 0;
                y++;
            }
            cameraobjList[i].transform.localPosition = new Vector3(pivot.x + (stride.x * x), pivot.y + (stride.y * y), 0);
            x++;
        }
    }

    public void SendPacket(char packet, char param0, char param1, char param2)
    {
        for(int i=0; i<cameraobjList.Count; i++)
        {
            cameraobjList[i].SendPacket(packet, param0, param1, param2);
        }
    }

    public void Reset()
    {
        for (int i = 0; i < cameraobjList.Count; i++)
        {
            cameraobjList[i].Reset();
        }
    }

}
