using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek
{
    static class Util
    {
        public static ushort ReadBigUInt16(this BinaryReader br)
        {
            byte a = br.ReadByte();
            byte b = br.ReadByte();
            return (ushort)((a << 8) | b);
        }

        public static uint ReadBigUInt32(this BinaryReader br)
        {
            ushort a = br.ReadBigUInt16();
            ushort b = br.ReadBigUInt16();
            return (uint)((a << 16) | b);
        }

        public static int ReadBigInt32(this BinaryReader br)
        {
            ushort a = br.ReadBigUInt16();
            ushort b = br.ReadBigUInt16();
            return (int)((a << 16) | b);
        }

        public static void WriteBE(this BinaryWriter bw, ushort value)
        {
            bw.Write((byte)(value >> 8));
            bw.Write((byte)(value & 0xFF));
        }

        public static void WriteBE(this BinaryWriter bw, uint value)
        {
            bw.WriteBE((ushort)(value >> 16));
            bw.WriteBE((ushort)(value & 0xFFFF));
        }


        public static ushort ExtractUInt16(byte[] array, long offset)
        {
            return (ushort)((array[offset] << 8) | array[offset + 1]);
        }
        public static uint ExtractUInt32(byte[] array, long offset)
        {
            return (uint)((array[offset] << 24) | (array[offset + 1] << 16) |
                (array[offset + 2] << 8) | array[offset + 3]);
        }
        public static void InjectUInt16(byte[] array, long offset, ushort value)
        {
            array[offset] = (byte)((value >> 8) & 0xFF);
            array[offset + 1] = (byte)(value & 0xFF);
        }
        public static void InjectUInt32(byte[] array, long offset, uint value)
        {
            array[offset] = (byte)((value >> 24) & 0xFF);
            array[offset + 1] = (byte)((value >> 16) & 0xFF);
            array[offset + 2] = (byte)((value >> 8) & 0xFF);
            array[offset + 3] = (byte)(value & 0xFF);
        }


        public static string ExtractNullTerminatedString(byte[] table, int offset)
        {
            if (offset >= 0 && offset < table.Length)
            {
                // find where it ends
                for (int i = offset; i < table.Length; i++)
                {
                    if (table[i] == 0)
                    {
                        return Encoding.ASCII.GetString(table, offset, i - offset);
                    }
                }
            }

            return null;
        }


        public static void DumpToConsole(byte[] array)
        {
            int lines = array.Length / 16;

            for (int offset = 0; offset < array.Length; offset += 0x10)
            {
                Console.Write("{0:X8} | ", offset);

                for (int pos = offset; pos < (offset + 0x10) && pos < array.Length; pos++)
                {
                    Console.Write("{0:X2} ", array[pos]);
                }

                Console.Write("| ");

                for (int pos = offset; pos < (offset + 0x10) && pos < array.Length; pos++)
                {
                    if (array[pos] >= ' ' && array[pos] <= 0x7F)
                        Console.Write("{0}", (char)array[pos]);
                    else
                        Console.Write(".");
                }

                Console.WriteLine();
            }
        }
    }
}
