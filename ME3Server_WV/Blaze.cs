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
            string t = p.Command.ToString("X");
            string t2 = p.Component.ToString("X");
            string[] lines = ComponentNames.Split(',');
            string cname = "";
            foreach (string line in lines)
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2 && parts[1] == t2)
                {
                    cname = parts[0];
                    break;
                }
            }
            switch (p.Component)
            {
                case 0x1:
                    for (int i = 0; i < DescComponent1.Length / 2; i++)
                        if (DescComponent1[i * 2] == t)
                            return cname + " : " + DescComponent1[i * 2 + 1];
                    break;
                case 0x4:
                    for (int i = 0; i < DescComponent4.Length / 2; i++)
                        if (DescComponent4[i * 2] == t)
                            return cname + " : " + DescComponent4[i * 2 + 1];
                    break;
                case 0x7:
                    for (int i = 0; i < DescComponent7.Length / 2; i++)
                        if (DescComponent7[i * 2] == t)
                            return cname + " : " + DescComponent7[i * 2 + 1];
                    break;
                case 0x9:
                    for (int i = 0; i < DescComponent9.Length / 2; i++)
                        if (DescComponent9[i * 2] == t)
                            return cname + " : " + DescComponent9[i * 2 + 1];
                    break;
                case 0xF:
                    for (int i = 0; i < DescComponentF.Length / 2; i++)
                        if (DescComponentF[i * 2] == t)
                            return cname + " : " + DescComponentF[i * 2 + 1];
                    break;
                case 0x19:
                    for (int i = 0; i < DescComponent19.Length / 2; i++)
                        if (DescComponent19[i * 2] == t)
                            return cname + " : " + DescComponent19[i * 2 + 1];
                    break;
                case 0x1C:
                    for (int i = 0; i < DescComponent1C.Length / 2; i++)
                        if (DescComponent1C[i * 2] == t)
                            return cname + " : " + DescComponent1C[i * 2 + 1];
                    break;
                case 0x7802:
                    for (int i = 0; i < DescComponent7802.Length / 2; i++)
                        if (DescComponent7802[i * 2] == t)
                            return cname + " : " + DescComponent7802[i * 2 + 1];
                    break;
            }
            return cname + " : " + p.Command.ToString("X");
        }
        public static string ComponentNames = "Authentication Component=1,Example Component=3,Game Manager Component=4,Redirector Component=5,Play Groups Component=6,Stats Component=7,Util Component=9,Census Data Component=A,Clubs Component=B,Game Report Lagacy Component=C,League Component=D,Mail Component=E,Messaging Component=F,Locker Component=14,Rooms Component=15,Tournaments Component=17,Commerce Info Component=18,Association Lists Component=19,GPS Content Controller Component=1B,Game Reporting Component=1C,Dynamic Filter Component=7D0,RSP Component=801,User Sessions Component=7802";
        public static string[] DescComponent1 = { "A", "createAccount", "14", "updateAccount", "1C", "updateParentalEmail", "1D", "listUserEntitlements2", "1E", "getAccount", "1F", "grantEntitlement", "20", "listEntitlements", "21", "hasEntitlement", "22", "getUseCount", "23", "decrementUseCount", "24", "getAuthToken", "25", "getHandoffToken", "26", "getPasswordRules", "27", "grantEntitlement2", "28", "login", "29", "acceptTos", "2A", "getTosInfo", "2B", "modifyEntitlement2", "2C", "consumecode", "2D", "passwordForgot", "2E", "getTermsAndConditionsContent", "2F", "getPrivacyPolicyContent", "30", "listPersonaEntitlements2", "32", "silentLogin", "33", "checkAgeReq", "34", "getOptIn", "35", "enableOptIn", "36", "disableOptIn", "3C", "expressLogin", "46", "logout", "50", "createPersona", "5A", "getPersona", "64", "listPersonas", "6E", "loginPersona", "78", "logoutPersona", "8C", "deletePersona", "8D", "disablePersona", "8F", "listDeviceAccounts", "96", "xboxCreateAccount", "98", "originLogin", "A0", "xboxAssociateAccount", "AA", "xboxLogin", "B4", "ps3CreateAccount", "BE", "ps3AssociateAccount", "C8", "ps3Login", "D2", "validateSessionKey", "E6", "createWalUserSession", "F1", "acceptLegalDocs", "F2", "getLegalDocsInfo", "F6", "getTermsOfServiceContent", "12C", "deviceLoginGuest" };
        public static string[] DescComponent4 = { "1", "createGame", "2", "destroyGame", "3", "advanceGameState", "4", "setGameSettings", "5", "setPlayerCapacity", "6", "setPresenceMode", "7", "setGameAttributes", "8", "setPlayerAttributes", "9", "joinGame", "B", "removePlayer", "D", "startMatchmaking", "E", "cancelMatchmaking", "F", "finalizeGameCreation", "11", "listGames", "12", "setPlayerCustomData", "13", "replayGame", "14", "returnDedicatedServerToPool", "15", "joinGameByGroup", "16", "leaveGameByGroup", "17", "migrateGame", "18", "updateGameHostMigrationStatus", "19", "resetDedicatedServer", "1A", "updateGameSession", "1B", "banPlayer", "1D", "updateMeshConnection", "1F", "removePlayerFromBannedList", "20", "clearBannedList", "21", "getBannedList", "26", "addQueuedPlayerToGame", "27", "updateGameName", "28", "ejectHost", "64", "getGameListSnapshot", "65", "getGameListSubscription", "66", "destroyGameList", "67", "getFullGameData", "68", "getMatchmakingConfig", "69", "getGameDataFromId", "6A", "addAdminPlayer", "6B", "removeAdminPlayer", "6C", "setPlayerTeam", "6D", "changeGameTeamId", "6E", "migrateAdminPlayer", "6F", "getUserSetGameListSubscription", "70", "swapPlayersTeam", "96", "registerDynamicDedicatedServerCreator", "97", "unregisterDynamicDedicatedServerCreator" };
        public static string[] DescComponent7 = { "1", "getStatDescs", "2", "getStats", "3", "getStatGroupList", "4", "getStatGroup", "5", "getStatsByGroup", "6", "getDateRange", "7", "getEntityCount", "A", "getLeaderboardGroup", "B", "getLeaderboardFolderGroup", "C", "getLeaderboard", "D", "getCenteredLeaderboard", "E", "getFilteredLeaderboard", "F", "getKeyScopesMap", "10", "getStatsByGroupAsync", "11", "getLeaderboardTreeAsync", "12", "getLeaderboardEntityCount", "13", "getStatCategoryList", "14", "getPeriodIds", "15", "getLeaderboardRaw", "16", "getCenteredLeaderboardRaw", "17", "getFilteredLeaderboardRaw", "18", "changeKeyscopeValue" };
        public static string[] DescComponent9 = { "1", "fetchClientConfig", "2", "ping", "3", "setClientData", "4", "localizeStrings", "5", "getTelemetryServer", "6", "getTickerServer", "7", "preAuth", "8", "postAuth", "A", "userSettingsLoad", "B", "userSettingsSave", "C", "userSettingsLoadAll", "E", "deleteUserSettings", "14", "filterForProfanity", "15", "fetchQosConfig", "16", "setClientMetrics", "17", "setConnectionState", "18", "getPssConfig", "19", "getUserOptions", "1A", "setUserOptions", "1B", "suspendUserPing" };
        public static string[] DescComponentF = { "1", "sendMessage", "2", "fetchMessages", "3", "purgeMessages", "4", "touchMessages", "5", "getMessages" };
        public static string[] DescComponent19 = { "1", "addUsersToList", "2", "removeUsersFromList", "3", "clearLists", "4", "setUsersToList", "5", "getListForUser", "6", "getLists", "7", "subscribeToLists", "8", "unsubscribeFromLists", "9", "getConfigListsInfo" };
        public static string[] DescComponent1C = { "1", "submitGameReport", "2", "submitOfflineGameReport", "3", "submitGameEvents", "4", "getGameReportQuery", "5", "getGameReportQueriesList", "6", "getGameReports", "7", "getGameReportView", "8", "getGameReportViewInfo", "9", "getGameReportViewInfoList", "A", "getGameReportTypes", "B", "updateMetric", "C", "getGameReportColumnInfo", "D", "getGameReportColumnValues", "64", "submitTrustedMidGameReport", "65", "submitTrustedEndGameReport" };
        public static string[] DescComponent7802 = { "3", "fetchExtendedData", "5", "updateExtendedDataAttribute", "8", "updateHardwareFlags", "C", "lookupUser", "D", "lookupUsers", "E", "lookupUsersByPrefix", "14", "updateNetworkInfo", "17", "lookupUserGeoIPData", "18", "overrideUserGeoIPData", "19", "updateUserSessionClientData", "1A", "setUserInfoAttribute", "1B", "resetUserGeoIPData", "20", "lookupUserSessionId", "21", "fetchLastLocaleUsedAndAuthError", "22", "fetchUserFirstLastAuthTime", "23", "resumeSession" };
        #endregion
    }
}
