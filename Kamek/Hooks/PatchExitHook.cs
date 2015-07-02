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
        public PatchExitHook(Word[] args)
        {
            if (args.Length != 2)
                throw new InvalidDataException("PatchExitCommand requires two arguments");

            var function = args[0];
            var dest = args[1];

            if (dest.Type != WordType.Value || dest.Value != 0)
            {
                // boop
                throw new NotImplementedException();
            }
        }
    }
}
