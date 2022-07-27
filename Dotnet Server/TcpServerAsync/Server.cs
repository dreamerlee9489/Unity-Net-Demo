using System.Net;
using System.Net.Sockets;

public class Server
{
    Socket? localSocket = null;
    Dictionary<int, Client> clients = new();
    
    public void Start(string ip, int port, int count)
    {
        localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        try
        {
            localSocket.Bind(endPoint);
            localSocket.Listen(1024);
            localSocket.BeginAccept(AcceptCallback, null);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e.Message);
            throw new SocketException(e.ErrorCode);
        }
    }

    void AcceptCallback(IAsyncResult ar)
    {
        Socket newSocket = localSocket.EndAccept(ar);
        Client client = new Client(newSocket);
        clients.Add(client.id, client);
        localSocket.BeginAccept(AcceptCallback, null);
    }

    public void CloseClient(Client client)
    {
        lock(clients)
        {
            client.Close();
            if(clients.ContainsKey(client.id))
            {
                clients.Remove(client.id);
                Console.WriteLine("客户端{0}主动断开连接", client.socket.RemoteEndPoint);
            }
        }
    }

    public void Broadcast(BaseMsg msg)
    {
        foreach (var client in clients.Values)
            client.Send(msg);
    }
}