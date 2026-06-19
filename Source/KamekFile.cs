using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek
{
    class KamekFile
    {
        public static byte[] PackFrom(Linker linker)
        {
            var kf = new KamekFile();
            kf.LoadFromLinker(linker);
            return kf.Pack();
        }



        public struct InjectedCodeBlob
        {
            public uint Address;
            public byte[] Data;
        }

        private Word _baseAddress;
        private byte[] _codeBlob;
        private List<InjectedCodeBlob> _injectedCodeBlobs;
        private long _bssSize;
        private long _ctorStart;
        private long _ctorEnd;

        public Word BaseAddress { get { return _baseAddress; } }
        public byte[] CodeBlob { get { return _codeBlob; } }
        public IReadOnlyList<InjectedCodeBlob> InjectedCodeBlobs { get { return _injectedCodeBlobs; } }

        #region Result Binary Manipulation
        private InjectedCodeBlob? FindInjectedBlobForAddr(Word addr, uint accessSize)
        {
            if (addr.Type == WordType.AbsoluteAddr)
                foreach (var blob in _injectedCodeBlobs)
                    if (blob.Address <= addr.Value && addr.Value + accessSize <= blob.Address + blob.Data.Length)
                        return blob;
            return null;
        }

        public ushort ReadUInt16(Word addr)
        {
            InjectedCodeBlob? blob = FindInjectedBlobForAddr(addr, 2);
            if (blob != null)
                return Util.ExtractUInt16(blob.Value.Data, addr.Value - blob.Value.Address);
            else
                return Util.ExtractUInt16(_codeBlob, addr - _baseAddress);
        }
        public uint ReadUInt32(Word addr)
        {
            InjectedCodeBlob? blob = FindInjectedBlobForAddr(addr, 4);
            if (blob != null)
                return Util.ExtractUInt32(blob.Value.Data, addr.Value - blob.Value.Address);
            else
                return Util.ExtractUInt32(_codeBlob, addr - _baseAddress);
        }
        public void WriteUInt16(Word addr, ushort value)
        {
            InjectedCodeBlob? blob = FindInjectedBlobForAddr(addr, 2);
            if (blob != null)
                Util.InjectUInt16(blob.Value.Data, addr.Value - blob.Value.Address, value);
            else
                Util.InjectUInt16(_codeBlob, addr - _baseAddress, value);
        }
        public void WriteUInt32(Word addr, uint value)
        {
            InjectedCodeBlob? blob = FindInjectedBlobForAddr(addr, 4);
            if (blob != null)
                Util.InjectUInt32(blob.Value.Data, addr.Value - blob.Value.Address, value);
            else
                Util.InjectUInt32(_codeBlob, addr - _baseAddress, value);
        }

        public bool Contains(Word addr)
        {
            if (addr.Type == WordType.AbsoluteAddr)
                foreach (var blob in _injectedCodeBlobs)
                    if (blob.Address <= addr.Value && addr.Value < blob.Address + blob.Data.Length)
                        return true;

            if (addr.Type != _baseAddress.Type)
                return false;

            return (addr >= _baseAddress && addr < (_baseAddress + _codeBlob.Length));
        }

        public uint QuerySymbolSize(Word addr)
        {
            return _symbolSizes[addr];
        }
        #endregion

        private Dictionary<Word, Commands.Command> _injectionCommands;
        private Dictionary<Word, Commands.Command> _otherCommands;
        // Injection commands have to come first, since relocations are applied on top of them
        public IEnumerable<KeyValuePair<Word, Commands.Command>> _commands { get { return _injectionCommands.Concat(_otherCommands); } }

        private List<Hooks.Hook> _hooks;
        private Dictionary<Word, uint> _symbolSizes;
        private AddressMapper _mapper;

        public void LoadFromLinker(Linker linker)
        {
            if (_codeBlob != null)
                throw new InvalidOperationException("this KamekFile already has stuff in it");

            _mapper = linker.Mapper;

            // Extract _just_ the code/data sections
            _codeBlob = new byte[linker.OutputEnd - linker.OutputStart];
            Array.Copy(linker.Memory, linker.OutputStart - linker.BaseAddress, _codeBlob, 0, _codeBlob.Length);

            _injectedCodeBlobs = new List<InjectedCodeBlob>();

            foreach (var injection in linker.InjectedSections)
            {
                byte[] data = new byte[injection.Data.Length];
                Array.Copy(injection.Data, 0, data, 0, injection.Data.Length);
                _injectedCodeBlobs.Add(new InjectedCodeBlob { Address=injection.Address, Data=data });
            }

            _baseAddress = linker.BaseAddress;
            _bssSize = linker.BssSize;
            _ctorStart = linker.CtorStart - linker.OutputStart;
            _ctorEnd = linker.CtorEnd - linker.OutputStart;

            _hooks = new List<Hooks.Hook>();
            _injectionCommands = new Dictionary<Word, Commands.Command>();
            _otherCommands = new Dictionary<Word, Commands.Command>();

            _symbolSizes = new Dictionary<Word, uint>();
            foreach (var pair in linker.SymbolSizes)
                _symbolSizes.Add(pair.Key, pair.Value);

            AddRelocsAsCommands(linker.Fixups);

            foreach (var cmd in linker.Hooks)
                ApplyHook(cmd);
            ApplyStaticCommands();

            AddInjectionsAsCommands();
        }


        private void AddRelocsAsCommands(IReadOnlyList<Linker.Fixup> relocs)
        {
            foreach (var rel in relocs)
            {
                if (_otherCommands.ContainsKey(rel.source))
                    throw new InvalidOperationException(string.Format("duplicate commands for address {0}", rel.source));
                Commands.Command cmd = new Commands.RelocCommand(rel.source, rel.dest, rel.type);
                cmd.CalculateAddress(this);
                cmd.AssertAddressNonNull();
                _otherCommands[rel.source] = cmd;
            }
        }


        private void ApplyHook(Linker.HookData hookData)
        {
            var hook = Hooks.Hook.Create(hookData, _mapper);
            foreach (var cmd in hook.Commands)
            {
                cmd.CalculateAddress(this);
                cmd.AssertAddressNonNull();
                if (_otherCommands.ContainsKey(cmd.Address.Value))
                    throw new InvalidOperationException(string.Format("duplicate commands for address {0}", cmd.Address.Value));
                _otherCommands[cmd.Address.Value] = cmd;
            }
            _hooks.Add(hook);
        }


        private void ApplyStaticCommands()
        {
            // leave _otherCommands containing just the ones we couldn't apply here
            var original = _otherCommands;
            _otherCommands = new Dictionary<Word, Commands.Command>();

            foreach (var cmd in original.Values)
            {
                if (!cmd.Apply(this))
                {
                    cmd.AssertAddressNonNull();
                    _otherCommands[cmd.Address.Value] = cmd;
                }
            }
        }


        private void AddInjectionsAsCommands()
        {
            foreach (var blob in _injectedCodeBlobs)
            {
                var addr = new Word(WordType.AbsoluteAddr, blob.Address);
                Commands.WriteRangeCommand cmd = new Commands.WriteRangeCommand(addr, blob.Data);
                cmd.CalculateAddress(this);
                cmd.AssertAddressNonNull();
                _injectionCommands[cmd.Address.Value] = cmd;
            }
        }



        public byte[] Pack()
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.WriteBE((uint)0x4B616D65); // 'Kamek\0\0\3'
                    bw.WriteBE((uint)0x6B000003);
                    bw.WriteBE((uint)_bssSize);
                    bw.WriteBE((uint)_codeBlob.Length);
                    bw.WriteBE((uint)_ctorStart);
                    bw.WriteBE((uint)_ctorEnd);
                    bw.WriteBE((uint)0);
                    bw.WriteBE((uint)0);

                    bw.Write(_codeBlob);

                    foreach (var pair in _commands)
                    {
                        pair.Value.AssertAddressNonNull();
                        uint cmdID = (uint)pair.Value.Id << 24;
                        if (pair.Value.Address.Value.IsRelative)
                        {
                            if (pair.Value.Address.Value.Value > 0xFFFFFF)
                                throw new InvalidOperationException("Address too high for packed command");

                            cmdID |= pair.Value.Address.Value.Value;
                            bw.WriteBE(cmdID);
                        }
                        else
                        {
                            cmdID |= 0xFFFFFE;
                            bw.WriteBE(cmdID);
                            bw.WriteBE(pair.Value.Address.Value.Value);
                        }
                        pair.Value.WriteArguments(bw);
                    }
                }

                return ms.ToArray();
            }
        }

        public string PackRiivolution(string template, string valuefilePath)
        {
            if (_baseAddress.Type == WordType.RelativeAddr)
                throw new InvalidOperationException("cannot pack a dynamically linked binary as a Riivolution patch");

            var elements = new List<string>();

            if (_codeBlob.Length > 0)
            {
                // add the big patch
                if (valuefilePath != null)
                    elements.Add(string.Format("<memory offset=\"0x{0:X8}\" valuefile=\"{1}\" />", _baseAddress.Value, valuefilePath));

                else
                {
                    elements.AddRange(Util.PackLargeWriteForRiivolution(_baseAddress, _codeBlob));
                }
            }

            // add individual patches
            foreach (var pair in _commands)
                elements.AddRange(pair.Value.PackForRiivolution());

            return InjectLinesIntoTemplate(template, elements.ToArray(), "Riivolution XML");
        }

        public string PackDolphin(string template)
        {
            if (_baseAddress.Type == WordType.RelativeAddr)
                throw new InvalidOperationException("cannot pack a dynamically linked binary as a Dolphin patch");

            var elements = new List<string>();

            if (_codeBlob.Length > 0)
            {
                // add the big patch
                elements.AddRange(Util.PackLargeWriteForDolphin(_baseAddress, _codeBlob));
            }

            // add individual patches
            foreach (var pair in _commands)
                elements.AddRange(pair.Value.PackForDolphin());

            return InjectLinesIntoTemplate(template, elements.ToArray(), "Dolphin INI");
        }

        public string PackGeckoCodes()
        {
            if (_baseAddress.Type == WordType.RelativeAddr)
                throw new InvalidOperationException("cannot pack a dynamically linked binary as a Gecko code");

            var codes = new List<ulong>();

            if (_codeBlob.Length > 0)
            {
                // add the big patch
                codes.AddRange(Util.PackLargeWriteForGeckoCodes(_baseAddress, _codeBlob));
            }

            // add individual patches
            foreach (var pair in _commands)
                codes.AddRange(pair.Value.PackGeckoCodes());

            // convert everything
            var elements = new string[codes.Count];
            for (int i = 0; i < codes.Count; i++)
                elements[i] = string.Format("{0:X8} {1:X8}", codes[i] >> 32, codes[i] & 0xFFFFFFFF);

            return string.Join("\n", elements);
        }

        public string PackActionReplayCodes()
        {
            if (_baseAddress.Type == WordType.RelativeAddr)
                throw new InvalidOperationException("cannot pack a dynamically linked binary as an Action Replay code");

            var codes = new List<ulong>();

            if (_codeBlob.Length > 0)
            {
                // add the big patch
                codes.AddRange(Util.PackLargeWriteForActionReplayCodes(_baseAddress, _codeBlob));
            }

            // add individual patches
            foreach (var pair in _commands)
                codes.AddRange(pair.Value.PackActionReplayCodes());

            // convert everything
            var elements = new string[codes.Count];
            for (int i = 0; i < codes.Count; i++)
                elements[i] = string.Format("{0:X8} {1:X8}", codes[i] >> 32, codes[i] & 0xFFFFFFFF);

            return string.Join("\n", elements);
        }

        public void InjectIntoDol(CodeFiles.Dol dol)
        {
            if (_baseAddress.Type == WordType.RelativeAddr)
                throw new InvalidOperationException("cannot pack a dynamically linked binary into a DOL");

            if (_codeBlob.Length > 0)
            {
                // find an empty text section
                int victimSection = -1;
                for (int i = dol.Sections.Length - 1; i >= 0; i--)
                {
                    if (dol.Sections[i].Data.Length == 0)
                    {
                        victimSection = i;
                        break;
                    }
                }

                if (victimSection == -1)
                    throw new InvalidOperationException("cannot find an empty text section in the DOL");

                // throw the code blob into it
                dol.Sections[victimSection].LoadAddress = _baseAddress.Value;
                dol.Sections[victimSection].Data = _codeBlob;
            }

            // apply all patches
            foreach (var pair in _commands)
                pair.Value.ApplyToCodeFile(dol);
        }

        public void InjectIntoAlf(CodeFiles.Alf alf)
        {
            if (_baseAddress.Type == WordType.RelativeAddr)
                throw new InvalidOperationException("cannot pack a dynamically linked binary into an ALF");

            if (_codeBlob.Length > 0)
            {
                CodeFiles.Alf.Section codeSection = new CodeFiles.Alf.Section();
                codeSection.LoadAddress = _baseAddress.Value;
                codeSection.Size = (uint)_codeBlob.Length;
                codeSection.Data = _codeBlob;
                codeSection.Symbols = new List<CodeFiles.Alf.Symbol>();
                alf.Sections.Add(codeSection);
            }

            if (_bssSize > 0)
            {
                CodeFiles.Alf.Section bssSection = new CodeFiles.Alf.Section();
                bssSection.LoadAddress = (uint)(_baseAddress.Value + _codeBlob.Length);
                bssSection.Size = (uint)_bssSize;
                bssSection.Data = new byte[0];
                bssSection.Symbols = new List<CodeFiles.Alf.Symbol>();
                alf.Sections.Add(bssSection);
            }

            // apply all patches
            foreach (var pair in _commands)
                pair.Value.ApplyToCodeFile(alf);
        }

        private static string InjectLinesIntoTemplate(string template, string[] lines, string formatName)
        {
            if (template == null)
            {
                return string.Join("\n", lines);
            }
            else
            {
                int placeholderIndex = template.IndexOf("$KF$");
                if (placeholderIndex == -1)
                    throw new InvalidDataException(string.Format("\"$KF$\" placeholder not found in {0} template", formatName));
                if (template.IndexOf("$KF$", placeholderIndex + 1) != -1)
                    throw new InvalidDataException(string.Format("multiple \"$KF$\" placeholders found in {0} template", formatName));

                // If the line containing $KF$ has only whitespace before it,
                // we can use that as indentation for all of our new lines.
                // Otherwise, be conservative and don't do that.

                int placeholderLineStartIndex = template.LastIndexOf('\n', placeholderIndex) + 1;
                string placeholderLineStart = template.Substring(
                    placeholderLineStartIndex,
                    placeholderIndex - placeholderLineStartIndex);

                var joinString = "\n";
                if (placeholderLineStart.All(char.IsWhiteSpace))
                {
                    joinString = "\n" + placeholderLineStart;
                }

                return template.Replace("$KF$", string.Join(joinString, lines));
            }
        }
    }
}
