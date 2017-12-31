using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ME3Server_WV
{
    public static class Blaze
    {
        #region Classes and Structs
        public class Packet
        {
            public ushort Length;
            public ushort Component;
            public ushort Command;
            public ushort Error;
            public ushort QType;
            public ushort ID;
            public ushort extLength;
            public byte[] Content;
        }
        public struct DoubleVal
        {
            public long v1;
            public long v2;
            public DoubleVal(long V1, long V2)
            {
                v1 = V1;
                v2 = V2;
            }
        }
        public struct TrippleVal
        {
            public long v1;
            public long v2;
            public long v3;
            public TrippleVal(long V1, long V2, long V3)
            {
                v1 = V1;
                v2 = V2;
                v3 = V3;
            }
        }
        public class Tdf
        {
            public string Label;
            public uint Tag;
            public byte Type;
            public TreeNode ToTree()
            {
                string typedescription;
                switch (Type)
                {
                    case 0:
                        typedescription = "(TdfInteger)";
                        break;
                    case 1:
                        typedescription = "(TdfString)";
                        break;
                    case 2:
                        typedescription = "(TdfBlob)";
                        break;
                    case 3:
                        typedescription = "(TdfStruct)";
                        break;
                    case 4:
                        typedescription = "(TdfList)";
                        break;
                    case 5:
                        typedescription = "(TdfDoubleList)";
                        break;
                    case 6:
                        typedescription = "(TdfUnion)";
                        break;
                    case 7:
                        typedescription = "(TdfIntegerList)";
                        break;
                    case 8:
                        typedescription = "(TdfDoubleVal)";
                        break;
                    case 9:
                        typedescription = "(TdfTrippleVal)";
                        break;
                    case 0xA:
                        typedescription = "(TdfFloat)";
                        break;
                    default:
                        typedescription = "(unknown)";
                        break;
                }
                return new TreeNode(Label + " : " + Type + " " + typedescription);
            }
            public void Set(string label, byte type)
            {
                Label = label;
                Type = type;
                Tag = 0;
                byte[] buff = Label2Tag(label);
                Tag |= (uint)(buff[0] << 24);
                Tag |= (uint)(buff[1] << 16);
                Tag |= (uint)(buff[2] << 8);
            }
        }
        public class TdfInteger : Tdf
        {
            public long Value;
            public static TdfInteger Create(string Label, long value)
            {
                TdfInteger res = new TdfInteger();
                res.Set(Label, 0);
                res.Value = value;
                return res;
            }
        }
        public class TdfFloat : Tdf
        {
            public float Value;
            public static TdfFloat Create(string Label, float value)
            {
                TdfFloat res = new TdfFloat();
                res.Set(Label, 0xA);
                res.Value = value;
                return res;
            }
        }
        public class TdfString : Tdf
        {
            public string Value;
            public static TdfString Create(string Label, string value)
            {
                TdfString res = new TdfString();
                res.Set(Label, 1);
                res.Value = value;
                return res;
            }
        }
        public class TdfStruct : Tdf
        {
            public List<Tdf> Values;
            public bool startswith2;
            public static TdfStruct Create(string Label, List<Tdf> list, bool start2 = false)
            {
                TdfStruct res = new TdfStruct();
                res.startswith2 = start2;
                res.Set(Label, 3);
                res.Values = list;
                return res;
            }
        }
        public class TdfList : Tdf
        {
            public byte SubType;
            public int Count;
            public object List;            
            public static TdfList Create(string Label, byte subtype, int count, object list)
            {
                TdfList res = new TdfList();
                res.Set(Label, 4);
                res.SubType = subtype;
                res.Count = count;
                res.List = list;
                return res;
            }
        }
        public class TdfIntegerList : Tdf
        {
            public int Count;
            public List<long> List;
            public static TdfIntegerList Create(string Label, int count, List<long> list)
            {
                TdfIntegerList res = new TdfIntegerList();
                res.Set(Label, 7);
                res.Count = count;
                res.List = list;
                return res;
            }
        }
        public class TdfDoubleList : Tdf
        {
            public byte SubType1;
            public byte SubType2;
            public int Count;
            public object List1;
            public object List2;
            public static TdfDoubleList Create(string Label, byte subtype1, byte subtype2, object list1, object list2, int count)
            {
                TdfDoubleList res = new TdfDoubleList();
                res.Set(Label, 5);
                res.SubType1 = subtype1;
                res.SubType2 = subtype2;
                res.List1 = list1;
                res.List2 = list2;
                res.Count = count;
                return res;
            }
        }
        public class TdfDoubleVal : Tdf
        {
            public DoubleVal Value;
        }
        public class TdfTrippleVal : Tdf
        {
            public TrippleVal Value;
            public static TdfTrippleVal Create(string Label, TrippleVal v)
            {
                TdfTrippleVal res = new TdfTrippleVal();
                res.Set(Label, 9);
                res.Value = v;
                return res;
            }
        }
        public class TdfUnion : Tdf
        {
            public byte UnionType;
            public Tdf UnionContent;
            public static TdfUnion Create(string Label, byte unionType = 0x7F, Tdf data = null)
            {
                TdfUnion res = new TdfUnion();
                res.Set(Label, 6);
                res.UnionType = unionType;
                res.UnionContent = data;
                return res;
            }
        }
        public class TdfBlob : Tdf
        {
            public byte[] Data;
            public static TdfBlob Create(string Label, byte[] data = null)
            {
                TdfBlob res = new TdfBlob();
                res.Set(Label, 2);
                if (data == null)
                    res.Data = new byte[0];
                else
                    res.Data = data;
                return res;
            }
        }
        #endregion

        #region Functions
        public static Packet ReadBlazePacket(Stream s)
        {
            Packet res = new Packet();
            res.Length = ReadUShort(s);
            res.Component = ReadUShort(s);
            res.Command = ReadUShort(s);
            res.Error = ReadUShort(s);
            res.QType = ReadUShort(s);
            res.ID = ReadUShort(s);
            if ((res.QType & 0x10) != 0)
                res.extLength = ReadUShort(s);
            else
                res.extLength = 0;
            int len = res.Length + (res.extLength << 16);
            res.Content = new byte[len];
            s.Read(res.Content, 0, len);
            return res;
        }
        public static Packet ReadBlazePacketHeader(Stream s)
        {
            Packet res = new Packet();
            res.Length = ReadUShort(s);
            res.Component = ReadUShort(s);
            res.Command = ReadUShort(s);
            res.Error = ReadUShort(s);
            res.QType = ReadUShort(s);
            res.ID = ReadUShort(s);
            if ((res.QType & 0x10) != 0)
                res.extLength = ReadUShort(s);
            else
                res.extLength = 0;
            int len = res.Length + (res.extLength << 16);
            res.Content = new byte[len];
            return res;
        }
        public static List<Packet> FetchAllBlazePackets(Stream s)
        {
            List<Packet> res = new List<Packet>();
            s.Seek(0, 0);
            while (s.Position < s.Length)
            {
                try
                {
                    res.Add(ReadBlazePacket(s));
                }
                catch (Exception)
                {
                    s.Position = s.Length;
                }
            }
            return res;
        }
        public static ushort ReadUShort(Stream s)
        {
            byte[] buff = new byte[2];
            s.Read(buff, 0, 2);
            return (ushort)((buff[0] << 8) + buff[1]);
        }
        public static uint ReadUInt(Stream s)
        {
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            return (uint)((buff[0] << 24) + (buff[1] << 16) + (buff[2] << 8) + buff[3]);
        }
        public static float ReadFloat(Stream s)
        {
            byte[] buff = new byte[4];
            byte[] buffr = new byte[4];
            s.Read(buff, 0, 4);
            for (int i = 0; i < 4; i++)
                buffr[i] = buff[3 - i];
            return BitConverter.ToSingle(buffr, 0);
        }
        public static void WriteFloat(Stream s, float f)
        {
            byte[] buff = BitConverter.GetBytes(f);
            byte[] buffr = new byte[4];
            s.Read(buff, 0, 4);
            for (int i = 0; i < 4; i++)
                buffr[i] = buff[3 - i];
            s.Write(buffr, 0, 4);
        }
        public static string TagToLabel(uint Tag)
        {
            string s = "";
            List<byte> buff = new List<byte>(BitConverter.GetBytes(Tag));
            buff.Reverse();
            byte[] res = new byte[4];
            res[0] |= (byte)((buff[0] & 0x80) >> 1);
            res[0] |= (byte)((buff[0] & 0x40) >> 2);
            res[0] |= (byte)((buff[0] & 0x30) >> 2);
            res[0] |= (byte)((buff[0] & 0x0C) >> 2);

            res[1] |= (byte)((buff[0] & 0x02) << 5);
            res[1] |= (byte)((buff[0] & 0x01) << 4);
            res[1] |= (byte)((buff[1] & 0xF0) >> 4);

            res[2] |= (byte)((buff[1] & 0x08) << 3);
            res[2] |= (byte)((buff[1] & 0x04) << 2);
            res[2] |= (byte)((buff[1] & 0x03) << 2);
            res[2] |= (byte)((buff[2] & 0xC0) >> 6);

            res[3] |= (byte)((buff[2] & 0x20) << 1);
            res[3] |= (byte)((buff[2] & 0x1F));

            for (int i = 0; i < 4; i++)
            {
                if (res[i] == 0)
                    res[i] = 0x20;
                s += (char)res[i];
            }
            return s;
        }
        public static byte[] Label2Tag(string Label)
        {            
            byte[] res = new byte[3];
            while (Label.Length < 4)
                Label += '\0';
            if (Label.Length > 4)
                Label = Label.Substring(0, 4);
            byte[] buff = new byte[4];
            for (int i = 0; i < 4; i++)
                buff[i] = (byte)Label[i];
            res[0] |= (byte)((buff[0] & 0x40) << 1);
            res[0] |= (byte)((buff[0] & 0x10) << 2);
            res[0] |= (byte)((buff[0] & 0x0F) << 2);
            res[0] |= (byte)((buff[1] & 0x40) >> 5);
            res[0] |= (byte)((buff[1] & 0x10) >> 4);

            res[1] |= (byte)((buff[1] & 0x0F) << 4);
            res[1] |= (byte)((buff[2] & 0x40) >> 3);
            res[1] |= (byte)((buff[2] & 0x10) >> 2);
            res[1] |= (byte)((buff[2] & 0x0C) >> 2);

            res[2] |= (byte)((buff[2] & 0x03) << 6);
            res[2] |= (byte)((buff[3] & 0x40) >> 1);
            res[2] |= (byte)((buff[3] & 0x1F));
            return res;
        }
        public static long DecompressInteger(Stream s)
        {
            List<byte> tmp = new List<byte>();
            byte b;
            while ((b = (byte)s.ReadByte()) >= 0x80)
                tmp.Add(b);
            tmp.Add(b);
            byte[] buff = tmp.ToArray();
            int currshift = 6;
            ulong result = (ulong)(buff[0] & 0x3F);
            for (int i = 1; i < buff.Length; i++)
            {
                byte curbyte = buff[i];
                ulong l = (ulong)(curbyte & 0x7F) << currshift;
                result |= l;
                currshift += 7;
            }
            return (long)result;
        }
        public static void CompressInteger(long l, Stream s)
        {
            List<byte> result = new List<byte>();
            if (l < 0x40)
            {
                result.Add((byte)(l & 0xFF));
            }
            else
            {
                byte curbyte = (byte)((l & 0x3F) | 0x80);
                result.Add(curbyte);
                long currshift = l >> 6;
                while (currshift >= 0x80)
                {
                    curbyte = (byte)((currshift & 0x7F) | 0x80);
                    currshift >>= 7;
                    result.Add(curbyte);
                }
                result.Add((byte)currshift);
            }
            foreach (byte b in result)
                s.WriteByte(b);
        }
        public static string ReadString(Stream s)
        {
            int len = (int)DecompressInteger(s);
            string res = "";
            for (int i = 0; i < len - 1; i++)
                res += (char)s.ReadByte();
            s.ReadByte();
            return res;
        }
        public static void WriteString(string str, Stream s)
        {
            int len;
            if(str.EndsWith("\0"))
                len = (int)str.Length;
            else
                len = (int)str.Length + 1;
            CompressInteger(len, s);
            for (int i = 0; i < len - 1; i++)
                s.WriteByte((byte)str[i]);
            s.WriteByte(0);
        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public static byte[] PacketToRaw(Packet p)
        {
            List<byte> res = new List<byte>();
            res.Add((byte)(p.Length >> 8));
            res.Add((byte)(p.Length & 0xFF));
            res.Add((byte)(p.Component >> 8));
            res.Add((byte)(p.Component & 0xFF));
            res.Add((byte)(p.Command >> 8));
            res.Add((byte)(p.Command & 0xFF));
            res.Add((byte)(p.Error >> 8));
            res.Add((byte)(p.Error & 0xFF));
            res.Add((byte)(p.QType >> 8));
            res.Add((byte)(p.QType & 0xFF));
            res.Add((byte)(p.ID >> 8));
            res.Add((byte)(p.ID & 0xFF));
            if ((p.QType & 0x10) != 0)
            {
                res.Add((byte)(p.extLength >> 8));
                res.Add((byte)(p.extLength & 0xFF));
            }
            res.AddRange(p.Content);
            return res.ToArray();
        }
        public static byte[] CreatePacket(ushort Component,ushort Command, ushort Error, ushort QType, ushort ID, List<Tdf> Content)
        {
            List<byte> res = new List<byte>();
            res.Add(0);                          //0
            res.Add(0);     
            res.Add((byte)(Component >> 8));
            res.Add((byte)(Component & 0xFF));
            res.Add((byte)(Command >> 8));      //4
            res.Add((byte)(Command & 0xFF));
            res.Add((byte)(Error >> 8));
            res.Add((byte)(Error & 0xFF));
            res.Add((byte)(QType >> 8));        //8
            res.Add((byte)(QType & 0xFF));
            res.Add((byte)(ID >> 8));
            res.Add((byte)(ID & 0xFF));  
            MemoryStream m = new MemoryStream();
            foreach (Tdf tdf in Content)
                WriteTdf(tdf, m);
            int len = (int)m.Length;
            res[0] = (byte)((len & 0xFFFF) >> 8);
            res[1] = (byte)(len & 0xFF);
            if (len > 0xFFFF)
            {
                res[9] = 0x10;
                res.Add((byte)((len & 0xFF000000) >> 24));
                res.Add((byte)((len & 0x00FF0000) >> 16));
            }
            else
                res[9] = 0x00;
            res.AddRange(m.ToArray());
            return res.ToArray();
        }
        public static uint GetUnixTimeStamp()
        {
            return (UInt32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        public static string HexDump(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            int bytesLength = bytes.Length;
            char[] HexChars = "0123456789ABCDEF".ToCharArray();
            int firstHexColumn = 11;
            int firstCharColumn = firstHexColumn + bytesPerLine * 3 + (bytesPerLine - 1) / 8 + 2;
            int lineLength = firstCharColumn + bytesPerLine + Environment.NewLine.Length;
            char[] line = (new String(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);
            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];
                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            return result.ToString();
        }
        public static Blaze.TdfStruct CreateStructStub(List<Tdf> tdfs, bool has2 = false)
        {
            Blaze.TdfStruct res = new TdfStruct();
            res.Values = tdfs;
            res.startswith2 = has2;
            return res;
        }
        public static string PacketToText(Packet p)
        {
            string text = ListToText("", ReadPacketContent(p));
            return text;
        }
        private static string ListToText(string start, List<Tdf> tdflist)
        {
            string nl = Environment.NewLine;
            string res = "";
            foreach (Tdf item in tdflist)
            {
                res += start + item.Label;
                switch (item.Type)
                {
                    case 0x0:
                        TdfInteger ti = (TdfInteger)item;
                        res += " => " + ti.Value + " (0x" + ti.Value.ToString("X") + ")" + nl ;
                        break;
                    case 0x1:
                        TdfString ts = (TdfString)item;
                        res += " => " + ts.Value + nl;
                        break;
                    case 0x3:
                        TdfStruct tsr = (TdfStruct)item;
                        res += nl + ListToText(start + "_ ", tsr.Values);
                        break;
                    case 0x4:
                        TdfList tl = (TdfList)item;
                        res += " list: " + tl.SubType + nl;
                        if (tl.SubType == 0)
                        {
                            List<long> listi = (List<long>)tl.List;
                            res += start + "_ ";
                            for (int i = 0; i < listi.Count - 1; i++)
                                res += listi[i] + " (0x" + listi[i].ToString("X8") + "), ";
                            res += listi[listi.Count - 1] + " (0x" + listi[listi.Count - 1].ToString("X8") + ")" +  nl;
                        }
                        else if (tl.SubType == 1)
                        {
                            List<string> lists = (List<string>)tl.List;
                            res += start + "_ ";
                            for (int i = 0; i < lists.Count; i++)
                                res += lists[i] + " ";
                            res += nl;
                        }
                        else if (tl.SubType == 3)
                        {
                            List<TdfStruct> listst = (List<TdfStruct>)tl.List;
                            for (int i = 0; i < listst.Count; i++)
                            {
                                res += start + "_ Entry #" + i + nl;
                                res += ListToText("_ _ ", listst[i].Values);
                            }
                        }
                        break;
                    case 0x6:
                        TdfUnion tu = (TdfUnion)item;
                        res += " union: 0x" + tu.UnionType.ToString("X") + nl + ListToText(start + "_ ", new List<Tdf>() { tu.UnionContent });
                        break;
                    case 0x9:
                        TrippleVal tv = ((TdfTrippleVal)item).Value;
                        res += " => " + tv.v1 + " " + tv.v2 + " " + tv.v3;
                        res += " (0x" + tv.v1.ToString("X") + " 0x" + tv.v2.ToString("X") + " 0x" + tv.v1.ToString("X") + ")" + nl;
                        break;
                    default:
                        res += nl;
                        break;
                }
            }
            return res;
        }
        #endregion        

        #region Reading
        public static Tdf ReadTdf(Stream s)
        {
            Tdf res = new Tdf();
            uint Head = ReadUInt(s);
            res.Tag = (Head & 0xFFFFFF00);
            res.Label = TagToLabel(res.Tag);
            res.Type = (byte)(Head & 0xFF);
            switch (res.Type)
            {
                case 0:
                    return ReadTdfInteger(res, s);
                case 1:
                    return ReadTdfString(res, s);
                case 2:
                    return ReadTdfBlob(res, s);
                case 3:
                    return ReadTdfStruct(res, s);
                case 4:
                    return ReadTdfList(res, s);
                case 5:
                    return ReadTdfDoubleList(res, s);
                case 6:
                    return ReadTdfUnion(res, s);
                case 7:
                    return ReadTdfIntegerList(res, s);
                case 8:
                    return ReadTdfDoubleVal(res, s);
                case 9:
                    return ReadTdfTrippleVal(res, s);
                case 0xA:
                    return ReadTdfFloat(res, s);
                default:
                    throw new Exception("Unknown Tdf Type: " + res.Type);
            }
        }
        public static TdfUnion ReadTdfUnion(Tdf head, Stream s)
        {
            TdfUnion res = new TdfUnion();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.UnionType = (byte)s.ReadByte();
            if (res.UnionType != 0x7F)
            {
                res.UnionContent = ReadTdf(s);
            }
            return res;
        }
        public static TdfBlob ReadTdfBlob(Tdf head, Stream s)
        {
            TdfBlob res = new TdfBlob();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.Data = new byte[DecompressInteger(s)];
            for (int i = 0; i < res.Data.Length; i++)
                res.Data[i] = (byte)s.ReadByte();
            return res;
        }
        public static TdfFloat ReadTdfFloat(Tdf head, Stream s)
        {
            TdfFloat res = new TdfFloat();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            res.Value = BitConverter.ToSingle(buff, 0);
            return res;
        }
        public static TdfInteger ReadTdfInteger(Tdf head, Stream s)
        {
            TdfInteger res = new TdfInteger();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.Value = DecompressInteger(s);
            return res;
        }
        public static TdfString ReadTdfString(Tdf head, Stream s)
        {
            TdfString res = new TdfString();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.Value = ReadString(s);
            return res;
        }
        public static TdfStruct ReadTdfStruct(Tdf head, Stream s)
        {
            TdfStruct res = new TdfStruct();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            bool has2 = false;
            res.Values = ReadStruct(s, out has2);
            res.startswith2 = has2;
            return res;
        }
        public static TdfTrippleVal ReadTdfTrippleVal(Tdf head, Stream s)
        {
            TdfTrippleVal res = new TdfTrippleVal();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.Value = ReadTrippleVal(s);
            return res;
        }
        public static TdfDoubleVal ReadTdfDoubleVal(Tdf head, Stream s)
        {
            TdfDoubleVal res = new TdfDoubleVal();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.Value = ReadDoubleVal(s);
            return res;
        }
        public static TdfList ReadTdfList(Tdf head, Stream s)
        {
            TdfList res = new TdfList();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.SubType = (byte)s.ReadByte();
            res.Count = (int)DecompressInteger(s);
            for (int i = 0; i < res.Count; i++)
            {
                switch (res.SubType)
                {
                    case 0:
                        if (res.List == null)
                            res.List = new List<long>();
                        List<long> l1 = (List<long>)res.List;
                        l1.Add(DecompressInteger(s));
                        res.List = l1;
                        break;
                    case 1:
                        if (res.List == null)
                            res.List = new List<string>();
                        List<string> l2 = (List<string>)res.List;
                        l2.Add(ReadString(s));
                        res.List = l2;
                        break;
                    case 3:
                        if (res.List == null)
                            res.List = new List<TdfStruct>();
                        List<TdfStruct> l3 = (List<TdfStruct>)res.List;
                        Blaze.TdfStruct tmp = new TdfStruct();
                        tmp.startswith2 = false;
                        tmp.Values = ReadStruct(s, out tmp.startswith2);
                        l3.Add(tmp);
                        res.List = l3;
                        break;
                    case 9:
                        if (res.List == null)
                            res.List = new List<TrippleVal>();
                        List<TrippleVal> l4 = (List<TrippleVal>)res.List;
                        l4.Add(ReadTrippleVal(s));
                        res.List = l4;
                        break;
                    default:
                        throw new Exception("Unknown Tdf Type in List: " + res.Type);
                }
            }
            return res;
        }
        public static TdfIntegerList ReadTdfIntegerList(Tdf head, Stream s)
        {
            TdfIntegerList res = new TdfIntegerList();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.Count = (int)DecompressInteger(s);
            for (int i = 0; i < res.Count; i++)
            {
                if (res.List == null)
                    res.List = new List<long>();
                List<long> l1 = (List<long>)res.List;
                l1.Add(DecompressInteger(s));
                res.List = l1;                        
            }
            return res;
        }
        public static TdfDoubleList ReadTdfDoubleList(Tdf head, Stream s)
        {
            TdfDoubleList res = new TdfDoubleList();
            res.Label = head.Label;
            res.Tag = head.Tag;
            res.Type = head.Type;
            res.SubType1 = (byte)s.ReadByte();
            res.SubType2 = (byte)s.ReadByte();
            res.Count = (int)DecompressInteger(s);
            for (int i = 0; i < res.Count; i++)
            {
                switch (res.SubType1)
                {
                    case 0:
                        if (res.List1 == null)
                            res.List1 = new List<long>();
                        List<long> l1 = (List<long>)res.List1;
                        l1.Add(DecompressInteger(s));
                        res.List1 = l1;
                        break;
                    case 1:
                        if (res.List1 == null)
                            res.List1 = new List<string>();
                        List<string> l2 = (List<string>)res.List1;
                        l2.Add(ReadString(s));
                        res.List1 = l2;
                        break;
                    case 3:
                        if (res.List1 == null)
                            res.List1 = new List<TdfStruct>();
                        List<TdfStruct> l3 = (List<TdfStruct>)res.List1;
                        Blaze.TdfStruct tmp = new TdfStruct();
                        tmp.startswith2 = false;
                        tmp.Values = ReadStruct(s, out tmp.startswith2);
                        l3.Add(tmp);
                        res.List1 = l3;
                        break;
                    case 0xA:
                        if (res.List1 == null)
                            res.List1 = new List<float>();
                        List<float> lf3 = (List<float>)res.List1;
                        lf3.Add(ReadFloat(s));
                        res.List1 = lf3;
                        break;
                    default:
                        throw new Exception("Unknown Tdf Type in Double List: " + res.SubType1);
                }
                switch (res.SubType2)
                {
                    case 0:
                        if (res.List2 == null)
                            res.List2 = new List<long>();
                        List<long> l1 = (List<long>)res.List2;
                        l1.Add(DecompressInteger(s));
                        res.List2 = l1;
                        break;
                    case 1:
                        if (res.List2 == null)
                            res.List2 = new List<string>();
                        List<string> l2 = (List<string>)res.List2;
                        l2.Add(ReadString(s));
                        res.List2 = l2;
                        break;
                    case 3:
                        if (res.List2 == null)
                            res.List2 = new List<TdfStruct>();
                        List<TdfStruct> l3 = (List<TdfStruct>)res.List2;
                        Blaze.TdfStruct tmp = new TdfStruct();
                        tmp.startswith2 = false;
                        tmp.Values = ReadStruct(s, out tmp.startswith2);
                        l3.Add(tmp);
                        res.List2 = l3;
                        break;
                    case 0xA:
                        if (res.List2 == null)
                            res.List2 = new List<float>();
                        List<float> lf3 = (List<float>)res.List2;
                        lf3.Add(ReadFloat(s));
                        res.List2 = lf3;
                        break;
                    default:
                        throw new Exception("Unknown Tdf Type in Double List: " + res.SubType2);
                }
            }
            return res;
        }
        public static List<Tdf> ReadStruct(Stream s, out bool has2)
        {
            List<Tdf> res = new List<Tdf>();
            byte b = 0;
            bool reshas2 = false;
            while ((b = (byte)s.ReadByte()) != 0)
            {
                if (b != 2)
                    s.Seek(-1, SeekOrigin.Current);
                else
                    reshas2 = true;
                res.Add(ReadTdf(s));
            }
            has2 = reshas2;
            return res;
        }
        public static List<Tdf> ReadPacketContent(Packet p)
        {
            List<Tdf> res = new List<Tdf>();
            MemoryStream m = new MemoryStream(p.Content);
            m.Seek(0, 0);
            try
            {
                while (m.Position < m.Length - 4)
                    res.Add(ReadTdf(m));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n@:" + m.Position.ToString("X"));
            }
            return res;
        }
        public static DoubleVal ReadDoubleVal(Stream s)
        {
            DoubleVal res = new DoubleVal();
            res.v1 = DecompressInteger(s);
            res.v2 = DecompressInteger(s);
            return res;
        }
        public static TrippleVal ReadTrippleVal(Stream s)
        {
            TrippleVal res = new TrippleVal();
            res.v1 = DecompressInteger(s);
            res.v2 = DecompressInteger(s);
            res.v3 = DecompressInteger(s);
            return res;
        }
        #endregion

        #region Writing
        public static void WriteTdf(Tdf tdf, Stream s)
        {
            s.WriteByte((byte)(tdf.Tag >> 24));
            s.WriteByte((byte)(tdf.Tag >> 16));
            s.WriteByte((byte)(tdf.Tag >> 8));
            s.WriteByte(tdf.Type);
            switch (tdf.Type)
            {
                case 0:
                    TdfInteger ti = (TdfInteger)tdf;
                    CompressInteger(ti.Value, s);
                    break;
                case 1:
                    TdfString ts = (TdfString)tdf;
                    WriteString(ts.Value, s);
                    break;
                case 2:
                    TdfBlob tb = (TdfBlob)tdf;
                    CompressInteger(tb.Data.Length, s);
                    for (int i = 0; i < tb.Data.Length; i++)
                        s.WriteByte(tb.Data[i]);
                    break;
                case 3:
                    TdfStruct tst = (TdfStruct)tdf;
                    if (tst.startswith2)
                        s.WriteByte(2);
                    foreach (Tdf ttdf in tst.Values)
                        WriteTdf(ttdf, s);
                    s.WriteByte(0);
                    break;
                case 4:
                    WriteTdfList((TdfList)tdf, s);
                    break;
                case 5:
                    WriteTdfDoubleList((TdfDoubleList)tdf, s);
                    break;
                case 6:
                    TdfUnion tu = (TdfUnion)tdf;
                    s.WriteByte(tu.UnionType);
                    if (tu.UnionType != 0x7F)
                    {
                        WriteTdf(tu.UnionContent, s);
                    }
                    break;
                case 7:
                    TdfIntegerList til = (TdfIntegerList)tdf;
                    CompressInteger(til.Count, s);
                    if (til.Count != 0)
                        foreach (long l in til.List)
                            CompressInteger(l, s);
                    break;
                case 8:
                    WriteDoubleValue(((TdfDoubleVal)tdf).Value, s);
                    break;
                case 9:
                    WriteTrippleValue(((TdfTrippleVal)tdf).Value, s);
                    break;
                case 0xA:
                    TdfFloat tf = (TdfFloat)tdf;
                    WriteFloat(s, tf.Value);
                    break;
            }
        }
        public static void WriteTdfList(TdfList tdf, Stream s)
        {
            s.WriteByte(tdf.SubType);
            CompressInteger(tdf.Count, s);
            for (int i = 0; i < tdf.Count; i++)
                switch (tdf.SubType)
                {
                    case 0:
                        CompressInteger(((List<long>)tdf.List)[i], s);
                        break;
                    case 1:
                        WriteString(((List<string>)tdf.List)[i], s);
                        break;
                    case 3:
                        Blaze.TdfStruct str = ((List<Blaze.TdfStruct>)tdf.List)[i];
                        if (str.startswith2)
                            s.WriteByte(2);
                        foreach (Tdf ttdf in str.Values)
                            WriteTdf(ttdf, s);
                        s.WriteByte(0);
                        break;
                    case 9:
                        WriteTrippleValue(((List<TrippleVal>)tdf.List)[i], s);
                        break;
                }
        }
        public static void WriteTdfDoubleList(TdfDoubleList tdf, Stream s)
        {
            s.WriteByte(tdf.SubType1);
            s.WriteByte(tdf.SubType2);
            CompressInteger(tdf.Count, s);
            for (int i = 0; i < tdf.Count; i++)                
            {
                switch (tdf.SubType1)
                {
                    case 0:
                        CompressInteger(((List<long>)(tdf.List1))[i], s);
                        break;
                    case 1:
                        WriteString(((List<string>)(tdf.List1))[i], s);
                        break;
                    case 3:
                        Blaze.TdfStruct str = ((List<Blaze.TdfStruct>)tdf.List1)[i];
                        if (str.startswith2)
                            s.WriteByte(2);
                        foreach (Tdf ttdf in str.Values)
                            WriteTdf(ttdf, s);
                        s.WriteByte(0);
                        break;
                    case 0xA:
                        WriteFloat(s, ((List<float>)(tdf.List1))[i]);
                        break;
                }
                switch (tdf.SubType2)
                {
                    case 0:
                        CompressInteger(((List<long>)(tdf.List2))[i], s);
                        break;
                    case 1:
                        WriteString(((List<string>)(tdf.List2))[i], s);
                        break;
                    case 3:
                        Blaze.TdfStruct str = ((List<Blaze.TdfStruct>)tdf.List2)[i];
                        if (str.startswith2)
                            s.WriteByte(2);
                        foreach (Tdf ttdf in str.Values)
                            WriteTdf(ttdf, s);
                        s.WriteByte(0);
                        break;
                    case 0xA:
                        WriteFloat(s, ((List<float>)(tdf.List2))[i]);
                        break;
                }
            }
        }
        public static void WriteTrippleValue(TrippleVal v, Stream s)
        {
            CompressInteger(v.v1, s);
            CompressInteger(v.v2, s);
            CompressInteger(v.v3, s);
        }
        public static void WriteDoubleValue(DoubleVal v, Stream s)
        {
            CompressInteger(v.v1, s);
            CompressInteger(v.v2, s);
        }        
        #endregion

        #region Describers
        public static string PacketToDescriber(Blaze.Packet p)
        {
            string desc1_component = p.Component.ToString("X4");
            string desc2_command = p.Command.ToString("X4");
            if (Components.ContainsKey(p.Component))
            {
                desc1_component = Components[p.Component];
                uint commandKey = (uint)(p.Component << 16) + p.Command;
                if (p.QType == 0x2000 && Notifications.ContainsKey(commandKey))
                {
                    desc2_command = Notifications[commandKey];
                }
                else if (Commands.ContainsKey(commandKey))
                {
                    desc2_command = Commands[commandKey];
                }
            }
            return desc1_component + " : " + desc2_command;
        }

        public static Dictionary<ushort, string> Components = new Dictionary<ushort, string>()
        {
            { 0x1, "Authentication Component" },
            { 0x3, "Example Component" },
            { 0x4, "Game Manager Component" },
            { 0x5, "Redirector Component" },
            { 0x6, "Play Groups Component" },
            { 0x7, "Stats Component" },
            { 0x9, "Util Component" },
            { 0xA, "Census Data Component" },
            { 0xB, "Clubs Component" },
            { 0xC, "Game Report Lagacy Component" },
            { 0xD, "League Component" },
            { 0xE, "Mail Component" },
            { 0xF, "Messaging Component" },
            { 0x14, "Locker Component" },
            { 0x15, "Rooms Component" },
            { 0x17, "Tournaments Component" },
            { 0x18, "Commerce Info Component" },
            { 0x19, "Association Lists Component" },
            { 0x1B, "GPS Content Controller Component" },
            { 0x1C, "Game Reporting Component" },
            { 0x7D0, "Dynamic Filter Component" },
            { 0x801, "RSP Component" },
            { 0x7802, "User Sessions Component" }
        };

        public static Dictionary<uint, string> Commands = new Dictionary<uint, string>()
        {
            //Authentication Component
            { 0x0001000A, "createAccount" },
            { 0x00010014, "updateAccount" },
            { 0x0001001C, "updateParentalEmail" },
            { 0x0001001D, "listUserEntitlements2" },
            { 0x0001001E, "getAccount" },
            { 0x0001001F, "grantEntitlement" },
            { 0x00010020, "listEntitlements" },
            { 0x00010021, "hasEntitlement" },
            { 0x00010022, "getUseCount" },
            { 0x00010023, "decrementUseCount" },
            { 0x00010024, "getAuthToken" },
            { 0x00010025, "getHandoffToken" },
            { 0x00010026, "getPasswordRules" },
            { 0x00010027, "grantEntitlement2" },
            { 0x00010028, "login" },
            { 0x00010029, "acceptTos" },
            { 0x0001002A, "getTosInfo" },
            { 0x0001002B, "modifyEntitlement2" },
            { 0x0001002C, "consumecode" },
            { 0x0001002D, "passwordForgot" },
            { 0x0001002E, "getTermsAndConditionsContent" },
            { 0x0001002F, "getPrivacyPolicyContent" },
            { 0x00010030, "listPersonaEntitlements2" },
            { 0x00010032, "silentLogin" },
            { 0x00010033, "checkAgeReq" },
            { 0x00010034, "getOptIn" },
            { 0x00010035, "enableOptIn" },
            { 0x00010036, "disableOptIn" },
            { 0x0001003C, "expressLogin" },
            { 0x00010046, "logout" },
            { 0x00010050, "createPersona" },
            { 0x0001005A, "getPersona" },
            { 0x00010064, "listPersonas" },
            { 0x0001006E, "loginPersona" },
            { 0x00010078, "logoutPersona" },
            { 0x0001008C, "deletePersona" },
            { 0x0001008D, "disablePersona" },
            { 0x0001008F, "listDeviceAccounts" },
            { 0x00010096, "xboxCreateAccount" },
            { 0x00010098, "originLogin" },
            { 0x000100A0, "xboxAssociateAccount" },
            { 0x000100AA, "xboxLogin" },
            { 0x000100B4, "ps3CreateAccount" },
            { 0x000100BE, "ps3AssociateAccount" },
            { 0x000100C8, "ps3Login" },
            { 0x000100D2, "validateSessionKey" },
            { 0x000100E6, "createWalUserSession" },
            { 0x000100F1, "acceptLegalDocs" },
            { 0x000100F2, "getLegalDocsInfo" },
            { 0x000100F6, "getTermsOfServiceContent" },
            { 0x0001012C, "deviceLoginGuest" },
            // Game Manager Component
            { 0x00040001, "createGame" },
            { 0x00040002, "destroyGame" },
            { 0x00040003, "advanceGameState" },
            { 0x00040004, "setGameSettings" },
            { 0x00040005, "setPlayerCapacity" },
            { 0x00040006, "setPresenceMode" },
            { 0x00040007, "setGameAttributes" },
            { 0x00040008, "setPlayerAttributes" },
            { 0x00040009, "joinGame" },
            { 0x0004000B, "removePlayer" },
            { 0x0004000D, "startMatchmaking" },
            { 0x0004000E, "cancelMatchmaking" },
            { 0x0004000F, "finalizeGameCreation" },
            { 0x00040011, "listGames" },
            { 0x00040012, "setPlayerCustomData" },
            { 0x00040013, "replayGame" },
            { 0x00040014, "returnDedicatedServerToPool" },
            { 0x00040015, "joinGameByGroup" },
            { 0x00040016, "leaveGameByGroup" },
            { 0x00040017, "migrateGame" },
            { 0x00040018, "updateGameHostMigrationStatus" },
            { 0x00040019, "resetDedicatedServer" },
            { 0x0004001A, "updateGameSession" },
            { 0x0004001B, "banPlayer" },
            { 0x0004001D, "updateMeshConnection" },
            { 0x0004001F, "removePlayerFromBannedList" },
            { 0x00040020, "clearBannedList" },
            { 0x00040021, "getBannedList" },
            { 0x00040026, "addQueuedPlayerToGame" },
            { 0x00040027, "updateGameName" },
            { 0x00040028, "ejectHost" },
            { 0x00040050, "*notifyGameUpdated" },
            { 0x00040064, "getGameListSnapshot" },
            { 0x00040065, "getGameListSubscription" },
            { 0x00040066, "destroyGameList" },
            { 0x00040067, "getFullGameData" },
            { 0x00040068, "getMatchmakingConfig" },
            { 0x00040069, "getGameDataFromId" },
            { 0x0004006A, "addAdminPlayer" },
            { 0x0004006B, "removeAdminPlayer" },
            { 0x0004006C, "setPlayerTeam" },
            { 0x0004006D, "changeGameTeamId" },
            { 0x0004006E, "migrateAdminPlayer" },
            { 0x0004006F, "getUserSetGameListSubscription" },
            { 0x00040070, "swapPlayersTeam" },
            { 0x00040096, "registerDynamicDedicatedServerCreator" },
            { 0x00040097, "unregisterDynamicDedicatedServerCreator" },
            // Redirector Component
            { 0x00050001, "getServerInstance" },
            // Stats Component
            { 0x00070001, "getStatDescs" },
            { 0x00070002, "getStats" },
            { 0x00070003, "getStatGroupList" },
            { 0x00070004, "getStatGroup" },
            { 0x00070005, "getStatsByGroup" },
            { 0x00070006, "getDateRange" },
            { 0x00070007, "getEntityCount" },
            { 0x0007000A, "getLeaderboardGroup" },
            { 0x0007000B, "getLeaderboardFolderGroup" },
            { 0x0007000C, "getLeaderboard" },
            { 0x0007000D, "getCenteredLeaderboard" },
            { 0x0007000E, "getFilteredLeaderboard" },
            { 0x0007000F, "getKeyScopesMap" },
            { 0x00070010, "getStatsByGroupAsync" },
            { 0x00070011, "getLeaderboardTreeAsync" },
            { 0x00070012, "getLeaderboardEntityCount" },
            { 0x00070013, "getStatCategoryList" },
            { 0x00070014, "getPeriodIds" },
            { 0x00070015, "getLeaderboardRaw" },
            { 0x00070016, "getCenteredLeaderboardRaw" },
            { 0x00070017, "getFilteredLeaderboardRaw" },
            { 0x00070018, "changeKeyscopeValue" },
            // Util Component
            { 0x00090001, "fetchClientConfig" },
            { 0x00090002, "ping" },
            { 0x00090003, "setClientData" },
            { 0x00090004, "localizeStrings" },
            { 0x00090005, "getTelemetryServer" },
            { 0x00090006, "getTickerServer" },
            { 0x00090007, "preAuth" },
            { 0x00090008, "postAuth" },
            { 0x0009000A, "userSettingsLoad" },
            { 0x0009000B, "userSettingsSave" },
            { 0x0009000C, "userSettingsLoadAll" },
            { 0x0009000E, "deleteUserSettings" },
            { 0x00090014, "filterForProfanity" },
            { 0x00090015, "fetchQosConfig" },
            { 0x00090016, "setClientMetrics" },
            { 0x00090017, "setConnectionState" },
            { 0x00090018, "getPssConfig" },
            { 0x00090019, "getUserOptions" },
            { 0x0009001A, "setUserOptions" },
            { 0x0009001B, "suspendUserPing" },
            // Messaging Component
            { 0x000F0001, "sendMessage" },
            { 0x000F0002, "fetchMessages" },
            { 0x000F0003, "purgeMessages" },
            { 0x000F0004, "touchMessages" },
            { 0x000F0005, "getMessages" },
            // Association Lists Component
            { 0x00190001, "addUsersToList" },
            { 0x00190002, "removeUsersFromList" },
            { 0x00190003, "clearLists" },
            { 0x00190004, "setUsersToList" },
            { 0x00190005, "getListForUser" },
            { 0x00190006, "getLists" },
            { 0x00190007, "subscribeToLists" },
            { 0x00190008, "unsubscribeFromLists" },
            { 0x00190009, "getConfigListsInfo" },
            // Game Reporting Component
            { 0x001C0001, "submitGameReport" },
            { 0x001C0002, "submitOfflineGameReport" },
            { 0x001C0003, "submitGameEvents" },
            { 0x001C0004, "getGameReportQuery" },
            { 0x001C0005, "getGameReportQueriesList" },
            { 0x001C0006, "getGameReports" },
            { 0x001C0007, "getGameReportView" },
            { 0x001C0008, "getGameReportViewInfo" },
            { 0x001C0009, "getGameReportViewInfoList" },
            { 0x001C000A, "getGameReportTypes" },
            { 0x001C000B, "updateMetric" },
            { 0x001C000C, "getGameReportColumnInfo" },
            { 0x001C000D, "getGameReportColumnValues" },
            { 0x001C0064, "submitTrustedMidGameReport" },
            { 0x001C0065, "submitTrustedEndGameReport" },
            // User Sessions Component
            { 0x78020003, "fetchExtendedData" },
            { 0x78020005, "updateExtendedDataAttribute" },
            { 0x78020008, "updateHardwareFlags" },
            { 0x7802000C, "lookupUser" },
            { 0x7802000D, "lookupUsers" },
            { 0x7802000E, "lookupUsersByPrefix" },
            { 0x78020014, "updateNetworkInfo" },
            { 0x78020017, "lookupUserGeoIPData" },
            { 0x78020018, "overrideUserGeoIPData" },
            { 0x78020019, "updateUserSessionClientData" },
            { 0x7802001A, "setUserInfoAttribute" },
            { 0x7802001B, "resetUserGeoIPData" },
            { 0x78020020, "lookupUserSessionId" },
            { 0x78020021, "fetchLastLocaleUsedAndAuthError" },
            { 0x78020022, "fetchUserFirstLastAuthTime" },
            { 0x78020023, "resumeSession" }
        };

        public static Dictionary<uint, string> Notifications = new Dictionary<uint, string>()
        {
            // Game Manager Component
            { 0x0004000A, "NotifyMatchmakingFailed" },
            { 0x0004000C, "NotifyMatchmakingAsyncStatus" },
            { 0x0004000F, "NotifyGameCreated" },
            { 0x00040010, "NotifyGameRemoved" },
            { 0x00040014, "NotifyGameSetup" },
            { 0x00040015, "NotifyPlayerJoining" },
            { 0x00040016, "NotifyJoiningPlayerInitiateConnections" },
            { 0x00040017, "NotifyPlayerJoiningQueue" },
            { 0x00040018, "NotifyPlayerPromotedFromQueue" },
            { 0x00040019, "NotifyPlayerClaimingReservation" },
            { 0x0004001E, "NotifyPlayerJoinCompleted" },
            { 0x00040028, "NotifyPlayerRemoved" },
            { 0x0004003C, "NotifyHostMigrationFinished" },
            { 0x00040046, "NotifyHostMigrationStart" },
            { 0x00040047, "NotifyPlatformHostInitialized" },
            { 0x00040050, "NotifyGameAttribChange" },
            { 0x0004005A, "NotifyPlayerAttribChange" },
            { 0x0004005F, "NotifyPlayerCustomDataChange" },
            { 0x00040064, "NotifyGameStateChange" },
            { 0x0004006E, "NotifyGameSettingsChange" },
            { 0x0004006F, "NotifyGameCapacityChange" },
            { 0x00040070, "NotifyGameReset" },
            { 0x00040071, "NotifyGameReportingIdChange" },
            { 0x00040073, "NotifyGameSessionUpdated" },
            { 0x00040074, "NotifyGamePlayerStateChange" },
            { 0x00040075, "NotifyGamePlayerTeamChange" },
            { 0x00040076, "NotifyGameTeamIdChange" },
            { 0x00040077, "NotifyProcessQueue" },
            { 0x00040078, "NotifyPresenceModeChanged" },
            { 0x00040079, "NotifyGamePlayerQueuePositionChange" },
            { 0x000400C9, "NotifyGameListUpdate" },
            { 0x000400CA, "NotifyAdminListChange" },
            { 0x000400DC, "NotifyCreateDynamicDedicatedServerGame" },
            { 0x000400E6, "NotifyGameNameChange" }
        };
        #endregion
    }
}
