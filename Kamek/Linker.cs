using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek
{
    class Linker
    {
        public struct Address
        {
            public readonly bool IsAbsolute;
            public readonly uint Addr;

            public Address(bool IsAbsolute, uint Addr)
            {
                this.IsAbsolute = IsAbsolute;
                this.Addr = Addr;
            }

            #region Address Arithmetic Operators
            public static Address operator+(Address a, long addend)
            {
                return new Address(a.IsAbsolute, (uint)(a.Addr + addend));
            }
            public static long operator-(Address a, Address b)
            {
                if (a.IsAbsolute != b.IsAbsolute)
                    throw new InvalidOperationException("cannot perform arithmetic on an absolute and a relative address");
                return a.Addr - b.Addr;
            }
            #endregion

            #region Address Comparison Operators
            public static bool operator<(Address a, Address b)
            {
                if (a.IsAbsolute != b.IsAbsolute)
                    throw new InvalidOperationException("cannot compare an absolute and a relative address");
                return a.Addr < b.Addr;
            }
            public static bool operator>(Address a, Address b)
            {
                if (a.IsAbsolute != b.IsAbsolute)
                    throw new InvalidOperationException("cannot compare an absolute and a relative address");
                return a.Addr > b.Addr;
            }
            public static bool operator<=(Address a, Address b)
            {
                if (a.IsAbsolute != b.IsAbsolute)
                    throw new InvalidOperationException("cannot compare an absolute and a relative address");
                return a.Addr <= b.Addr;
            }
            public static bool operator>=(Address a, Address b)
            {
                if (a.IsAbsolute != b.IsAbsolute)
                    throw new InvalidOperationException("cannot compare an absolute and a relative address");
                return a.Addr >= b.Addr;
            }
            #endregion

            public override string ToString()
            {
                if (IsAbsolute)
                    return string.Format("<Address 0x{0:X}>", Addr);
                else
                    return string.Format("<Address Base+0x{0:X}>", Addr);
            }
        }


        private bool _linked = false;
        private List<Elf> _modules = new List<Elf>();

        public void AddModule(Elf elf)
        {
            if (_linked)
                throw new InvalidOperationException("This linker has already been linked");
            if (_modules.Contains(elf))
                throw new InvalidOperationException("This module is already part of this linker");

            _modules.Add(elf);
        }

        public void LinkStatic(uint baseAddress, Dictionary<string, uint> externalSymbols)
        {
            _baseAddress = new Address(true, baseAddress);
            _externalSymbols = externalSymbols;
            DoLink();

            if (_fixups.Count != 0)
                throw new InvalidOperationException("static link somehow resulted in fixups");
        }
        public void LinkDynamic(Dictionary<String, uint> externalSymbols)
        {
            _baseAddress = new Address(false, 0);
            _externalSymbols = externalSymbols;
            DoLink();
        }

        private void DoLink()
        {
            if (_linked)
                throw new InvalidOperationException("This linker has already been linked");
            _linked = true;

            CollectSections();
            BuildSymbolTables();
            ProcessRelocations();
            ExtractKamekData();
        }



        private Address _baseAddress;
        private Address _ctorStart, _ctorEnd;
        private Address _outputStart, _outputEnd;
        private Address _bssStart, _bssEnd;
        private Address _kamekStart, _kamekEnd;
        private byte[] _output = null;

        public Address BaseAddress { get { return _baseAddress; } }
        public Address CtorStart { get { return _ctorStart; } }
        public Address CtorEnd { get { return _ctorEnd; } }
        public byte[] Output { get { return _output; } }
        public long BssSize { get { return _bssEnd - _bssStart; } }


        #region Collecting Sections
        private List<byte[]> _binaryBlobs = new List<byte[]>();
        private Dictionary<Elf.ElfSection, Address> _sectionBases = new Dictionary<Elf.ElfSection, Address>();

        private Address _location;

        private void ImportSections(string prefix)
        {
            foreach (var elf in _modules)
            {
                foreach (var s in (from s in elf.Sections
                                   where s.name.StartsWith(prefix)
                                   select s))
                {
                    if (s.data != null)
                        _binaryBlobs.Add(s.data);
                    else
                        _binaryBlobs.Add(new byte[s.sh_size]);

                    _sectionBases[s] = _location;
                    _location += s.sh_size;

                    // Align to 4 bytes
                    if ((_location.Addr % 4) != 0)
                    {
                        long alignment = 4 - (_location.Addr % 4);
                        _binaryBlobs.Add(new byte[alignment]);
                        _location += alignment;
                    }
                }
            }
        }

        private void CollectSections()
        {
            _location = _baseAddress;

            _outputStart = _location;
            ImportSections(".init");
            ImportSections(".fini");
            ImportSections(".text");
            _ctorStart = _location;
            ImportSections(".ctors");
            _ctorEnd = _location;
            ImportSections(".dtors");
            ImportSections(".rodata");
            ImportSections(".data");
            _outputEnd = _location;

            _bssStart = _location;
            ImportSections(".bss");
            _bssEnd = _location;

            _kamekStart = _location;
            ImportSections(".kamek");
            _kamekEnd = _location;

            // Create one big blob from this
            _output = new byte[_location - _baseAddress];
            int position = 0;
            foreach (var blob in _binaryBlobs)
            {
                Array.Copy(blob, 0, _output, position, blob.Length);
                position += blob.Length;
            }
        }
        #endregion


        #region Result Binary Manipulation
        private ushort ReadUInt16(Address addr)
        {
            return Util.ExtractUInt16(_output, addr - _baseAddress);
        }
        private uint ReadUInt32(Address addr)
        {
            return Util.ExtractUInt32(_output, addr - _baseAddress);
        }
        private void WriteUInt16(Address addr, ushort value)
        {
            Util.InjectUInt16(_output, addr - _baseAddress, value);
        }
        private void WriteUInt32(Address addr, uint value)
        {
            Util.InjectUInt32(_output, addr - _baseAddress, value);
        }
        #endregion


        #region Symbol Tables
        private struct Symbol
        {
            public Address address;
            public uint size;
            public bool isWeak;
        }
        private Dictionary<string, Symbol> _globalSymbols = null;
        private Dictionary<Elf, Dictionary<string, Symbol>> _localSymbols = null;
        private Dictionary<Elf.ElfSection, string[]> _symbolTableContents = null;
        private Dictionary<string, uint> _externalSymbols = null;

        private void BuildSymbolTables()
        {
            _globalSymbols = new Dictionary<string, Symbol>();
            _localSymbols = new Dictionary<Elf, Dictionary<string, Symbol>>();
            _symbolTableContents = new Dictionary<Elf.ElfSection, string[]>();

            _globalSymbols["__ctor_loc"] = new Symbol { address = _ctorStart };
            _globalSymbols["__ctor_end"] = new Symbol { address = _ctorEnd };

            foreach (Elf elf in _modules)
            {
                var locals = new Dictionary<string, Symbol>();
                _localSymbols[elf] = locals;

                foreach (var s in (from s in elf.Sections
                                   where s.sh_type == Elf.ElfSection.Type.SHT_SYMTAB
                                   select s))
                {
                    // we must have a string table
                    uint strTabIdx = s.sh_link;
                    if (strTabIdx <= 0 || strTabIdx >= elf.Sections.Count)
                        throw new InvalidDataException("Symbol table is not linked to a string table");

                    var strtab = elf.Sections[(int)strTabIdx];

                    _symbolTableContents[s] = ParseSymbolTable(elf, s, strtab, locals);
                }
            }
        }

        private string[] ParseSymbolTable(Elf elf, Elf.ElfSection symtab, Elf.ElfSection strtab, Dictionary<string, Symbol> locals)
        {
            if (symtab.sh_entsize != 16)
                throw new InvalidDataException("Invalid symbol table format (sh_entsize != 16)");
            if (strtab.sh_type != Elf.ElfSection.Type.SHT_STRTAB)
                throw new InvalidDataException("String table does not have type SHT_STRTAB");

            var symbolNames = new List<string>();
            var reader = new BinaryReader(new MemoryStream(symtab.data));
            int count = symtab.data.Length / 16;

            // always ignore the first symbol
            symbolNames.Add(null);
            reader.BaseStream.Seek(16, SeekOrigin.Begin);

            for (int i = 1; i < count; i++)
            {
                // Read info from the ELF
                uint st_name = reader.ReadBigUInt32();
                uint st_value = reader.ReadBigUInt32();
                uint st_size = reader.ReadBigUInt32();
                byte st_info = reader.ReadByte();
                byte st_other = reader.ReadByte();
                ushort st_shndx = reader.ReadBigUInt16();

                Elf.SymBind bind = (Elf.SymBind)(st_info >> 4);
                Elf.SymType type = (Elf.SymType)(st_info & 0xF);

                string name = Util.ExtractNullTerminatedString(strtab.data, (int)st_name);

                symbolNames.Add(name);
                if (name.Length == 0 || st_shndx == 0)
                    continue;

                // What location is this referencing?
                Elf.ElfSection refSection;
                if (st_shndx < 0xFF00)
                    refSection = elf.Sections[st_shndx];
                else if (st_shndx == 0xFFF1) // absolute symbol
                    refSection = null;
                else
                    throw new InvalidDataException("unknown section index found in symbol table");

                Address addr;
                if (st_shndx == 0xFFF1)
                {
                    // Absolute symbol
                    addr = new Address(true, st_value);
                }
                else if (st_shndx < 0xFF00)
                {
                    // Part of a section
                    var section = elf.Sections[st_shndx];
                    if (!_sectionBases.ContainsKey(section))
                        continue; // skips past symbols we don't care about, like DWARF junk
                    addr = _sectionBases[section] + st_value;
                }
                else
                    throw new NotImplementedException("unknown section index found in symbol table");


                switch (bind)
                {
                    case Elf.SymBind.STB_LOCAL:
                        if (locals.ContainsKey(name))
                            throw new InvalidDataException("redefinition of local symbol " + name);
                        locals[name] = new Symbol { address = addr, size = st_size };
                        break;

                    case Elf.SymBind.STB_GLOBAL:
                        if (_globalSymbols.ContainsKey(name) && !_globalSymbols[name].isWeak)
                            throw new InvalidDataException("redefinition of global symbol " + name);
                        _globalSymbols[name] = new Symbol { address = addr, size = st_size };
                        break;

                    case Elf.SymBind.STB_WEAK:
                        if (!_globalSymbols.ContainsKey(name))
                            _globalSymbols[name] = new Symbol { address = addr, size = st_size, isWeak = true };
                        break;
                }
            }

            return symbolNames.ToArray();
        }


        Symbol ResolveSymbol(Elf elf, string name)
        {
            var locals = _localSymbols[elf];
            if (locals.ContainsKey(name))
                return locals[name];
            if (_globalSymbols.ContainsKey(name))
                return _globalSymbols[name];
            if (_externalSymbols.ContainsKey(name))
                return new Symbol { address = new Address(true, _externalSymbols[name]) };

            throw new InvalidDataException("undefined symbol " + name);
        }
        #endregion


        #region Relocations
        public struct Fixup
        {
            public Elf.Reloc type;
            public Address source, dest;
        }
        private List<Fixup> _fixups = new List<Fixup>();
        public IList<Fixup> Fixups { get { return _fixups; } }

        private void ProcessRelocations()
        {
            foreach (Elf elf in _modules)
            {
                foreach (var s in (from s in elf.Sections
                                   where s.sh_type == Elf.ElfSection.Type.SHT_REL
                                   select s))
                {
                    throw new InvalidDataException("OH SHIT");
                }

                foreach (var s in (from s in elf.Sections
                                   where s.sh_type == Elf.ElfSection.Type.SHT_RELA
                                   select s))
                {
                    // Get the two affected sections
                    if (s.sh_info <= 0 || s.sh_info >= elf.Sections.Count)
                        throw new InvalidDataException("Rela table is not linked to a section");
                    if (s.sh_link <= 0 || s.sh_link >= elf.Sections.Count)
                        throw new InvalidDataException("Rela table is not linked to a symbol table");

                    var affected = elf.Sections[(int)s.sh_info];
                    var symtab = elf.Sections[(int)s.sh_link];

                    ProcessRelaSection(elf, s, affected, symtab);
                }
            }
        }


        private void ProcessRelaSection(Elf elf, Elf.ElfSection relocs, Elf.ElfSection section, Elf.ElfSection symtab)
        {
            if (relocs.sh_entsize != 12)
                throw new InvalidDataException("Invalid relocs format (sh_entsize != 12)");
            if (symtab.sh_type != Elf.ElfSection.Type.SHT_SYMTAB)
                throw new InvalidDataException("Symbol table does not have type SHT_SYMTAB");

            var reader = new BinaryReader(new MemoryStream(relocs.data));
            int count = relocs.data.Length / 12;

            for (int i = 0; i < count; i++)
            {
                uint r_offset = reader.ReadBigUInt32();
                uint r_info = reader.ReadBigUInt32();
                int r_addend = reader.ReadBigInt32();

                Elf.Reloc reloc = (Elf.Reloc)(r_info & 0xFF);
                int symIndex = (int)(r_info >> 8);

                if (symIndex == 0)
                    throw new InvalidDataException("linking to undefined symbol");
                if (!_sectionBases.ContainsKey(section))
                    continue; // we don't care about this

                string symName = _symbolTableContents[symtab][symIndex];
                //Console.WriteLine("{0,-30} {1}", symName, reloc);

                Address source = _sectionBases[section] + r_offset;
                Address dest = ResolveSymbol(elf, symName).address + r_addend;

                //Console.WriteLine("Linking from {0} to {1}", source, dest);

                if (!AttemptApplyReloc(reloc, source, dest))
                {
                    // This relocation cannot be statically applied,
                    // so defer it to later
                    if (source.IsAbsolute)
                        throw new InvalidOperationException("cannot dynamically relocate an absolute address");

                    if (!KamekUseReloc(reloc, source, dest))
                    {
                        if (source >= _outputEnd)
                            throw new InvalidOperationException("cannot dynamically relocate something outside the output");

                        _fixups.Add(new Fixup { type = reloc, source = source, dest = dest });
                    }
                }
            }
        }

        private bool AttemptApplyReloc(Elf.Reloc type, Address source, Address dest)
        {
            switch (type)
            {
                case Elf.Reloc.R_PPC_REL24:
                    if (source.IsAbsolute == dest.IsAbsolute)
                    {
                        long delta = dest - source;
                        uint insn = ReadUInt32(source) & 0xFC000003;
                        insn |= ((uint)delta & 0x3FFFFFC);
                        WriteUInt32(source, insn);

                        return true;
                    }
                    break;

                case Elf.Reloc.R_PPC_ADDR32:
                    if (dest.IsAbsolute)
                    {
                        WriteUInt32(source, dest.Addr);
                        return true;
                    }
                    break;

                case Elf.Reloc.R_PPC_ADDR16_LO:
                    if (dest.IsAbsolute)
                    {
                        WriteUInt16(source, (ushort)(dest.Addr & 0xFFFF));
                        return true;
                    }
                    break;

                case Elf.Reloc.R_PPC_ADDR16_HI:
                    if (dest.IsAbsolute)
                    {
                        WriteUInt16(source, (ushort)(dest.Addr >> 16));
                        return true;
                    }
                    break;

                case Elf.Reloc.R_PPC_ADDR16_HA:
                    if (dest.IsAbsolute)
                    {
                        ushort v = (ushort)(dest.Addr >> 16);
                        if ((dest.Addr & 0x8000) == 0x8000)
                            v++;
                        WriteUInt16(source, v);
                        return true;
                    }
                    break;

                default:
                    throw new NotImplementedException("unrecognised relocation type");
            }

            return false;
        }
        #endregion


        #region Kamek Data
        private Dictionary<Address, Address> _kamekRelocations = new Dictionary<Address, Address>();

        private bool KamekUseReloc(Elf.Reloc type, Address source, Address dest)
        {
            if (source < _kamekStart || source >= _kamekEnd)
                return false;
            if (type != Elf.Reloc.R_PPC_ADDR32)
                return false;

            _kamekRelocations[source] = dest;
            return true;
        }

        // for the purposes of this function, a u32 is treated as an
        // absolute address :p
        private Address ResolveKamekPointer(Address ptr)
        {
            if (_kamekRelocations.ContainsKey(ptr))
                return _kamekRelocations[ptr];
            else
                return new Address(true, ReadUInt32(ptr));
        }


        private void ExtractKamekData()
        {
            foreach (var elf in _modules)
            {
                foreach (var pair in _localSymbols[elf])
                {
                    if (pair.Key.StartsWith("_kCmd"))
                    {
                        var type = pair.Key.Substring(5, pair.Key.IndexOf('_', 5) - 5);
                        var cmd = pair.Value.address;
                        switch (type)
                        {
                            case "Write32":
                                Address addr = ResolveKamekPointer(cmd);
                                Address value = ResolveKamekPointer(cmd + 4);
                                //Console.WriteLine("Writing {1:X} to {0:X}", addr, value);
                                break;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
