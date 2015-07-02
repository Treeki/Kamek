using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kamek.Commands;

namespace Kamek.Hooks
{
    class WriteHook : Hook
    {
        public WriteHook(bool isConditional, Word[] args)
        {
            if (args.Length != (isConditional ? 4 : 3))
                throw new InvalidDataException("wrong arg count for WriteCommand");

            var type = (WriteCommand.Type)GetValueArg(args[0]).Value;
            Word address, value;
            Word? original = null;

            address = GetAbsoluteArg(args[1]);
            if (type == WriteCommand.Type.Pointer)
            {
                value = GetAnyPointerArg(args[2]);
                if (isConditional)
                    original = GetAnyPointerArg(args[3]);
            }
            else
            {
                value = GetValueArg(args[2]);
                if (isConditional)
                    original = GetValueArg(args[3]);
            }

            Commands.Add(new WriteCommand(address, value, type, original));

            /*
            // TODO
            // if (_type == Mode.Pointer && _value.IsAbsolute)
            //     _value = TransformGamePointer(_value);

            // Resolve the original address into a uint now
            if (isConditional)
            {
                if (args[3].Type == WordType.RelativeAddr)
                    throw new InvalidDataException("conditional WriteCommand original value cannot point within the Kamek binary");
                // TODO
                // if (_type == Mode.Pointer)
                //     call TransformGamePointer on _original, too
                Commands.Add(new WriteCommand(args[1], args[2], type, args[3].Value));
            }
            else
            {
                Commands.Add(new WriteCommand(args[1], args[2], type, null));
            }
            */
        }


        /*public void WriteBinary(BinaryWriter bw)
        {
            if (_mode != Mode.Pointer && !_value.IsAbsolute)
                throw new InvalidDataException("cannot write a relocatable pointer as a value");

            KamekFile.BinCommand command = 0;
            if (_original.HasValue)
            {
                switch (_mode)
                {
                    case Mode.Value8: command = KamekFile.BinCommand.CondWrite8; break;
                    case Mode.Value16: command = KamekFile.BinCommand.CondWrite16; break;
                    case Mode.Value32: command = KamekFile.BinCommand.CondWrite32; break;
                    case Mode.Pointer: command = KamekFile.BinCommand.CondWritePointer; break;
                }
            }
            else
            {
                switch (_mode)
                {
                    case Mode.Value8: command = KamekFile.BinCommand.Write8; break;
                    case Mode.Value16: command = KamekFile.BinCommand.Write16; break;
                    case Mode.Value32: command = KamekFile.BinCommand.Write32; break;
                    case Mode.Pointer: command = KamekFile.BinCommand.WritePointer; break;
                }
            }

            if (_addr.IsAbsolute)
                command |= KamekFile.BinCommand.AbsoluteAddrFlag;
            
            bw.Write((byte)command);
            bw.WriteBE((uint)_addr.Addr);

            switch (_mode)
            {
                case Mode.Value8: bw.Write((byte)(_value.Addr & 0xFF)); break;
                case Mode.Value16: bw.WriteBE((ushort)(_value.Addr & 0xFFFF)); break;
                case Mode.Value32:
                case Mode.Pointer: bw.WriteBE((uint)_value.Addr); break;
            }

            if (_original.HasValue)
            {
                switch (_mode)
                {
                    case Mode.Value8: bw.Write((byte)(_original.Value & 0xFF)); break;
                    case Mode.Value16: bw.WriteBE((ushort)(_original.Value & 0xFFFF)); break;
                    case Mode.Value32:
                    case Mode.Pointer: bw.WriteBE((uint)_original.Value); break;
                }
            }
        }


        public string GenerateRiivPatch()
        {
            if (!_value.IsAbsolute || !_addr.IsAbsolute)
                throw new InvalidDataException("all WriteCommand args must be absolute when generating Riivolution patch");

            if (_original.HasValue)
            {
                switch (_mode)
                {
                    case Mode.Value8: return string.Format("<memory offset='0x{0:X8}' value='{1:X2}' original='{2:X2}' />", _addr.Addr, _value.Addr, _original.Value);
                    case Mode.Value16: return string.Format("<memory offset='0x{0:X8}' value='{1:X4}' original='{2:X4}' />", _addr.Addr, _value.Addr, _original.Value);
                    case Mode.Value32:
                    case Mode.Pointer: return string.Format("<memory offset='0x{0:X8}' value='{1:X8}' original='{2:X8}' />", _addr.Addr, _value.Addr, _original.Value);
                }
            }
            else
            {
                switch (_mode)
                {
                    case Mode.Value8: return string.Format("<memory offset='0x{0:X8}' value='{1:X2}' />", _addr.Addr, _value.Addr);
                    case Mode.Value16: return string.Format("<memory offset='0x{0:X8}' value='{1:X4}' />", _addr.Addr, _value.Addr);
                    case Mode.Value32:
                    case Mode.Pointer: return string.Format("<memory offset='0x{0:X8}' value='{1:X8}' />", _addr.Addr, _value.Addr);
                }
            }

            return null;
        }*/
    }
}
