using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Commands
{
    class WriteCommand : Command
    {
        public enum Type
        {
            Pointer = 1,
            Value32 = 2,
            Value16 = 3,
            Value8 = 4
        }

        private static Ids IdFromType(Type type, bool isConditional)
        {
            if (isConditional)
            {
                switch (type)
                {
                    case Type.Pointer: return Ids.CondWritePointer;
                    case Type.Value32: return Ids.CondWrite32;
                    case Type.Value16: return Ids.CondWrite16;
                    case Type.Value8: return Ids.CondWrite8;
                }
            }
            else
            {
                switch (type)
                {
                    case Type.Pointer: return Ids.WritePointer;
                    case Type.Value32: return Ids.Write32;
                    case Type.Value16: return Ids.Write16;
                    case Type.Value8: return Ids.Write8;
                }
            }

            throw new NotImplementedException();
        }



        public readonly Type ValueType;
        public readonly Word Value;
        public readonly Word? Original;

        public WriteCommand(Word address, Word value, Type valueType, Word? original)
            : base(IdFromType(valueType, original.HasValue), address)
        {
            Value = value;
            ValueType = valueType;
            Original = original;
        }

        public override void WriteArguments(BinaryWriter bw)
        {
            if (ValueType == Type.Pointer)
                Value.AssertNotAmbiguous();
            else
                Value.AssertValue();

            bw.WriteBE(Value.Value);

            if (Original.HasValue)
            {
                Original.Value.AssertNotRelative();
                bw.WriteBE(Original.Value);
            }
        }

        public override string PackForRiivolution()
        {
            Address.Value.AssertAbsolute();
            if (ValueType == Type.Pointer)
                Value.AssertAbsolute();
            else
                Value.AssertValue();

            if (Original.HasValue)
            {
                Original.Value.AssertNotRelative();

                switch (ValueType)
                {
                    case Type.Value8: return string.Format("<memory offset='0x{0:X8}' value='{1:X2}' original='{2:X2}' />", Address.Value.Value, Value.Value, Original.Value.Value);
                    case Type.Value16: return string.Format("<memory offset='0x{0:X8}' value='{1:X4}' original='{2:X4}' />", Address.Value.Value, Value.Value, Original.Value.Value);
                    case Type.Value32:
                    case Type.Pointer: return string.Format("<memory offset='0x{0:X8}' value='{1:X8}' original='{2:X8}' />", Address.Value.Value, Value.Value, Original.Value.Value);
                }
            }
            else
            {
                switch (ValueType)
                {
                    case Type.Value8: return string.Format("<memory offset='0x{0:X8}' value='{1:X2}' />", Address.Value.Value, Value.Value);
                    case Type.Value16: return string.Format("<memory offset='0x{0:X8}' value='{1:X4}' />", Address.Value.Value, Value.Value);
                    case Type.Value32:
                    case Type.Pointer: return string.Format("<memory offset='0x{0:X8}' value='{1:X8}' />", Address.Value.Value, Value.Value);
                }
            }

            return null;
        }

        public override string PackForDolphin()
        {
            Address.Value.AssertAbsolute();
            if (ValueType == Type.Pointer)
                Value.AssertAbsolute();
            else
                Value.AssertValue();

            switch (ValueType)
            {
                case Type.Value8: return string.Format("0x{0:X8}:byte:0x000000{1:X2}", Address.Value, Value.Value);
                case Type.Value16: return string.Format("0x{0:X8}:word:0x0000{1:X4}", Address.Value, Value.Value);
                case Type.Value32:
                case Type.Pointer: return string.Format("0x{0:X8}:dword:0x{1:X8}", Address.Value, Value.Value);
            }

            return null;
        }

        public override IEnumerable<ulong> PackGeckoCodes()
        {
            Address.Value.AssertAbsolute();
            if (ValueType == Type.Pointer)
                Value.AssertAbsolute();
            else
                Value.AssertValue();

            if (Address.Value.Value >= 0x90000000)
                throw new NotImplementedException("MEM2 writes not yet supported for gecko");

            ulong code = ((ulong)(Address.Value.Value & 0x1FFFFFF) << 32) | Value.Value;
            switch (ValueType)
            {
                case Type.Value16: code |= 0x2000000UL << 32; break;
                case Type.Value32:
                case Type.Pointer: code |= 0x4000000UL << 32; break;
            }

            if (Original.HasValue)
            {
                if (ValueType == Type.Pointer)
                    Original.Value.AssertAbsolute();
                else
                    Original.Value.AssertValue();

                if (ValueType == Type.Value8)
                {
                    // Gecko doesn't natively support conditional 8-bit writes,
                    // so we have to implement it manually with a code embedding PPC...

                    uint addrTop = (uint)(Address.Value.Value >> 16);
                    uint addrBtm = (uint)(Address.Value.Value & 0xFFFF);
                    uint orig = (uint)Original.Value.Value;
                    uint value = (uint)Value.Value;

                    // r0 and r3 empirically *seem* to be available, though there's zero documentation on this
                    // r4 is definitely NOT available (codehandler dies if you mess with it)
                    uint inst1 = 0x3C600000 | addrTop;  // lis r3, X
                    uint inst2 = 0x60630000 | addrBtm;  // ori r3, r3, X
                    uint inst3 = 0x88030000;            // lbz r0, 0(r3)
                    uint inst4 = 0x2C000000 | orig;     // cmpwi r0, X
                    uint inst5 = 0x4082000C;            // bne @end
                    uint inst6 = 0x38000000 | value;    // li r0, X
                    uint inst7 = 0x98030000;            // stb r0, 0(r3)
                    uint inst8 = 0x4E800020;            // @end: blr

                    return new ulong[5] {
                        (0xC0000000UL << 32) | 4,  // "4" for four lines of instruction data below
                        ((ulong)inst1 << 32) | inst2,
                        ((ulong)inst3 << 32) | inst4,
                        ((ulong)inst5 << 32) | inst6,
                        ((ulong)inst7 << 32) | inst8
                    };
                }
                else
                {
                    // Sandwich the write between "if" and "endif" codes

                    ulong if_start = ((ulong)(Address.Value.Value & 0x1FFFFFF) << 32) | Original.Value.Value;

                    switch (ValueType)
                    {
                        case Type.Value16: if_start |= 0x28000000UL << 32; break;
                        case Type.Value32:
                        case Type.Pointer: if_start |= 0x20000000UL << 32; break;
                    }

                    ulong if_end = 0xE2000001UL << 32;

                    return new ulong[3] { if_start, code, if_end };
                }

            }
            else
            {
                return new ulong[1] { code };
            }
        }

        public override IEnumerable<ulong> PackActionReplayCodes()
        {
            Address.Value.AssertAbsolute();
            if (ValueType == Type.Pointer)
                Value.AssertAbsolute();
            else
                Value.AssertValue();

            if (Address.Value.Value >= 0x90000000)
                throw new NotImplementedException("MEM2 writes not yet supported for action replay");

            ulong code = ((ulong)(Address.Value.Value & 0x1FFFFFF) << 32) | Value.Value;
            switch (ValueType)
            {
                case Type.Value16: code |= 0x2000000UL << 32; break;
                case Type.Value32:
                case Type.Pointer: code |= 0x4000000UL << 32; break;
            }

            if (Original.HasValue)
            {
                if (ValueType == Type.Pointer)
                    Original.Value.AssertAbsolute();
                else
                    Original.Value.AssertValue();

                ulong if_start = ((ulong)(Address.Value.Value & 0x1FFFFFF) << 32) | Original.Value.Value;
                switch (ValueType)
                {
                    case Type.Value8:  if_start |= 0x08000000UL << 32; break;
                    case Type.Value16: if_start |= 0x0A000000UL << 32; break;
                    case Type.Value32:
                    case Type.Pointer: if_start |= 0x0C000000UL << 32; break;
                }

                return new ulong[2] { if_start, code };
            }
            else
            {
                return new ulong[1] { code };
            }
        }

        public override void ApplyToDol(Dol dol)
        {
            Address.Value.AssertAbsolute();
            if (ValueType == Type.Pointer)
                Value.AssertAbsolute();
            else
                Value.AssertValue();

            if (Original.HasValue)
            {
                bool patchOK = false;
                switch (ValueType)
                {
                    case Type.Value8:
                        patchOK = (dol.ReadByte(Address.Value.Value) == Original.Value.Value);
                        break;
                    case Type.Value16:
                        patchOK = (dol.ReadUInt16(Address.Value.Value) == Original.Value.Value);
                        break;
                    case Type.Value32:
                    case Type.Pointer:
                        patchOK = (dol.ReadUInt32(Address.Value.Value) == Original.Value.Value);
                        break;
                }
                if (!patchOK)
                    return;
            }

            switch (ValueType)
            {
                case Type.Value8: dol.WriteByte(Address.Value.Value, (byte)Value.Value); break;
                case Type.Value16: dol.WriteUInt16(Address.Value.Value, (ushort)Value.Value); break;
                case Type.Value32:
                case Type.Pointer: dol.WriteUInt32(Address.Value.Value, Value.Value); break;
            }
        }

        public override bool Apply(KamekFile file)
        {
            return false;
        }
    }
}
