using System.Net;
using System.Net.Sockets;

public class Server
{
    Socket socket = null;
    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
    byte[] receiveBuffer = new byte[512];
    Dictionary<int, Client> clients = new();

    public Server(string ip, int port)
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEP, ReceiveFromCallback, remoteEP);
            ThreadPool.QueueUserWorkItem(CheckTimeThread);
            Console.WriteLine("UDP异步服务器已启动");
        }
        catch (System.Exception e)
        {
            Console.WriteLine("UDP异步服务器启动失败: " + e.Message);
            throw;
        }
    }

    private void CheckTimeThread(object? state)
    {
        long currTime = 0;
        List<int> removeList = new();
        while (true)
        {
            Thread.Sleep(30000);
            currTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            foreach (int id in clients.Keys)
                if (currTime - clients[id].lastTime >= 30)
                    removeList.Add(clients[id].id);
            for (int i = 0; i < removeList.Count; i++)
                RemoveClient(removeList[i]);
            removeList.Clear();
        }
    }

    private void ReceiveFromCallback(IAsyncResult ar)
    {
        try
        {
            socket.EndReceiveFrom(ar, ref remoteEP);
            string ip = (remoteEP as IPEndPoint).Address.ToString();
            int port = (remoteEP as IPEndPoint).Port;
            int id = ip.GetHashCode() ^ port.GetHashCode();
            if (!clients.ContainsKey(id))
                clients.Add(id, new Client(ip, port, id));
            clients[id].ReceiveMsg(receiveBuffer);
            socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEP, ReceiveFromCallback, remoteEP);
        }
        catch (System.Exception e)
        {
            Console.WriteLine("接收失败: " + e.Message);
            throw;
        }
    }

    public void SendTo(byte[] bytes, EndPoint remoteEP)
    {
        socket.BeginSendTo(bytes, 0, bytes.Length, SocketFlags.None, remoteEP, (ar) =>
        {
            try
            {
                socket.EndSendTo(ar);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("发送失败: " + e.Message);
                throw;
            }
        }, null);
    }

    public void Broadcast(BaseMsg msg)
    {
        foreach (int id in clients.Keys)
            SendTo(msg.Writing(), clients[id].localEP);
    }

    public void RemoveClient(int id)
    {
        if (clients.ContainsKey(id))
        {
            Console.WriteLine("客户端{0}被移除了", clients[id].localEP);
            clients.Remove(id);
        }
    }

    public void Close()
    {
        if (socket != null)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket = null;
        }
    }
}