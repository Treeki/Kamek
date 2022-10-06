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
        public readonly Word FunctionStart;
        public readonly Word Target;

        public PatchExitCommand(Word functionStart, Word target)
            : base(Ids.Branch, null)
        {
            FunctionStart = functionStart;
            Target = target;
        }

        public override void WriteArguments(BinaryWriter bw)
        {
            Target.AssertNotAmbiguous();
            bw.WriteBE(Target.Value);
        }

        public override void CalculateAddress(KamekFile file)
        {
            // Do some reasonableness checks.
            // For now, we'll only work on functions ending in a blr
            var functionSize = file.QuerySymbolSize(FunctionStart);
            if (functionSize < 4)
            {
                throw new InvalidOperationException("Function too small!");
            }
            var functionEnd = FunctionStart + (functionSize - 4);
            if (file.ReadUInt32(functionEnd) != 0x4E800020)
            {
                throw new InvalidOperationException("Function does not end in blr");
            }

            // Just to be extra sure, are there any other returns in this function?
            for (var check = FunctionStart; check < functionEnd; check += 4)
            {
                var insn = file.ReadUInt32(check);
                if ((insn & 0xFC00FFFF) == 0x4C000020)
                {
                    throw new InvalidOperationException("Function contains a return partway through");
                }
            }

            Address = functionEnd;
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

        public override IEnumerable<ulong> PackActionReplayCodes()
        {
            throw new NotImplementedException();
        }

        public override void ApplyToDol(Dol dol)
        {
            throw new NotImplementedException();
        }

        public override bool Apply(KamekFile file)
        {
            if (Address.Value.Type == file.BaseAddress.Type
                    && file.Contains(Address.Value)
                    && Address.Value.Type == Target.Type)
            {
                file.WriteUInt32(Address.Value, GenerateInstruction());
                return true;
            }

            return false;
        }


        private uint GenerateInstruction()
        {
            long delta = Target - Address.Value;
            uint insn = (Id == Ids.BranchLink) ? 0x48000001U : 0x48000000U;
            insn |= ((uint)delta & 0x3FFFFFC);
            return insn;
        }
    }
}
