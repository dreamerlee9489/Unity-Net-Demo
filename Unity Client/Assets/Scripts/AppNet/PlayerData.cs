using System.Text;

namespace AppNet
{
    /// <summary>
    /// 玩家数据类
    /// </summary>
    public class PlayerData : BaseData //0
    {
        //1
        public string name;
        public int atk;
        public int lev;

        //2
        public override int GetBytesNum()
        {
            int num = 0;
            num += 4 + Encoding.UTF8.GetBytes(name).Length;
            num += 4;
            num += 4;
            return num;
        }

        //4
        public override int Reading(byte[] bytes, int beginIndex = 0)
        {
            int index = beginIndex;
            name = ReadString(bytes, ref index);
            atk = ReadInt(bytes, ref index);
            lev = ReadInt(bytes, ref index);
            return index - beginIndex;
        }

        //3
        public override byte[] Writing()
        {
            int index = 0;
            byte[] bytes = new byte[GetBytesNum()];
            WriteString(bytes, name, ref index);
            WriteInt(bytes, atk, ref index);
            WriteInt(bytes, lev, ref index);
            return bytes;
        }
    }
}
