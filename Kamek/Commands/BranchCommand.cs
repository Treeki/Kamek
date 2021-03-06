﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Commands
{
    class BranchCommand : Command
    {
        public readonly Word Target;

        public BranchCommand(Word source, Word target, bool isLink)
            : base(isLink ? Ids.BranchLink : Ids.Branch, source)
        {
            Target = target;
        }

        public override void WriteArguments(BinaryWriter bw)
        {
            Target.AssertNotAmbiguous();
            bw.WriteBE(Target.Value);
        }

        public override string PackForRiivolution()
        {
            Address.AssertAbsolute();
            Target.AssertAbsolute();

            return string.Format("<memory offset='0x{0:X8}' value='{1:X8}' />", Address.Value, GenerateInstruction());
        }

        public override IEnumerable<ulong> PackGeckoCodes()
        {
            Address.AssertAbsolute();
            Target.AssertAbsolute();

            ulong code = ((ulong)(Address.Value & 0x1FFFFFF) << 32) | GenerateInstruction();
            code |= 0x4000000UL << 32;

            return new ulong[1] { code };
        }

        public override bool Apply(KamekFile file)
        {
            if (Address.IsAbsolute && Target.IsAbsolute && file.Contains(Address))
            {
                file.WriteUInt32(Address, GenerateInstruction());
                return true;
            }

            return false;
        }

        public override void ApplyToDol(Dol dol)
        {
            Address.AssertAbsolute();
            Target.AssertAbsolute();

            dol.WriteUInt32(Address.Value, GenerateInstruction());
        }


        private uint GenerateInstruction()
        {
            long delta = Target - Address;
            uint insn = (Id == Ids.BranchLink) ? 0x48000001U : 0x48000000U;
            insn |= ((uint)delta & 0x3FFFFFC);
            return insn;
        }
    }
}
