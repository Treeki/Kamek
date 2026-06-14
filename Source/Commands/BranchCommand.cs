using System;
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
        public readonly Word? Original;

        private static Ids IdFromFlags(bool isLink, bool isConditional)
        {
            if (isConditional)
                return isLink ? Ids.CondBranchLink : Ids.CondBranch;
            else
                return isLink ? Ids.BranchLink : Ids.Branch;

            throw new NotImplementedException();
        }

        public BranchCommand(Word source, Word target, Word? original, bool isLink)
            : base(IdFromFlags(isLink, original.HasValue), source)
        {
            Target = target;
            Original = original;
        }

        public override void WriteArguments(BinaryWriter bw)
        {
            Target.AssertNotAmbiguous();
            bw.WriteBE(Target.Value);

            if (Original.HasValue)
            {
                Original.Value.AssertNotRelative();
                bw.WriteBE(Original.Value.Value);
            }
        }

        public override IEnumerable<string> PackForRiivolution()
        {
            return GenerateWriteCommand().PackForRiivolution();
        }

        public override IEnumerable<string> PackForDolphin()
        {
            return GenerateWriteCommand().PackForDolphin();
        }

        public override IEnumerable<ulong> PackGeckoCodes()
        {
            return GenerateWriteCommand().PackGeckoCodes();
        }

        public override IEnumerable<ulong> PackActionReplayCodes()
        {
            return GenerateWriteCommand().PackActionReplayCodes();
        }

        public override bool Apply(KamekFile file)
        {
            if (file.Contains(Address.Value) && Address.Value.Type == Target.Type)
            {
                if (Original.HasValue && file.ReadUInt32(Address.Value) != Original.Value.Value)
                    return true;

                file.WriteUInt32(Address.Value, GenerateInstruction());
                return true;
            }

            return false;
        }

        public override void ApplyToCodeFile(CodeFiles.CodeFile file)
        {
            GenerateWriteCommand().ApplyToCodeFile(file);
        }


        private WriteCommand GenerateWriteCommand()
        {
            Target.AssertAbsolute();
            return new WriteCommand(
                Address.Value,
                new Word(WordType.Value, GenerateInstruction()),
                WriteCommand.Type.Value32,
                Original);
        }

        private uint GenerateInstruction()
        {
            long delta = Target - Address.Value;
            uint insn = IsLink() ? 0x48000001U : 0x48000000U;
            insn |= ((uint)delta & 0x3FFFFFC);
            return insn;
        }

        private bool IsLink()
        {
            return Id == Ids.BranchLink || Id == Ids.CondBranchLink;
        }
    }
}
