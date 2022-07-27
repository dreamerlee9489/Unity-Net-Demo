internal class Program
{
    public static Server server = null;
    
    private static void Main(string[] args)
    {
        server = new Server();
        server.Start("127.0.0.1", 9999, 1024);
        Console.WriteLine("TCP异步服务器已启动!");

        while(true)
        {
            string input = Console.ReadLine();
            if(input.Substring(2) == "1001")
            {
                PlayerMsg msg = new PlayerMsg();
                msg.playerID = 10010;
                msg.playerData = new PlayerData();
                msg.playerData.name = "来自服务端的消息";
                msg.playerData.lev = 99;
                msg.playerData.atk = 88;
                server.Broadcast(msg);
            }
        }
    }
}