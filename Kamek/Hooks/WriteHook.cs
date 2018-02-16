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
        }
    }
}
