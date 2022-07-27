using System.Text;
using System.Net;
using System.Net.Sockets;

public class Program
{
    static bool isStart = false;
    static int cacheIndex = 0;
    static byte[] cacheBuffer = new byte[1024];
    static Socket localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    static List<Socket> connectedSockets = new List<Socket>();

    private static void Main(string[] args)
    {
        try
        {
            localSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999));
            localSocket.Listen(1024);
            isStart = true;
            Console.WriteLine("TCP同步服务器已启动");
        }
        catch (SocketException e)
        {
            Console.WriteLine("TCP同步服务器绑定端口失败!");
            throw new SocketException(e.ErrorCode);
        }

        Task acceptTask = Task.Run(() =>
        {
            while (isStart)
            {
                Socket newSocket = localSocket.Accept();
                connectedSockets.Add(newSocket);
                PlayerMsg msg = new();
                msg.playerID = 10010;
                msg.playerData.atk = 100;
                msg.playerData.lev = 20;
                msg.playerData.name = "来自服务器的信息";
                newSocket.Send(msg.Writing());
                Console.WriteLine(newSocket.RemoteEndPoint + "连入了服务器" + newSocket.LocalEndPoint);
            }
        });

        Task receiveTask = Task.Run(() =>
        {
            int count = 0;
            while (isStart)
            {
                for (int i = 0; i < connectedSockets.Count; i++)
                {
                    if (connectedSockets[i].Available > 0)
                    {
                        byte[] buffer = new byte[1024];
                        count = connectedSockets[i].Receive(buffer);
                        ThreadPool.QueueUserWorkItem(HandleMessage, (connectedSockets[i], buffer, count));
                    }
                }
            }
        });

        while(isStart)
        {
            string input = Console.ReadLine();
            if(input.Equals("quit"))
            {
                for (int i = 0; i < connectedSockets.Count; i++)
                {
                    connectedSockets[i].Shutdown(SocketShutdown.Both);
                    connectedSockets[i].Close();
                }
                connectedSockets.Clear();
                isStart = false;
            }
            else if(input.Substring(0, 3).Equals("<B>"))
            {
                for (int i = 0; i < connectedSockets.Count; i++)
                    connectedSockets[i].Send(Encoding.UTF8.GetBytes("广播消息: " + input.Substring(3)));
            }
        }
    }

    static void HandleMessage(object? state)
    {
        (Socket s, byte[] buffer, int count) tuple = ((Socket s, byte[] buffer, int count))state;
        tuple.buffer.CopyTo(cacheBuffer, cacheIndex);
        cacheIndex += tuple.count;
        while (true)
        {
            int readIndex = 0, msgID = -1, msgLength = -1;
            if (cacheIndex - readIndex >= 8)
            {
                msgID = BitConverter.ToInt32(cacheBuffer, readIndex);
                readIndex += 4;
                msgLength = BitConverter.ToInt32(cacheBuffer, readIndex);
                readIndex += 4;
            }
            if (cacheIndex - readIndex >= msgLength && msgLength != -1)
            {
                BaseMsg msg = null;
                switch (msgID)
                {
                    case 1001:
                        msg = new PlayerMsg();
                        msg.Reading(cacheBuffer, readIndex);
                        break;
                    case 999:
                        msg = new HeartMsg();
                        break;;
                }
                if(msg != null)
                    Console.WriteLine("收到来自" + tuple.s.RemoteEndPoint + "的消息: " + msg.GetType());
                readIndex += msgLength;
                if (readIndex == cacheIndex)
                {
                    cacheIndex = 0;
                    break;
                }
            }
            else
            {
                if (msgLength != -1)
                    readIndex -= 8;
                Array.Copy(cacheBuffer, readIndex, cacheBuffer, 0, cacheIndex - readIndex);
                cacheIndex -= readIndex;
                break;
            }
        }
    }
}