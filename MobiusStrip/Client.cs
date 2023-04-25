
using System.Net.Sockets;
using System.Threading;


namespace MobiusStrip;

public class Client : NetworkNode
{
    readonly Thread _acceptThread;

    public Client(string ip, int port) : base(ip, port)
    {
        _acceptThread = new Thread(() =>
        {
            _client = new TcpClient();
            _client.Connect(ip, port);
            MovementStream = _client.GetStream();
            ModMain.Log($"[CLIENT]: Connected!");

        });
    }
    

    public override void AwaitConnection()
    {
        if (!_acceptThread.IsAlive)
        {
            _acceptThread.Start();
        }
    }
    
}