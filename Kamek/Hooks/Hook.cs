using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Hooks
{
    abstract class Hook
    {
        public static Hook Create(Linker.HookData data)
        {
            switch (data.type)
            {
                case 1:
                    return new WriteHook(false, data.args);
                case 2:
                    return new WriteHook(true, data.args);
                case 3:
                    return new BranchHook(false, data.args);
                case 4:
                    return new BranchHook(true, data.args);
                case 5:
                    return new PatchExitHook(data.args);
                default:
                    throw new NotImplementedException("unknown command type");
            }
        }


        public readonly List<Commands.Command> Commands = new List<Commands.Command>();

        protected Word GetValueArg(Word word)
        {
            // _MUST_ be a value
            if (word.Type != WordType.Value)
                throw new InvalidDataException(string.Format("hook {0} requested a value argument, but got {1}", this, word));

            return word;
        }

        protected Word GetAbsoluteArg(Word word)
        {
            if (word.Type != WordType.AbsoluteAddr)
            {
                if (word.Type == WordType.Value)
                    return new Word(WordType.AbsoluteAddr, word.Value);
                else
                    throw new InvalidDataException(string.Format("hook {0} requested an absolute address argument, but got {1}", this, word));
            }

            return word;
        }

        protected Word GetAnyPointerArg(Word word)
        {
            switch (word.Type)
            {
                case WordType.Value:
                    return new Word(WordType.AbsoluteAddr, word.Value);
                case WordType.AbsoluteAddr:
                case WordType.RelativeAddr:
                    return word;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
