using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HSMSComponent
{
    class Program
    {
        public enum ITEM_TYPE
        {
            LIST = 0,   // 0x00 Byte
            BIN = 32,   // 0x20 Byte
            BOOL = 36,  // 0x24 Byte
            ASCII = 64, // 0x40 Byte
            JIS8 = 68,  // 0x44 Byte
            CHAR16 = 72,    // 0x48 UINT16
            INT8 = 100, // 0x64 SByte
            INT64 = 96, // 0x60 INT64
            INT32 = 112,    // 0x70 INT32
            INT16 = 104,  // 0x68 INT16
            FLOAT64 = 128,  // 0x80 Double
            FLOAT32 = 144,  // 0x90 Single
            UINT64 = 160,   // 0xA0 UINT64
            UINT8 = 164,    // 0xA4 Byte
            UINT16 = 168,   // 0xA8 UINT16
            UINT32 = 176,   // 0xB0 UINT32
            UNKNOWN = 255   // 0xFF
        }

        /*
         * [H]
         * 00-00-86-0B-00-00-00-00-00-02-
         * [B]
         * 01-03-B1-00-B1-04-00-00-10-68-
         * 01-01-01-02-B1-00-01-04-41-05-
         * 4C-4F-54-30-31-41-05-4C-4F-54-
         * 30-32-41-04-4F-50-30-31-41-05-
         * 52-43-50-30-31
         */

        static void Main(string[] args)
        {
            XmlDocument doc = Decorde();
            Console.WriteLine(PrettyPrint(doc.OuterXml));
            Console.ReadKey();
        }

        public static string PrettyPrint(string XML)
        {
            string Result = "";

            MemoryStream MS = new MemoryStream();
            XmlTextWriter W = new XmlTextWriter(MS, Encoding.Unicode);
            XmlDocument D = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                D.LoadXml(XML);

                W.Formatting = System.Xml.Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                D.WriteContentTo(W);
                W.Flush();
                MS.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                MS.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader SR = new StreamReader(MS);

                // Extract the text from the StreamReader.
                string FormattedXML = SR.ReadToEnd();

                Result = FormattedXML;
            }
            catch (XmlException)
            {
            }

            MS.Close();
            W.Close();

            return Result;
        }

        private static XmlDocument Decorde()
        {
            string rawStr = string.Empty;
            string[] temp = null;
            byte[] rawData = null;

            List<byte> bytes = new List<byte>();

            XmlDocument xdoc = new XmlDocument();
            XmlElement root = xdoc.CreateElement("MESSAGE");

            rawStr = "00 00 86 0B 00 00 00 00 00 02 01 03 B1 00 B1 04 00 00 10 68 01 01 01 02 B1 00 01 04 41 05 4C 4F 54 30 31 41 05 4C 4F 54 30 32 41 04 4F 50 30 31 41 05 52 43 50 30 31";
            //rawStr = "00 00 86 0B 00 00 00 00 00 02 01 03 B1 00 B1 04 00 00 10 68 01 01 01 02 B1 00 01 05 41 05 4C 4F 54 30 31 41 05 4C 4F 54 30 32 41 04 4F 50 30 31 41 05 52 43 50 30 31 21 02 AA FE";
            //rawStr = "00 00 86 0B 00 00 00 00 00 02 01 03 B1 00 B1 04 00 00 10 68 01 01 01 02 B1 00 01 05 41 05 4C 4F 54 30 31 41 05 4C 4F 54 30 32 41 04 4F 50 30 31 41 05 52 43 50 30 31 25 01 01";


            temp = rawStr.Split(' ');

            try
            {
                for (int i = 0; i < temp.Length; i++)
                {
                    byte b;
                    b = Convert.ToByte(temp[i], 16);
                    bytes.Add(b);
                }

                rawData = bytes.ToArray();

                SetRawData(ref root, rawData);
            }
            catch
            {
            }

            xdoc.AppendChild(root);
            return xdoc;
        }

        private static void SetRawData(ref XmlElement root, byte[] rawData)
        {
            byte stream;
            byte function;

            int point = 0;
            string msg = string.Empty;

            stream = Convert.ToByte(rawData[2] & 0x7F); //  0x7F [0111 1111] ==> bit0 control message, bit1~7 stream 
            function = rawData[3];
            point = 10;

            //
            root.SetAttribute("F", function.ToString());
            root.SetAttribute("S", stream.ToString());
            root.SetAttribute("TYPE", "MESSAGE");

            ParseBytes(ref root, ref point, rawData);
        }

        private static void ParseBytes(ref XmlElement root, ref int ptr, byte[] data)
        {
            ITEM_TYPE itemType;

            XmlElement elmnt;
            UInt32 itemLength = 0;

            int lShift = 8;
            byte noLenByte = 0;

            string type = string.Empty;
            string value = string.Empty;


            itemType = (ITEM_TYPE)(data[ptr] & 0xFC);     // 0xFC [1111 1100]
            noLenByte = Convert.ToByte(data[ptr] & 0x03); // 0x03 [0000 0011]
            ptr++;

            itemLength = Convert.ToUInt16(data[ptr] << lShift * (noLenByte - 1));
            ptr += noLenByte;

            elmnt = root.OwnerDocument.CreateElement("ITEM");

            switch (itemType)
            {
                case ITEM_TYPE.LIST:
                    type = "LIST";
                    value = itemLength.ToString();
                    for (int idx = 0; idx < itemLength; idx++) //List에서 No Of Length Byte는 Child의 갯수
                    {
                        ParseBytes(ref elmnt, ref ptr, data);
                    }
                    break;

                case ITEM_TYPE.BIN:
                    StringBuilder sb = new StringBuilder();
                    for (int idx = 0; idx < itemLength; idx++)
                    {
                        sb.AppendFormat("{0:X} ", data[ptr]); //hex
                        ptr++;
                    }
                    value = sb.ToString().Trim();
                    break;

                case ITEM_TYPE.BOOL:
                    type = "BOOL";
                    if (itemLength != 0)
                    {
                        if (data[ptr] != 0) value += "TRUE";
                        else value += "FALSE";
                        ptr++;
                    }
                    break;

                case ITEM_TYPE.JIS8:
                    break;
                case ITEM_TYPE.CHAR16:
                    break;
                case ITEM_TYPE.ASCII:
                    type = "ASCII";
                    byte[] ascBytes = new byte[itemLength];
                    Array.Copy(data, ptr, ascBytes, 0, itemLength);
                    ptr = ptr + (int)itemLength;
                    value = System.Text.ASCIIEncoding.Default.GetString(ascBytes);
                    break;

                case ITEM_TYPE.INT8:
                    break;
                case ITEM_TYPE.INT16:
                    break;
                case ITEM_TYPE.INT32:
                    break;
                case ITEM_TYPE.INT64:
                    break;
                case ITEM_TYPE.UINT8:
                    break;
                case ITEM_TYPE.UINT16:
                    break;
                case ITEM_TYPE.UINT32:
                    type = "UINT32";
                    byte[] u4Byte = new byte[4];
                    UInt32 u4 = 0;

                    itemLength = itemLength / 4;

                    if (itemLength != 0)
                    {
                        for (int idx = 0; idx < itemLength; idx++)
                        {
                            Array.Copy(data, ptr, u4Byte, 0, 4);
                            Array.Reverse(u4Byte);
                            ptr = ptr + 4;

                            u4 = BitConverter.ToUInt32(u4Byte, 0);

                            if (idx == 0) value += u4.ToString();
                            else value += value + " " + u4.ToString();
                        }
                    }
                    break;

                case ITEM_TYPE.UINT64:

                    break;
                case ITEM_TYPE.FLOAT32:
                    break;
                case ITEM_TYPE.FLOAT64:
                    break;
            }

            elmnt.SetAttribute("TYPE", type);
            elmnt.SetAttribute("VALUE", value);
            root.AppendChild(elmnt);
        }
    }
}
