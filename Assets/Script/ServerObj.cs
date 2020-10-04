using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerObj : MonoBehaviour
{
    private static ServerObj _instance;
    [SerializeField] Button btnAutoFocus;
    [SerializeField] Button btnCapture;

    [SerializeField] Dropdown iso;
    [SerializeField] Dropdown shutterspeed;
    [SerializeField] Dropdown aperture;

    [SerializeField] RawImage previewImage;

    public static ServerObj getInstance()
    {
        return _instance;
    }

    private void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        ServerSocket.getInstance().Start();
        InitOption();
    }

    private void OnDestroy()
    {
        ServerSocket.getInstance().Destroy();
    }


    void InitOption()
    {
        string[] isoString = {"Auto", "100", "200", "400", "800", "1600", "3200", "6400" };
        string[] shutterspeedString = 
            { "bulb", "30", "25", "20", "15", "13", "10", "8", "6", "5", "4", "3.2", "2.5", "2", "1.6", 
                "1.3", "1", "0.8", "0.6", "0.5", "0.4", "0.3", "1/4", "1/5", "1/6", "1/8", "1/10", "1/13", 
                "1/15", "1/20", "1/25", "1/30", "1/40", "1/50", "1/60", "1/80", "1/100", "1/125", "1/160", 
                "1/200", "1/250", "1/320", "1/400", "1/500", "1/640", "1/800", "1/1000", "1/1250", "1/1600",
                "1/2000", "1/2500", "1/3200", "1/4000"};
        string[] apertureString = { "5", "5.6", "6.3", "7.1", "8", "9", "10", "11", "13", "14",  "16", "18", "20", "22", "25", "29", "32" };

        iso.options.Clear();
        for (int i=0; i< isoString.Length; i++)
        {
            Dropdown.OptionData option = new Dropdown.OptionData();
            option.text = isoString[i];
            iso.options.Add(option);
        }
        iso.value = 1;
        iso.onValueChanged.AddListener(OnISOValueChanged);

        shutterspeed.options.Clear();
        for (int i = 0; i < shutterspeedString.Length; i++)
        {
            Dropdown.OptionData option = new Dropdown.OptionData();
            option.text = shutterspeedString[i];
            shutterspeed.options.Add(option);
        }
        shutterspeed.value = 36;
        shutterspeed.onValueChanged.AddListener(OnShuutterSpeedValueChanged);

        aperture.options.Clear();
        for (int i = 0; i < apertureString.Length; i++)
        {
            Dropdown.OptionData option = new Dropdown.OptionData();
            option.text = apertureString[i];
            aperture.options.Add(option);
        }
        aperture.value = 6;
        aperture.onValueChanged.AddListener(OnApertureValueChanged);

    }

    public void onClickReset()
    {
        CameraManager.getInstance().Reset();
    }

    public void onClickAutoFocus()
    {
        CameraManager.getInstance().SendPacket(Predef.PACKET_HALFPRESS);
        Debug.Log("Auto Focus!");
    }

    public void onClickCapture()
    {
        CameraManager.getInstance().SendPacket(Predef.PACKET_SHOT);
        Debug.Log("Shot!");
    }

    public void OnISOValueChanged(int value)
    {
        Debug.Log(value);
    }

    public void OnShuutterSpeedValueChanged(int value)
    {
        Debug.Log(value);
    }

    public void OnApertureValueChanged(int value)
    {
        Debug.Log(value);
    }

    public void OnHover(CameraObj camobj)
    {
//         if (camobj != null)
//             Debug.Log(string.Format("Hover Cam {0}", camobj.cameranum));
// 
//         string path = string.Format("E:/ftp/name-{0}.jpg", camobj.cameranum);
//         StartCoroutine(load_image_preview(path));
    }

    private IEnumerator load_image_preview(string _path)
    {
        WWW www = new WWW(_path);
        yield return www;
        Texture2D texTmp = new Texture2D(128, 128, TextureFormat.RGB24, false);

        www.LoadImageIntoTexture(texTmp);
        //cur_image_loaded = new Texture2D(256, 256, TextureFormat.RGB24, false);
        //cur_image_loaded = texTmp;
        previewImage.texture = texTmp;
    }


}
