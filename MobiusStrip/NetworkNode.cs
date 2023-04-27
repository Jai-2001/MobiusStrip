using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;


namespace MobiusStrip;

public abstract class NetworkNode
{
    protected NetworkStream? MovementStream;
    protected TcpClient? _client;
    protected string sendBuf;
    protected string recvBuf;
    protected Vector3 recievedPos;
    protected Vector3 boxPosition;
    private MovingBox? currentBox;
    protected int LastRead;
    protected IAsyncResult? LastWrite;
    private bool requestSwap;
    private bool swapGranted;
    protected NetworkNode(string ip, int port)
    {
        sendBuf = "";
        recvBuf = "";
        LastRead = 0;
        boxPosition = Vector3.zero;
        //_swapState = 0;
    }

    public bool Paired()
    {
        return _client is { Connected: true };
    }

    public abstract void AwaitConnection();

    public bool MessageReceived()
    {
        if (_client != null)
        {
            if (_client.Available > 0)
            {
                byte[] buf = new byte[_client.Available];
                LastRead += MovementStream.Read(buf, 0, buf.Length);
                recvBuf += Encoding.UTF8.GetString(buf);
            }

            return recvBuf.Contains("{") && recvBuf.Contains("}");
        }

        return false;
    }

    public void SendMessage(Vector3 pos, bool requestSwap, string boxName, Vector3 boxPos)
    {
        requestSwap = this.requestSwap;
        if (Paired())
        {
            if (LastWrite != null)
            {
                MovementStream.EndWrite(LastWrite);
                LastWrite = null;
            }

            sendBuf = $"{{{pos.x}|{pos.y}|{pos.z}|{requestSwap}|{boxName}|{boxPos.x}|{boxPos.y}|{boxPos.z}}}";
            byte[] buf = Encoding.UTF8.GetBytes(sendBuf);
            LastWrite = MovementStream.BeginWrite(buf, 0, sendBuf.Length, null, null);
        }

    }

    public void ReceiveMessage()
    {
        Vector3 position = Vector3.zero;
        if (MessageReceived())
        {
            string next = recvBuf.Substring(recvBuf.IndexOf("{", StringComparison.Ordinal)+1, 
                recvBuf.IndexOf("}", StringComparison.Ordinal)-1);
            ModMain.Log(next);
            string[] parts = next.Split('|');
            position = GetVectorFromStrings(parts[0], parts[1], parts[2]);
            if (parts[4].Contains("/"))
            {
                currentBox = GameObject.Find(parts[4]).GetComponent<MovingBox>();
                boxPosition = GetVectorFromStrings(parts[5], parts[6], parts[7]);
            }
            else
            {
                currentBox = null;
                boxPosition = Vector3.zero;
            }
            swapGranted = requestSwap & bool.Parse(parts[3]);
            recvBuf = "";
        }
        recievedPos = position;
    }

    public static Vector3 GetVectorFromStrings(string x, string y, string z)
    {
       return new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
    }
    
    public Vector3 GetReceivedPosition()
    {
        return recievedPos;
    }
    
    public MovingBox? GetMovingBox()
    {
        return currentBox;
    }
    public Vector3 GetBoxPosition()
    {
        return boxPosition;
    }


    public bool CheckSwapGranted()
    {
        return swapGranted;
    }
}