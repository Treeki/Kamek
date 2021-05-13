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
        private bool _linked = false;
        private List<Elf> _modules = new List<Elf>();
        public readonly AddressMapper Mapper;

        public Linker(AddressMapper mapper)
        {
            Mapper = mapper;
        }

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
            _baseAddress = new Word(WordType.AbsoluteAddr, Mapper.Remap(baseAddress));
            DoLink(externalSymbols);
        }
        public void LinkDynamic(Dictionary<String, uint> externalSymbols)
        {
            _baseAddress = new Word(WordType.RelativeAddr, 0);
            DoLink(externalSymbols);
        }

        private void DoLink(Dictionary<String, uint> externalSymbols)
        {
            if (_linked)
                throw new InvalidOperationException("This linker has already been linked");
            _linked = true;

            _externalSymbols = new Dictionary<string, uint>();
            foreach (var pair in externalSymbols)
                _externalSymbols.Add(pair.Key, Mapper.Remap(pair.Value));

            CollectSections();
            BuildSymbolTables();
            ProcessRelocations();
            ProcessHooks();
        }



        private Word _baseAddress;
        private Word _ctorStart, _ctorEnd;
        private Word _outputStart, _outputEnd;
        private Word _bssStart, _bssEnd;
        private Word _kamekStart, _kamekEnd;
        private byte[] _memory = null;

        public Word BaseAddress { get { return _baseAddress; } }
        public Word CtorStart { get { return _ctorStart; } }
        public Word CtorEnd { get { return _ctorEnd; } }
        public Word OutputStart { get { return _outputStart; } }
        public Word OutputEnd { get { return _outputEnd; } }
        public byte[] Memory { get { return _memory; } }
        public long BssSize { get { return _bssEnd - _bssStart; } }


        #region Collecting Sections
        private List<byte[]> _binaryBlobs = new List<byte[]>();
        private Dictionary<Elf.ElfSection, Word> _sectionBases = new Dictionary<Elf.ElfSection, Word>();

        private Word _location;

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
                    if ((_location.Value % 4) != 0)
                    {
                        long alignment = 4 - (_location.Value % 4);
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

            // TODO: maybe should align to 0x20 here?
            _bssStart = _location;
            ImportSections(".bss");
            _bssEnd = _location;

            _kamekStart = _location;
            ImportSections(".kamek");
            _kamekEnd = _location;

            // Create one big blob from this
            _memory = new byte[_location - _baseAddress];
            int position = 0;
            foreach (var blob in _binaryBlobs)
            {
                Array.Copy(blob, 0, _memory, position, blob.Length);
                position += blob.Length;
            }
        }
        #endregion


        #region Result Binary Manipulation
        private ushort ReadUInt16(Word addr)
        {
            return Util.ExtractUInt16(_memory, addr - _baseAddress);
        }
        private uint ReadUInt32(Word addr)
        {
            return Util.ExtractUInt32(_memory, addr - _baseAddress);
        }
        private void WriteUInt16(Word addr, ushort value)
        {
            Util.InjectUInt16(_memory, addr - _baseAddress, value);
        }
        private void WriteUInt32(Word addr, uint value)
        {
            Util.InjectUInt32(_memory, addr - _baseAddress, value);
        }
        #endregion


        #region Symbol Tables
        private struct Symbol
        {
            public Word address;
            public uint size;
            public bool isWeak;
        }
        private Dictionary<string, Symbol> _globalSymbols = null;
        private Dictionary<Elf, Dictionary<string, Symbol>> _localSymbols = null;
        private Dictionary<Elf.ElfSection, string[]> _symbolTableContents = null;
        private Dictionary<string, uint> _externalSymbols = null;
        private Dictionary<Word, uint> _symbolSizes = null;
        public IReadOnlyDictionary<Word, uint> SymbolSizes { get { return _symbolSizes; } }

        private void BuildSymbolTables()
        {
            _globalSymbols = new Dictionary<string, Symbol>();
            _localSymbols = new Dictionary<Elf, Dictionary<string, Symbol>>();
            _symbolTableContents = new Dictionary<Elf.ElfSection, string[]>();
            _symbolSizes = new Dictionary<Word, uint>();

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

                Word addr;
                if (st_shndx == 0xFFF1)
                {
                    // Absolute symbol
                    addr = new Word(WordType.AbsoluteAddr, st_value);
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
                        _symbolSizes[addr] = st_size;
                        break;

                    case Elf.SymBind.STB_GLOBAL:
                        if (_globalSymbols.ContainsKey(name) && !_globalSymbols[name].isWeak)
                            throw new InvalidDataException("redefinition of global symbol " + name);
                        _globalSymbols[name] = new Symbol { address = addr, size = st_size };
                        _symbolSizes[addr] = st_size;
                        break;

                    case Elf.SymBind.STB_WEAK:
                        if (!_globalSymbols.ContainsKey(name))
                        {
                            _globalSymbols[name] = new Symbol { address = addr, size = st_size, isWeak = true };
                            _symbolSizes[addr] = st_size;
                        }
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
                return new Symbol { address = new Word(WordType.AbsoluteAddr, _externalSymbols[name]) };
            if (name.StartsWith("__kAutoMap_")) {
                var addr = name.Substring(11);
                if (addr.StartsWith("0x") || addr.StartsWith("0X"))
                    addr = addr.Substring(2);
                var parsedAddr = uint.Parse(addr, System.Globalization.NumberStyles.AllowHexSpecifier);
                var mappedAddr = Mapper.Remap(parsedAddr);
                return new Symbol { address = new Word(WordType.AbsoluteAddr, mappedAddr) };
            }

            throw new InvalidDataException("undefined symbol " + name);
        }
        #endregion


        #region Relocations
        public struct Fixup
        {
            public Elf.Reloc type;
            public Word source, dest;
        }
        private List<Fixup> _fixups = new List<Fixup>();
        public IReadOnlyList<Fixup> Fixups { get { return _fixups; } }

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

                Word source = _sectionBases[section] + r_offset;
                Word dest = ResolveSymbol(elf, symName).address + r_addend;

                //Console.WriteLine("Linking from {0} to {1}", source, dest);

                if (!KamekUseReloc(reloc, source, dest))
                    _fixups.Add(new Fixup { type = reloc, source = source, dest = dest });
            }
        }
        #endregion


        #region Kamek Hooks
        private Dictionary<Word, Word> _kamekRelocations = new Dictionary<Word, Word>();

        private bool KamekUseReloc(Elf.Reloc type, Word source, Word dest)
        {
            if (source < _kamekStart || source >= _kamekEnd)
                return false;
            if (type != Elf.Reloc.R_PPC_ADDR32)
                throw new InvalidOperationException("Unsupported relocation type in the Kamek hook data section");

            _kamekRelocations[source] = dest;
            return true;
        }

        public struct HookData
        {
            public uint type;
            public Word[] args;
        }

        private List<HookData> _hooks = new List<HookData>();
        public IList<HookData> Hooks { get { return _hooks; } }


        private void ProcessHooks()
        {
            foreach (var elf in _modules)
            {
                foreach (var pair in _localSymbols[elf])
                {
                    if (pair.Key.StartsWith("_kHook"))
                    {
                        var cmdAddr = pair.Value.address;

                        var argCount = ReadUInt32(cmdAddr);
                        var type = ReadUInt32(cmdAddr + 4);
                        var args = new Word[argCount];

                        for (int i = 0; i < argCount; i++)
                        {
                            var argAddr = cmdAddr + (8 + (i * 4));
                            if (_kamekRelocations.ContainsKey(argAddr))
                                args[i] = _kamekRelocations[argAddr];
                            else
                                args[i] = new Word(WordType.Value, ReadUInt32(argAddr));
                        }

                        _hooks.Add(new HookData { type = type, args = args });
                    }
                }
            }
        }
        #endregion
    }
}
