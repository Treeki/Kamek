using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Hooks
{
    class BranchHook : Hook
    {
        public BranchHook(bool isLink, Word[] args)
        {
            if (args.Length != 2)
                throw new InvalidDataException("wrong arg count for BranchCommand");

            var source = args[0];
            var dest = args[1];
        }

        /*public void WriteBinary(System.IO.BinaryWriter bw)
        {
            uint insn = _isLink ? 0x48000001U : 0x48000000U;
            
            // write a four-byte patch, followed by a fixup
            var command = KamekFile.BinCommand.Write32;
            if (_source.IsAbsolute)
                command |= KamekFile.BinCommand.AbsoluteAddrFlag;

            bw.Write((byte)command);
            bw.WriteBE((uint)_source.Addr);
            bw.WriteBE((uint)insn);

            bw.Write((byte)Elf.Reloc.R_PPC_REL24);
            bw.WriteBE((uint)_source.Addr);
            bw.WriteBE((uint)_dest.Addr);
        }

        public string GenerateRiivPatch()
        {
            uint insn = _isLink ? 0x48000001U : 0x48000000U;
            return "";
        }*/
    }
}
