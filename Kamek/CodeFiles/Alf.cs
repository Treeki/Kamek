using System;
using System.Collections.Generic;
using System.IO;

namespace Kamek.CodeFiles
{
    class Alf : CodeFile
    {
        private const uint MAGIC = 0x464F4252;

        public struct Symbol
        {
            public uint Address;
            public uint Size;
            public byte[] MangledName;
            public byte[] DemangledName;
            public bool IsData;
            public uint? Unk10;
        }

        public struct Section : IComparable<Section>
        {
            public uint LoadAddress;
            public uint Size;  // may be smaller than Data.Length, for bss
            public byte[] Data;
            public List<Symbol> Symbols;

            public uint EndAddress { get { return (uint)(LoadAddress + Size); } }

            public int CompareTo(Section other)
            {
                return (int)LoadAddress - (int)other.LoadAddress;
            }
        }

        public uint Version;  // 104 or 105
        public List<Section> Sections;
        public uint EntryPoint;


        public Alf(Stream input)
        {
            var br = new BinaryReader(input);

            uint magic = br.ReadLittleUInt32();
            Version = br.ReadLittleUInt32();
            EntryPoint = br.ReadLittleUInt32();
            uint numSections = br.ReadLittleUInt32();

            if (magic != MAGIC)
                throw new InvalidOperationException("wrong ALF magic");
            if (Version != 104 && Version != 105)
                throw new InvalidOperationException("unrecognized ALF version");

            Sections = new List<Section>();

            for (int i = 0; i < numSections; i++)
            {
                Section section = new Section();
                section.LoadAddress = br.ReadLittleUInt32();
                int storedSize = br.ReadLittleInt32();
                section.Size = br.ReadLittleUInt32();
                section.Data = br.ReadBytes(storedSize);
                section.Symbols = new List<Symbol>();
                Sections.Add(section);
            }

            br.ReadLittleUInt32();  // table size, ignored
            uint numSymbols = br.ReadLittleUInt32();

            for (int i = 0; i < numSymbols; i++)
            {
                Symbol symbol = new Symbol();

                int mangledNameSize = br.ReadLittleInt32();
                symbol.MangledName = br.ReadBytes(mangledNameSize);

                int demangledNameSize = br.ReadLittleInt32();
                symbol.DemangledName = br.ReadBytes(demangledNameSize);

                symbol.Address = br.ReadLittleUInt32();
                symbol.Size = br.ReadLittleUInt32();
                symbol.IsData = br.ReadLittleUInt32() == 1;
                int sectionID = br.ReadLittleInt32();

                if (Version == 105)
                    symbol.Unk10 = br.ReadLittleUInt32();
                else
                    symbol.Unk10 = null;

                Sections[sectionID - 1].Symbols.Add(symbol);
            }
        }


        public override void Write(Stream output)
        {
            var bw = new BinaryWriter(output);

            // Header
            bw.WriteLE(MAGIC);
            bw.WriteLE(Version);
            bw.WriteLE(EntryPoint);
            bw.WriteLE((uint)Sections.Count);

            // Sort the sections
            Sections.Sort();

            // Write all sections
            foreach (var section in Sections)
            {
                bw.WriteLE(section.LoadAddress);
                bw.WriteLE((uint)section.Data.Length);
                bw.WriteLE(section.Size);
                bw.Write(section.Data);
            }

            // Write the symbol table
            long savedPosition = output.Position;
            bw.WriteLE((uint)0);  // table size
            bw.WriteLE((uint)0);  // total number of entries

            int symbolCount = 0;
            for (int i = 0; i < Sections.Count; i++)
            {
                Section section = Sections[i];

                symbolCount += section.Symbols.Count;

                foreach (var symbol in section.Symbols)
                {
                    bw.WriteLE((uint)symbol.MangledName.Length);
                    bw.Write(symbol.MangledName);
                    bw.WriteLE((uint)symbol.DemangledName.Length);
                    bw.Write(symbol.DemangledName);
                    bw.WriteLE(symbol.Address);
                    bw.WriteLE(symbol.Size);
                    bw.WriteLE((uint)(symbol.IsData ? 1 : 0));
                    bw.WriteLE((uint)(i + 1));
                    if (Version == 105)
                        bw.WriteLE(symbol.Unk10.GetValueOrDefault());
                }
            }

            // 8 extra null bytes for some reason -- don't know why ALF does this ¯\_(ツ)_/¯
            bw.WriteLE((uint)0);
            bw.WriteLE((uint)0);

            long tableSize = output.Position - savedPosition - 4;
            output.Position = savedPosition;
            bw.WriteLE((uint)tableSize);
            bw.WriteLE((uint)symbolCount);
        }


        public bool ResolveAddress(uint address, out int sectionID, out uint offset)
        {
            for (int i = 0; i < Sections.Count; i++)
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


        public override uint ReadUInt32(uint address)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in ALF file");

            return Util.ExtractUInt32(Sections[sectionID].Data, offset);
        }

        public override void WriteUInt32(uint address, uint value)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in ALF file");

            Util.InjectUInt32(Sections[sectionID].Data, offset, value);
        }


        public override ushort ReadUInt16(uint address)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in ALF file");

            return Util.ExtractUInt16(Sections[sectionID].Data, offset);
        }

        public override void WriteUInt16(uint address, ushort value)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in ALF file");

            Util.InjectUInt16(Sections[sectionID].Data, offset, value);
        }


        public override byte ReadByte(uint address)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in ALF file");

            return Sections[sectionID].Data[offset];
        }

        public override void WriteByte(uint address, byte value)
        {
            int sectionID;
            uint offset;
            if (!ResolveAddress(address, out sectionID, out offset))
                throw new InvalidOperationException("address out of range in ALF file");

            Sections[sectionID].Data[offset] = value;
        }
    }
}
