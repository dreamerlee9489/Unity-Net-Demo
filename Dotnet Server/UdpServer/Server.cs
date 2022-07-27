using System.Net;
using System.Net.Sockets;

public class Server
{
    Socket socket = null;
    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
    Dictionary<int, Client> clients = new();
    byte[] recvBuff = new byte[512];

    public Server(string ip, int port)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
        Task.Run(ReceiveMsg);
        Task.Run(CheckTimeOut);
        Console.WriteLine("UDP同步服务器已启动!");
    }

    private void CheckTimeOut()
    {
        List<int> removeList = new();
        while (socket != null)
        {
            Thread.Sleep(30000);
            long nowTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            foreach (int id in clients.Keys)
                if(nowTime - clients[id].lastTime >= 30)
                    removeList.Add(id);
            foreach (int i in removeList)
                clients.Remove(i);
            removeList.Clear();
        }
    }

    private void ReceiveMsg()
    {
        string ip;
        int port;
        int id;
        while (socket != null)
        {
            if(socket?.Available > 0)
            {
                socket.ReceiveFrom(recvBuff, ref remoteEP);
                ip = (remoteEP as IPEndPoint).Address.ToString();
                port = (remoteEP as IPEndPoint).Port;
                id = ip.GetHashCode() ^ port.GetHashCode();
                if(!clients.ContainsKey(id))
                    clients.Add(id, new Client(ip, port, id));
                clients[id].ReceiveMsg(recvBuff);
            }
        }
    }

    public void Broadcast(BaseMsg msg)
    {
        Console.WriteLine("count=" + clients.Count);
        foreach (int id in clients.Keys)
        {
            try
            {
                socket.SendTo(msg.Writing(), clients[id].localEP);
            }
            catch (SocketException e)
            {
                Console.WriteLine("发送失败: " + e.Message);                
                throw new SocketException(e.ErrorCode);
            }
        }
    }

    public void RemoveClient(int id)
    {
        if(clients.ContainsKey(id))
        {
            clients.Remove(id);
            Console.WriteLine("已移除客户端{0}" + clients[id].localEP);
        }
    }

    public void Close()
    {
        socket.Shutdown(SocketShutdown.Both);
        socket.Close();
        socket = null;
    }
}