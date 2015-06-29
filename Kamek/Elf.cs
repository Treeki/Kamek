using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek
{
    class Elf
    {
        class ElfHeader
        {
            public uint ei_mag;
            public byte ei_class, ei_data, ei_version, ei_osabi, ei_abiversion;
            public ushort e_type, e_machine;
            public uint e_version, e_entry, e_phoff, e_shoff, e_flags;
            public ushort e_ehsize, e_phentsize, e_phnum, e_shentsize, e_shnum, e_shstrndx;

            public static ElfHeader Read(BinaryReader reader)
            {
                var h = new ElfHeader();

                h.ei_mag = reader.ReadBigUInt32();
                if (h.ei_mag != 0x7F454C46) // "\x7F" "ELF"
                    throw new InvalidDataException("Incorrect ELF header");

                h.ei_class = reader.ReadByte();
                if (h.ei_class != 1)
                    throw new InvalidDataException("Only 32-bit ELF files are supported");

                h.ei_data = reader.ReadByte();
                if (h.ei_data != 2)
                    throw new InvalidDataException("Only big-endian ELF files are supported");

                h.ei_version = reader.ReadByte();
                if (h.ei_version != 1)
                    throw new InvalidDataException("Only ELF version 1 is supported [a]");

                h.ei_osabi = reader.ReadByte();
                h.ei_abiversion = reader.ReadByte();
                reader.BaseStream.Seek(7, SeekOrigin.Current);

                h.e_type = reader.ReadBigUInt16();
                h.e_machine = reader.ReadBigUInt16();
                h.e_version = reader.ReadBigUInt32();
                if (h.e_version != 1)
                    throw new InvalidDataException("Only ELF version 1 is supported [b]");

                h.e_entry = reader.ReadBigUInt32();
                h.e_phoff = reader.ReadBigUInt32();
                h.e_shoff = reader.ReadBigUInt32();
                h.e_flags = reader.ReadBigUInt32();
                h.e_ehsize = reader.ReadBigUInt16();
                h.e_phentsize = reader.ReadBigUInt16();
                h.e_phnum = reader.ReadBigUInt16();
                h.e_shentsize = reader.ReadBigUInt16();
                h.e_shnum = reader.ReadBigUInt16();
                h.e_shstrndx = reader.ReadBigUInt16();

                return h;
            }
        }


        public class ElfSection
        {
            public enum Type : uint
            {
                SHT_NULL = 0,
                SHT_PROGBITS,
                SHT_SYMTAB,
                SHT_STRTAB,
                SHT_RELA,
                SHT_HASH,
                SHT_DYNAMIC,
                SHT_NOTE,
                SHT_NOBITS,
                SHT_REL,
                SHT_SHLIB,
                SHT_DYNSYM,
                SHT_LOPROC = 0x70000000,
                SHT_HIPROC = 0x7fffffff,
                SHT_LOUSER = 0x80000000,
                SHT_HIUSER = 0xffffffff
            }
            [Flags]
            public enum Flags : uint
            {
                SHF_WRITE = 1,
                SHF_ALLOC = 2,
                SHF_EXECINSTR = 4,
                SHF_MASKPROC = 0xf0000000
            }
            public uint sh_name;
            public Type sh_type;
            public Flags sh_flags;
            public uint sh_addr, sh_size;
            public uint sh_link, sh_info, sh_addralign, sh_entsize;
            public string name;
            public byte[] data;

            public static ElfSection Read(BinaryReader reader)
            {
                var s = new ElfSection();

                s.sh_name = reader.ReadBigUInt32();
                s.sh_type = (Type)reader.ReadBigUInt32();
                s.sh_flags = (Flags)reader.ReadBigUInt32();
                s.sh_addr = reader.ReadBigUInt32();
                uint sh_offset = reader.ReadBigUInt32();
                s.sh_size = reader.ReadBigUInt32();
                s.sh_link = reader.ReadBigUInt32();
                s.sh_info = reader.ReadBigUInt32();
                s.sh_addralign = reader.ReadBigUInt32();
                s.sh_entsize = reader.ReadBigUInt32();

                if (s.sh_type != Type.SHT_NULL && s.sh_type != Type.SHT_NOBITS)
                {
                    long savePos = reader.BaseStream.Position;
                    reader.BaseStream.Position = sh_offset;
                    s.data = reader.ReadBytes((int) s.sh_size);
                    reader.BaseStream.Position = savePos;
                }

                return s;
            }
        }


        public enum SymBind
        {
            STB_LOCAL = 0,
            STB_GLOBAL = 1,
            STB_WEAK = 2,
            STB_LOPROC = 13,
            STB_HIPROC = 15
        }
        public enum SymType
        {
            STT_NOTYPE = 0,
            STT_OBJECT,
            STT_FUNC,
            STT_SECTION,
            STT_FILE,
            STT_LOPROC = 13,
            STT_HIPROC = 15
        }


        public enum Reloc
        {
            R_PPC_ADDR32 = 1,
            R_PPC_ADDR16_LO = 4,
            R_PPC_ADDR16_HI = 5,
            R_PPC_ADDR16_HA = 6,
            R_PPC_REL24 = 10
        }


        private ElfHeader _header;
        private List<ElfSection> _sections = new List<ElfSection>();

        public IList<ElfSection> Sections { get { return _sections; } }

        public Elf(Stream input)
        {
            var reader = new BinaryReader(input);

            _header = ElfHeader.Read(reader);

            if (_header.e_type != 1)
                throw new InvalidDataException("Only relocatable objects are supported");
            if (_header.e_machine != 0x14)
                throw new InvalidDataException("Only PowerPC is supported");


            input.Seek(_header.e_shoff, SeekOrigin.Begin);
            for (int i = 0; i < _header.e_shnum; i++)
            {
                _sections.Add(ElfSection.Read(reader));
            }

            if (_header.e_shstrndx > 0 && _header.e_shstrndx < _sections.Count)
            {
                var table = _sections[_header.e_shstrndx].data;

                for (int i = 0; i < _sections.Count; i++)
                {
                    _sections[i].name = Util.ExtractNullTerminatedString(table, (int)_sections[i].sh_name);
                }
            }
        }
    }
}
