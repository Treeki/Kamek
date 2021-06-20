using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Commands
{
    class PatchExitCommand : Command
    {
        public readonly Word Target;
        public Word EndAddress;

        public PatchExitCommand(Word source, Word target)
            : base(Ids.Branch, source)
        {
            Target = target;
        }

        public override void WriteArguments(BinaryWriter bw)
        {
            EndAddress.AssertNotAmbiguous();
            Target.AssertNotAmbiguous();
            bw.WriteBE(EndAddress.Value);
            bw.WriteBE(Target.Value);
        }

        public override string PackForRiivolution()
        {
            throw new NotImplementedException();
        }

        public override string PackForDolphin()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ulong> PackGeckoCodes()
        {
            throw new NotImplementedException();
        }

        public override void ApplyToDol(Dol dol)
        {
            throw new NotImplementedException();
        }

        public override bool Apply(KamekFile file)
        {
            // Do some reasonableness checks.
            // For now, we'll only work on functions ending in a blr
            var functionSize = file.QuerySymbolSize(Address);
            if (functionSize < 4)
            {
                throw new InvalidOperationException("Function too small!");
            }
            var functionEnd = Address + (functionSize - 4);
            if (file.ReadUInt32(functionEnd) != 0x4E800020)
            {
                throw new InvalidOperationException("Function does not end in blr");
            }

            // Just to be extra sure, are there any other returns in this function?
            for (var check = Address; check < functionEnd; check += 4)
            {
                var insn = file.ReadUInt32(check);
                if ((insn & 0xFC00FFFF) == 0x4C000020)
                {
                    throw new InvalidOperationException("Function contains a return partway through");
                }
            }

            EndAddress = functionEnd;
            if (EndAddress.IsAbsolute && Target.IsAbsolute && file.Contains(Address))
            {
                file.WriteUInt32(EndAddress, GenerateInstruction());
                return true;
            }

            return false;
        }


        private uint GenerateInstruction()
        {
            long delta = Target - EndAddress;
            uint insn = (Id == Ids.BranchLink) ? 0x48000001U : 0x48000000U;
            insn |= ((uint)delta & 0x3FFFFFC);
            return insn;
        }
    }
}
