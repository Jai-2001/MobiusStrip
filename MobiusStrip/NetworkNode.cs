using System;
using System.Net.Sockets;
using UnityEngine;

namespace MobiusStrip;

public abstract class NetworkNode
{
    protected NetworkStream? MovementStream;
    protected TcpClient? _client;
    protected byte[] sendBuf;
    protected byte[] recvBuf;
    protected int LastRead;
    protected IAsyncResult? LastWrite;
    private byte _swapState;
    //0 is none,
    //1 is send request,
    //2 is send granted,
    //3 is received granted,
    //4 is received declined
    private bool accepted;
    private bool granted;
    protected NetworkNode(string ip, int port)
    {
        sendBuf = new byte[13];
        recvBuf = new byte[13];
        LastRead = 0;
        _swapState = 0;
    }

    public bool Paired()
    {
        return _client is { Connected: true };
    }

    public abstract void AwaitConnection();

    public bool MessageReceived()
    {
        return _client is { Available: >= 13 };
    }

    public void SendMessage(Vector3 position, bool requestSwap)
    {
        if (Paired())
        {
            if (requestSwap) _swapState = 1;
            if (LastWrite != null)
            {
                MovementStream.EndWrite(LastWrite);
                LastWrite = null;
            }
            Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, sendBuf, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, sendBuf, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, sendBuf, 8, 4);

            sendBuf[12] = _swapState;
            LastWrite = MovementStream.BeginWrite(sendBuf, 0, 13, null, null);
        }

    }

    public Vector3 ReceiveMessage(bool acceptSwap)
    {
        Vector3 position = Vector3.zero;
        if (MessageReceived())
        {
            LastRead += MovementStream.Read(recvBuf, LastRead, 13 - LastRead);
            if (LastRead == 13)
            {
                LastRead = 0;
                float x = BitConverter.ToSingle(recvBuf, 0);
                float y = BitConverter.ToSingle(recvBuf, 4);
                float z = BitConverter.ToSingle(recvBuf, 8);
                position = new Vector3(x, y, z);
                resolveSwapState(recvBuf[12]);
            }
        }
        return position;
    }

    //0 means do nothing,
    //1 means send 2 to accept,
    //2 means perform swap if local and recieved are 2
    //3 means clear state

    private void resolveSwapState(byte recievedState)
    {
        switch (recievedState)
        {
            case 3: accepted = false;
                _swapState = 0;
                break;
            case 2: accepted = _swapState == 2;
                break;
            case 1: _swapState = (byte)(granted ? 2 : 3);
                break;
        }
    }

    public bool CheckSwapGranted()
    {
        return accepted;
    }
}