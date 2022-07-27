using System.Net.Sockets;

internal class Program
{
    public static Server server = null;

    private static void Main(string[] args)
    {
        server = new Server("127.0.0.1", 9999);
        while (true)
        {
            string input = Console.ReadLine();
            if(input.Substring(0, 3).Equals("<B>"))
            {
                PlayerMsg msg = new PlayerMsg();
                msg.playerID = 10010;
                msg.playerData = new PlayerData();
                msg.playerData.name = "UDP同步服务器消息";
                msg.playerData.atk = 99;
                msg.playerData.lev = 88;
                server.Broadcast(msg);
            }
        }
    }
}