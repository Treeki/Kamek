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
        public BranchHook(bool isLink, Word[] args)
        {
            if (args.Length != 2)
                throw new InvalidDataException("wrong arg count for BranchCommand");

            var source = GetAbsoluteArg(args[0]);
            var dest = GetAnyPointerArg(args[1]);

            Commands.Add(new Commands.BranchCommand(source, dest, isLink));
        }
    }
}
