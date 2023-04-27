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

    public void SendMessage(Vector3 position, bool requestSwap, string boxName = "")
    {
        requestSwap = true;
        if (Paired())
        {
            if (LastWrite != null)
            {
                MovementStream.EndWrite(LastWrite);
                LastWrite = null;
            }

            sendBuf = $"{{{position.x} {position.y} {position.z} {requestSwap} {boxName}}}" ;

            byte[] buf = Encoding.UTF8.GetBytes(sendBuf);
            LastWrite = MovementStream.BeginWrite(buf, 0, sendBuf.Length, null, null);
        }

    }

    public Vector3 ReceiveMessage()
    {
        Vector3 position = Vector3.zero;
        if (MessageReceived())
        {
            string next = recvBuf.Substring(recvBuf.IndexOf("{", StringComparison.Ordinal)+1, 
                recvBuf.IndexOf("}", StringComparison.Ordinal)-1);
            ModMain.Log(next);
            string[] parts = next.Split(" ".ToCharArray());
            position = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            swapGranted = requestSwap & bool.Parse(parts[3]);
            recvBuf = "";
        }
        return position;
    }
    

    public bool CheckSwapGranted()
    {
        return swapGranted;
    }
}