using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Commands
{
    class RelocCommand : Command
    {
        public readonly Word Target;

        public RelocCommand(Word source, Word target, Elf.Reloc reloc)
            : base((Ids)reloc, source)
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
            Address.AssertAbsolute();
            Target.AssertAbsolute();

            switch (Id)
            {
                case Ids.Rel24:
                    long delta = Target - Address;
                    uint insn = dol.ReadUInt32(Address.Value) & 0xFC000003;
                    insn |= ((uint)delta & 0x3FFFFFC);
                    dol.WriteUInt32(Address.Value, insn);
                    break;

                case Ids.Addr32:
                    dol.WriteUInt32(Address.Value, Target.Value);
                    break;

                case Ids.Addr16Lo:
                    dol.WriteUInt16(Address.Value, (ushort)(Target.Value & 0xFFFF));
                    break;

                case Ids.Addr16Hi:
                    dol.WriteUInt16(Address.Value, (ushort)(Target.Value >> 16));
                    break;

                case Ids.Addr16Ha:
                    ushort v = (ushort)(Target.Value >> 16);
                    if ((Target.Value & 0x8000) == 0x8000)
                        v++;
                    dol.WriteUInt16(Address.Value, v);
                    break;

                default:
                    throw new NotImplementedException("unrecognised relocation type");
            }
        }

        public override bool Apply(KamekFile file)
        {
            switch (Id)
            {
                case Ids.Rel24:
                    if ((Address.IsAbsolute && Target.IsAbsolute) || (Address.IsRelative && Target.IsRelative))
                    {
                        long delta = Target - Address;
                        uint insn = file.ReadUInt32(Address) & 0xFC000003;
                        insn |= ((uint)delta & 0x3FFFFFC);
                        file.WriteUInt32(Address, insn);

                        return true;
                    }
                    break;

                case Ids.Addr32:
                    if (Target.IsAbsolute)
                    {
                        file.WriteUInt32(Address, Target.Value);
                        return true;
                    }
                    break;

                case Ids.Addr16Lo:
                    if (Target.IsAbsolute)
                    {
                        file.WriteUInt16(Address, (ushort)(Target.Value & 0xFFFF));
                        return true;
                    }
                    break;

                case Ids.Addr16Hi:
                    if (Target.IsAbsolute)
                    {
                        file.WriteUInt16(Address, (ushort)(Target.Value >> 16));
                        return true;
                    }
                    break;

                case Ids.Addr16Ha:
                    if (Target.IsAbsolute)
                    {
                        ushort v = (ushort)(Target.Value >> 16);
                        if ((Target.Value & 0x8000) == 0x8000)
                            v++;
                        file.WriteUInt16(Address, v);
                        return true;
                    }
                    break;

                default:
                    throw new NotImplementedException("unrecognised relocation type");
            }

            return false;
        }
    }
}
