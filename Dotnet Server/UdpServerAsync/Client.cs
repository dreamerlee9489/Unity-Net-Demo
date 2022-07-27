using System.Net;
public class Client 
{
    public IPEndPoint localEP;
    public string ip;
    public int port;
    public int id;
    public long lastTime = -1;

    public Client(string ip, int port, int id)
    {
        this.ip = ip;
        this.port = port;
        this.id = id;
        localEP = new IPEndPoint(IPAddress.Parse(ip), port);
    }

    public void ReceiveMsg(byte[] bytes)
    {
        byte[] receiveBuffer = new byte[512];
        bytes.CopyTo(receiveBuffer, 0);
        lastTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
        ThreadPool.QueueUserWorkItem(HandleMsg, receiveBuffer);
    }

    private void HandleMsg(object? state)
    {
        try
        {
            byte[] bytes = state as byte[];
            int index = 0;
            int id = BitConverter.ToInt32(bytes, index);
            index += 4;
            int length = BitConverter.ToInt32(bytes, index);
            index += 4;
            switch (id)
            {
                case 1001:
                    PlayerMsg playerMsg = new PlayerMsg();
                    playerMsg.Reading(bytes, index);
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
        catch (System.Exception e)
        {
            Console.WriteLine("处理消息出错: " + e.Message);
            throw;
        }
    }
}