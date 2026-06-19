using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private Word _initStart, _initEnd;
        private Word _textStart, _textEnd;
        private Word _ctorStart, _ctorEnd;
        private Word _dtorStart, _dtorEnd;
        private Word _rodataStart, _rodataEnd;
        private Word _dataStart, _dataEnd;
        private Word _outputStart, _outputEnd;
        private Word _bssStart, _bssEnd;
        private byte[] _memory = null;

        public Word BaseAddress { get { return _baseAddress; } }
        public Word CtorStart { get { return _ctorStart; } }
        public Word CtorEnd { get { return _ctorEnd; } }
        public Word OutputStart { get { return _outputStart; } }
        public Word OutputEnd { get { return _outputEnd; } }
        public byte[] Memory { get { return _memory; } }
        public long BssSize { get { return _bssEnd - _bssStart; } }


        #region Collecting Injection Sections

        [Flags]
        public enum InjectionFlags : uint
        {
            KM_INJECT_STRIP_BLR_PAST = 1,
            KM_INJECT_ADD_PADDING = 2
        }

        public struct InjectedSection
        {
            public uint Address;
            public byte[] Data;
        }

        private List<InjectedSection> _injectedSections = new List<InjectedSection>();
        public IReadOnlyList<InjectedSection> InjectedSections { get { return _injectedSections; } }

        private void ImportInjectedSections()
        {
            foreach (var elf in _modules)
            {
                foreach (var injectionSection in (from s in elf.Sections
                                                  where s.name.StartsWith(".km_inject_") && !s.name.EndsWith("_meta")
                                                  select s))
                {
                    Elf.ElfSection metaSection = (from s in elf.Sections
                                                  where s.name == $"{injectionSection.name}_meta"
                                                  select s).Single();

                    var numValues = Util.ExtractUInt32(metaSection.data, 0);
                    if (numValues < 4)
                        continue;

                    var startAddr = Util.ExtractUInt32(metaSection.data, 4);
                    var endAddr = Util.ExtractUInt32(metaSection.data, 8);
                    var flags = (InjectionFlags)Util.ExtractUInt32(metaSection.data, 12);
                    var pad = Util.ExtractUInt32(metaSection.data, 16);

                    ProcessSectionInjection(elf, injectionSection, startAddr, endAddr, flags, pad);
                }
            }
        }

        private void ProcessSectionInjection(Elf elf, Elf.ElfSection sec, uint start, uint end, InjectionFlags flags, uint pad)
        {
            uint startPreMap = start;
            uint sizePreMap = end - start;

            start = Mapper.Remap(start);
            end = Mapper.Remap(end);

            uint sizePostMap = end - start;
            if (sizePreMap != sizePostMap)
                throw new InvalidDataException($"Injected code range at 0x{startPreMap:x} changes size when remapped (0x{sizePreMap:x} -> 0x{sizePostMap:x})");

            EnforceSectionInjectionSize(sec, start, end - start + 4, flags, pad);

            _injectedSections.Add(new InjectedSection { Address=start, Data=sec.data });
            _sectionBases[sec] = new Word(WordType.AbsoluteAddr, start);
        }

        private void EnforceSectionInjectionSize(Elf.ElfSection sec, uint start, uint requestedSize, InjectionFlags flags, uint pad)
        {
            if (sec.sh_size < requestedSize)
            {
                // Section data is too short

                if ((flags & InjectionFlags.KM_INJECT_ADD_PADDING) != 0)
                {
                    // Pad it with the user-provided pad value
                    Array.Resize(ref sec.data, (int)requestedSize);
                    for (uint offs = sec.sh_size; offs < requestedSize; offs += 4)
                        Util.InjectUInt32(sec.data, offs, pad);
                    sec.sh_size = requestedSize;
                }
            }
            else if (sec.sh_size > requestedSize)
            {
                // Section data is too long

                if ((flags & InjectionFlags.KM_INJECT_STRIP_BLR_PAST) != 0
                        && sec.sh_size == requestedSize + 4
                        && Util.ExtractUInt32(sec.data, requestedSize) == 0x4e800020)
                {
                    // Section data is too long, but by exactly one "blr" instruction. Instead of
                    // erroring, make an exception and just trim the blr instead. (This way, users
                    // don't need to put a "nofralloc" in every single kmWriteDefAsm call.)
                    Array.Resize(ref sec.data, (int)requestedSize);
                    sec.sh_size = requestedSize;
                }
                else
                {
                    throw new InvalidDataException($"Injected code at 0x{start:x} doesn't fit (0x{sec.sh_size:x} > 0x{requestedSize:x})");
                }
            }
        }
        #endregion


        #region Collecting Other Sections
        private Dictionary<Elf.ElfSection, Word> _sectionBases = new Dictionary<Elf.ElfSection, Word>();
        private List<Elf.ElfSection> _hookSections = new List<Elf.ElfSection>();

        private void ImportSections(ref List<byte[]> blobs, ref Word location, string prefix)
        {
            foreach (var elf in _modules)
            {
                foreach (var s in (from s in elf.Sections
                                   where s.name.StartsWith(prefix)
                                   select s))
                {
                    if (s.data != null)
                        blobs.Add(s.data);
                    else
                        blobs.Add(new byte[s.sh_size]);

                    _sectionBases[s] = location;
                    location += s.sh_size;

                    // Align to 4 bytes
                    if ((location.Value % 4) != 0)
                    {
                        long alignment = 4 - (location.Value % 4);
                        blobs.Add(new byte[alignment]);
                        location += alignment;
                    }
                }
            }
        }

        private void ImportHookSections()
        {
            foreach (var elf in _modules)
                foreach (var s in (from s in elf.Sections
                                   where s.name.StartsWith(".kamek")
                                   select s))
                    _hookSections.Add(s);
        }

        private void CollectSections()
        {
            List<byte[]> blobs = new List<byte[]>();
            Word location = _baseAddress;

            _outputStart = location;

            _initStart = location;
            ImportSections(ref blobs, ref location, ".init");
            _initEnd = location;

            ImportSections(ref blobs, ref location, ".fini");

            _textStart = location;
            ImportSections(ref blobs, ref location, ".text");
            _textEnd = location;

            _ctorStart = location;
            ImportSections(ref blobs, ref location, ".ctors");
            _ctorEnd = location;

            _dtorStart = location;
            ImportSections(ref blobs, ref location, ".dtors");
            _dtorEnd = location;

            _rodataStart = location;
            ImportSections(ref blobs, ref location, ".rodata");
            _rodataEnd = location;

            _dataStart = location;
            ImportSections(ref blobs, ref location, ".data");
            _dataEnd = location;

            _outputEnd = location;

            // TODO: maybe should align to 0x20 here?
            _bssStart = location;
            ImportSections(ref blobs, ref location, ".bss");
            _bssEnd = location;

            // Create one big blob from this
            _memory = new byte[location - _baseAddress];
            int position = 0;
            foreach (var blob in blobs)
            {
                Array.Copy(blob, 0, _memory, position, blob.Length);
                position += blob.Length;
            }

            ImportHookSections();
            ImportInjectedSections();
        }
        #endregion


        #region Symbol Tables
        private struct Symbol
        {
            public Word address;
            public uint size;
            public bool isWeak;
        }
        private struct SymbolName
        {
            public string name;
            public ushort shndx;
        }
        private Dictionary<string, Symbol> _globalSymbols = null;
        private Dictionary<Elf, Dictionary<string, Symbol>> _localSymbols = null;
        private Dictionary<Tuple<Elf.ElfSection, string>, uint> _hookSymbols = null;
        private Dictionary<Elf.ElfSection, SymbolName[]> _symbolTableContents = null;
        private Dictionary<string, uint> _externalSymbols = null;
        private Dictionary<Word, uint> _symbolSizes = null;
        public IReadOnlyDictionary<Word, uint> SymbolSizes { get { return _symbolSizes; } }

        private void BuildSymbolTables()
        {
            _globalSymbols = new Dictionary<string, Symbol>();
            _localSymbols = new Dictionary<Elf, Dictionary<string, Symbol>>();
            _hookSymbols = new Dictionary<Tuple<Elf.ElfSection, string>, uint>();
            _symbolTableContents = new Dictionary<Elf.ElfSection, SymbolName[]>();
            _symbolSizes = new Dictionary<Word, uint>();

            _globalSymbols["__ctor_loc"] = new Symbol { address = _ctorStart };
            _globalSymbols["__ctor_end"] = new Symbol { address = _ctorEnd };

            _globalSymbols["_f_init"] = new Symbol { address = _initStart };
            _globalSymbols["_e_init"] = new Symbol { address = _initEnd };

            _globalSymbols["_f_text"] = new Symbol { address = _textStart };
            _globalSymbols["_e_text"] = new Symbol { address = _textEnd };

            _globalSymbols["_f_ctors"] = new Symbol { address = _ctorStart };
            _globalSymbols["_e_ctors"] = new Symbol { address = _ctorEnd };

            _globalSymbols["_f_dtors"] = new Symbol { address = _dtorStart };
            _globalSymbols["_e_dtors"] = new Symbol { address = _dtorEnd };

            _globalSymbols["_f_rodata"] = new Symbol { address = _rodataStart };
            _globalSymbols["_e_rodata"] = new Symbol { address = _rodataEnd };

            _globalSymbols["_f_data"] = new Symbol { address = _dataStart };
            _globalSymbols["_e_data"] = new Symbol { address = _dataEnd };

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

        private SymbolName[] ParseSymbolTable(Elf elf, Elf.ElfSection symtab, Elf.ElfSection strtab, Dictionary<string, Symbol> locals)
        {
            if (symtab.sh_entsize != 16)
                throw new InvalidDataException("Invalid symbol table format (sh_entsize != 16)");
            if (strtab.sh_type != Elf.ElfSection.Type.SHT_STRTAB)
                throw new InvalidDataException("String table does not have type SHT_STRTAB");

            var symbolNames = new List<SymbolName>();
            var reader = new BinaryReader(new MemoryStream(symtab.data));
            int count = symtab.data.Length / 16;

            // always ignore the first symbol
            symbolNames.Add(new SymbolName());
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

                symbolNames.Add(new SymbolName { name = name, shndx = st_shndx });
                if (name.Length == 0 || st_shndx == 0)
                    continue;

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
                    if (_sectionBases.ContainsKey(section))
                        addr = _sectionBases[section] + st_value;
                    else if (_hookSections.Contains(section))
                    {
                        _hookSymbols[new Tuple<Elf.ElfSection, string>(section, name)] = st_value;
                        continue;
                    }
                    else
                        continue; // skips past symbols we don't care about, like DWARF junk
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
            if (name.StartsWith("__kAutoMap_"))
            {
                var addr = name.Substring(11);
                if (addr.StartsWith("0x") || addr.StartsWith("0X"))
                    addr = addr.Substring(2);
                var parsedAddr = uint.Parse(addr, System.Globalization.NumberStyles.AllowHexSpecifier);
                var mappedAddr = Mapper.Remap(parsedAddr);
                return new Symbol { address = new Word(WordType.AbsoluteAddr, mappedAddr) };
            }

            throw new InvalidDataException("undefined symbol " + name);
        }

        public void WriteSymbolMap(string path)
        {
            using StreamWriter file = new StreamWriter(path, false);
            file.WriteLine("Kamek Binary Map");
            file.WriteLine("  Offset   Size   Name");

            foreach (var s in _globalSymbols.OrderBy(x => x.Value.address.Value))
            {
                String name = s.Key;
                Symbol sym = s.Value;
                file.WriteLine(String.Format("  {0:X8} {1:X6} {2}", sym.address.Value, sym.size, name));
            }
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

                Word source;
                if (_sectionBases.ContainsKey(section))
                    source = _sectionBases[section] + r_offset;
                else if (_hookSections.Contains(section))
                    source = new Word(WordType.Value, r_offset);
                else
                    continue; // we don't care about this

                SymbolName symbol = _symbolTableContents[symtab][symIndex];
                string symName = symbol.name;
                //Console.WriteLine("{0,-30} {1}", symName, reloc);

                Word dest = (String.IsNullOrEmpty(symName) ? _sectionBases[elf.Sections[symbol.shndx]] : ResolveSymbol(elf, symName).address) + r_addend;

                //Console.WriteLine("Linking from 0x{0:X8} to 0x{1:X8}", source.Value, dest.Value);

                if (!KamekUseReloc(section, reloc, source.Value, dest))
                    _fixups.Add(new Fixup { type = reloc, source = source, dest = dest });
            }
        }
        #endregion


        #region Kamek Hooks
        private Dictionary<Tuple<Elf.ElfSection, uint>, Word> _hookRelocations = new Dictionary<Tuple<Elf.ElfSection, uint>, Word>();

        private bool KamekUseReloc(Elf.ElfSection section, Elf.Reloc type, uint source, Word dest)
        {
            if (!_hookSections.Contains(section))
                return false;
            if (type != Elf.Reloc.R_PPC_ADDR32)
                throw new InvalidOperationException($"Unsupported relocation type {type} in the Kamek hook data section");

            _hookRelocations[new Tuple<Elf.ElfSection, uint>(section, source)] = dest;
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
            foreach (var pair in _hookSymbols)
            {
                Elf.ElfSection section = pair.Key.Item1;
                string name = pair.Key.Item2;

                if (name.StartsWith("_kHook"))
                {
                    uint cmdOffs = pair.Value;

                    var argCount = Util.ExtractUInt32(section.data, cmdOffs);
                    var type = Util.ExtractUInt32(section.data, cmdOffs + 4);
                    var args = new Word[argCount];

                    for (uint i = 0; i < argCount; i++)
                    {
                        var argOffs = cmdOffs + (8 + (i * 4));
                        var tuple = new Tuple<Elf.ElfSection, uint>(section, argOffs);
                        if (_hookRelocations.ContainsKey(tuple))
                            args[i] = _hookRelocations[tuple];
                        else
                            args[i] = new Word(WordType.Value, Util.ExtractUInt32(section.data, argOffs));
                    }

                    _hooks.Add(new HookData { type = type, args = args });
                }
            }
        }
        #endregion
    }
}
