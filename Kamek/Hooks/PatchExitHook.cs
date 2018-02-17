using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Hooks
{
    class PatchExitHook : Hook
    {
        public PatchExitHook(Word[] args, AddressMapper mapper)
        {
            if (args.Length != 2)
                throw new InvalidDataException("PatchExitCommand requires two arguments");

            var function = args[0];
            var dest = GetAnyPointerArg(args[1], mapper);

            if (!args[1].IsValue || args[1].Value != 0)
            {
                Commands.Add(new Commands.PatchExitCommand(function, dest));
            }
        }
    }
}
