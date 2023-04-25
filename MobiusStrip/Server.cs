
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace MobiusStrip;

public class Server: NetworkNode
{
    private readonly Thread _acceptThread;


    public Server(string ip, int port) : base(ip, port)
    {
        IPAddress address = IPAddress.Parse(ip);
        var server = new TcpListener(address, port);
        _acceptThread = new Thread(() =>
        {
            server.Start();
            _client = server.AcceptTcpClient();
            MovementStream = _client.GetStream();
            ModMain.Log($"[Server]: Connected!");
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