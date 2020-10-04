using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Predef
{
    static public int defaultIso = 3;               // 100
    static public int defaultAperture = 5;          // "9";
    static public int defaultShutterSpeed = 36;     //"1/100";

    static public int Iso = defaultIso;
    static public int Aperture = defaultAperture;
    static public int ShutterSpeed = defaultShutterSpeed;

    static public int tcpport = 8888;
    static public int udpport = 9999;

    static public int TCP_BUFFER = 8;
    static public int UDP_BUFFER = 8;



    // Packet
    static public char PACKET_TRY_CONNECT = (char)0x05;     // connect to server
    static public char PACKET_SHOT = (char)0x10;        	// shot picture
    static public char PACKET_HALFPRESS = (char)0x20;   	// auto focus
    static public char PACKET_HALFRELEASE = (char)0x21; 	// auto focus cancel

    static public char PACKET_ISO = (char)0x31;
    static public char PACKET_APERTURE = (char)0x32;
    static public char PACKET_SHUTTERSPEED = (char)0x33;

    static public char PACKET_FORCE_UPLOAD = (char)0x40;
    static public char PACKET_UPLOAD_PROGRESS = (char)0x41;
    static public char PACKET_UPLOAD_DONE = (char)0x42;

    static public char PACKET_AUTOFOCUS_RESULT = (char)0x50;

    static public char RESPONSE_OK = (char)0x05;
    static public char RESPONSE_FAIL = (char)0x06;
}

