using System;
using System.Collections.Generic;
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

        public override byte[] PackArguments()
        {
            throw new NotImplementedException();
        }

        public override string PackForRiivolution()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ulong> PackGeckoCodes()
        {
            throw new NotImplementedException();
        }

        public override bool Apply(KamekFile file)
        {
            switch (Id)
            {
                case Ids.Rel24:
                    if (Address.Type == WordType.AbsoluteAddr && Target.Type == WordType.AbsoluteAddr)
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
