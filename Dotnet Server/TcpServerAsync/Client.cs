using System.Net.Sockets;

public class Client
{
    public int id = 0;
    public static int INITIAL_ID = 10000;
    public Socket? socket = null;

    byte[] buffer = new byte[1024];
    int bufferIndex = 0;

    long lastTime = 0;
    static int TIME_OUT_SPAN = 10;

    public Client(Socket socket)
    {
        id = INITIAL_ID++;
        this.socket = socket;
        socket.BeginReceive(buffer, bufferIndex, buffer.Length, SocketFlags.None, ReceiveCallback, null);
        Task.Run(() =>
        {
            while (socket != null && socket.Connected)
            {
                if (lastTime != 0 && DateTime.Now.Ticks / TimeSpan.TicksPerSecond - lastTime >= TIME_OUT_SPAN)
                {
                    Program.server.CloseClient(this);
                    break;
                }
            }
            Thread.Sleep(5000);
        });
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            if(socket != null && socket.Connected)
            {
                int num = socket.EndReceive(ar);
                HandleReceiveMsg(num);
                socket.BeginReceive(buffer, bufferIndex, buffer.Length - bufferIndex, SocketFlags.None, ReceiveCallback, null);
            }
            else
            {
                Console.WriteLine("接收失败: 与服务端断开连接");
                Program.server.CloseClient(this);
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("接收消息错误: " + e.Message);     
            throw new SocketException(e.ErrorCode);
        }
    }

    private void HandleReceiveMsg(int num)
    {
        bufferIndex += num;
        int msgID = -1, msgLength = -1, readIndex = 0;
        while(true)
        {
            msgLength = -1;
            if(bufferIndex - readIndex >= 8)
            {
                msgID = BitConverter.ToInt32(buffer, readIndex);
                readIndex += 4;
                msgLength = BitConverter.ToInt32(buffer, readIndex);
                readIndex += 4;
            }
            if(bufferIndex - readIndex >= msgLength && msgLength != -1)
            {
                BaseMsg msg = null;
                switch (msgID)
                {
                case 1001:
                    msg = new PlayerMsg();
                    msg.Reading(buffer, readIndex);
                    break;
                case 1003:
                    msg = new QuitMsg();
                    break;
                case 999:
                    msg = new HeartMsg();
                    break;
                }
                if(msg != null)
                    ThreadPool.QueueUserWorkItem(ParseMsg, msg);
                readIndex += msgLength;
                if(readIndex == bufferIndex)
                {
                    bufferIndex = 0;
                    break;
                }
            }
            else
            {
                if(msgLength != -1)
                    readIndex -= 8;
                Array.Copy(buffer, readIndex, buffer, 0, bufferIndex - readIndex);
                bufferIndex -= readIndex;
                break;
            }
        }
    }

    private void ParseMsg(object? state)
    {
        switch (state)
        {
        case PlayerMsg msg:
            PlayerMsg playerMsg = msg as PlayerMsg;
            Console.WriteLine(playerMsg.playerID);
            Console.WriteLine(playerMsg.playerData.name);
            Console.WriteLine(playerMsg.playerData.lev);
            Console.WriteLine(playerMsg.playerData.atk);
            break;
        case QuitMsg msg:
            Program.server.CloseClient(this);
            break;
        case HeartMsg msg:
            lastTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            Console.WriteLine("来自" + socket.RemoteEndPoint + "的心跳消息");
            break;
        }
    }

    public void Send(BaseMsg msg)
    {
        if(socket != null && socket.Connected)
        {
            byte[] bytes = msg.Writing();
            socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, null);
        }
        else
        {
            Program.server.CloseClient(this);
        }
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            if(socket != null && socket.Connected)
                socket.EndSend(ar);
            else
                Program.server.CloseClient(this);
        }
        catch (SocketException e)
        {
            Console.WriteLine("发送失败: " + e.Message);
            throw new SocketException(e.ErrorCode);
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