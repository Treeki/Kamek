using System;
using System.IO;

namespace Kamek
{
    class Dol
    {
        public struct Section
        {
            public uint LoadAddress;
            public byte[] Data;

            public uint EndAddress { get { return (uint)(LoadAddress + Data.Length); } }
        }

        public readonly Section[] Sections;
        public uint EntryPoint;
        public uint BssAddress, BssSize;


        public Dol(Stream input)
        {
            Sections = new Section[18];
            var br = new BinaryReader(input);

            var fields = new uint[3 * 18];
            for (int i = 0; i < fields.Length; i++)
                fields[i] = br.ReadBigUInt32();

            for (int i = 0; i < 18; i++)
            {
                uint fileOffset = fields[i];
                uint size = fields[36 + i];
                Sections[i].LoadAddress = fields[18 + i];

                long savedPosition = input.Position;
                input.Position = fileOffset;
                Sections[i].Data = br.ReadBytes((int) size);
                input.Position = savedPosition;
            }

            BssAddress = br.ReadBigUInt32();
            BssSize = br.ReadBigUInt32();
            EntryPoint = br.ReadBigUInt32();
        }


        public void Write(Stream output)
        {
            var bw = new BinaryWriter(output);

            // Generate the header
            var fields = new uint[3 * 18];
            uint position = 0x100;
            for (int i = 0; i < 18; i++)
            {
                if (Sections[i].Data.Length > 0)
                {
                    fields[i] = position;
                    fields[i + 18] = Sections[i].LoadAddress;
                    fields[i + 36] = (uint)Sections[i].Data.Length;
                    position += (uint)((Sections[i].Data.Length + 0x1F) & ~0x1F);
                }
            }

            for (int i = 0; i < (3 * 18); i++)
                bw.WriteBE(fields[i]);
            bw.WriteBE(BssAddress);
            bw.WriteBE(BssSize);
            bw.WriteBE(EntryPoint);
            bw.Write(new byte[0x100 - 0xE4]);

            // Write all sections
            for (int i = 0; i < 18; i++)
            {
                bw.Write(Sections[i].Data);

                int paddedLength = ((Sections[i].Data.Length + 0x1F) & ~0x1F);
                int padding = paddedLength - Sections[i].Data.Length;
                if (padding > 0)
                    bw.Write(new byte[padding]);
            }
        }


        public bool ResolveAddress(uint address, out int sectionID, out uint offset)
        {
            for (int i = 0; i < Sections.Length; i++)
            {
                if (address >= Sections[i].LoadAddress && address < Sections[i].EndAddress)
                {
                    sectionID = i;
                    offset = address - Sections[i].LoadAddress;
                    return true;
                }
            }

            sectionID = -1;
            offset = 0;
            return false;
        }


        public uint ReadUInt32(uint address)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in DOL file");

            return Util.ExtractUInt32(Sections[sectionID].Data, offset);
        }

        public void WriteUInt32(uint address, uint value)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in DOL file");

            Util.InjectUInt32(Sections[sectionID].Data, offset, value);
        }


        public ushort ReadUInt16(uint address)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in DOL file");

            return Util.ExtractUInt16(Sections[sectionID].Data, offset);
        }

        public void WriteUInt16(uint address, ushort value)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in DOL file");

            Util.InjectUInt16(Sections[sectionID].Data, offset, value);
        }


        public byte ReadByte(uint address)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in DOL file");

            return Sections[sectionID].Data[offset];
        }

        public void WriteByte(uint address, byte value)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in DOL file");

            Sections[sectionID].Data[offset] = value;
        }
    }
}
