using System.Net;

public class Client
{
    public IPEndPoint localEP;
    public int id;
    public long lastTime = -1;
    byte[] recvBuff = new byte[512];

    public Client(string ip, int port, int id)
    {
        this.id = id;
        this.localEP = new IPEndPoint(IPAddress.Parse(ip), port);
    }

    public void ReceiveMsg(byte[] bytes)
    {
        bytes.CopyTo(recvBuff, 0);
        lastTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
        ThreadPool.QueueUserWorkItem(HandleMsg, recvBuff);
    }

    private void HandleMsg(object? state)
    {
        byte[] bytes = state as byte[];
        int nowIndex = 0;
        int id = BitConverter.ToInt32(bytes, nowIndex);
        nowIndex += 4;
        int length = BitConverter.ToInt32(bytes, nowIndex);
        nowIndex += 4;
        switch (id)
        {
            case 1001:
                PlayerMsg playerMsg = new PlayerMsg();
                playerMsg.Reading(bytes, nowIndex);
                Console.WriteLine(playerMsg.playerID);
                Console.WriteLine(playerMsg.playerData.name);
                Console.WriteLine(playerMsg.playerData.atk);
                Console.WriteLine(playerMsg.playerData.lev);
                break;
            case 1003:
                QuitMsg quitMsg = new QuitMsg();
                Program.server.RemoveClient(id);
                break;
        }
    }
}