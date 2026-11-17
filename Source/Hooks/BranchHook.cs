using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Hooks
{
    class BranchHook : Hook
    {
        public BranchHook(bool isLink, bool isConditional, Word[] args, AddressMapper mapper)
        {
            if (args.Length != (isConditional ? 3 : 2))
                throw new InvalidDataException("wrong arg count for BranchCommand");

            // expected args:
            //   source   : pointer to game code
            //   dest     : pointer to game code or to Kamek code
            //   original : value (an encoded PPC instruction, probably)
            var source = GetAbsoluteArg(args[0], mapper);
            var dest = GetAnyPointerArg(args[1], mapper);
            Word? original = isConditional ? GetValueArg(args[2]) : null;

            Commands.Add(new Commands.BranchCommand(source, dest, original, isLink));
        }
    }
}
