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
            Address.AssertAbsolute();
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

            if (Original.HasValue)
                throw new NotImplementedException("conditional writes not yet supported for gecko");
            if (Address.Value.Value >= 0x90000000)
                throw new NotImplementedException("MEM2 writes not yet supported for gecko");

            ulong code = ((ulong)(Address.Value.Value & 0x1FFFFFF) << 32) | Value.Value;
            switch (ValueType)
            {
                case Type.Value16: code |= 0x2000000UL << 32; break;
                case Type.Value32:
                case Type.Pointer: code |= 0x4000000UL << 32; break;
            }

            return new ulong[1] { code };
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
