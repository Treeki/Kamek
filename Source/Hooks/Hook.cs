using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Hooks
{
    public enum HookType : uint {
        kctWrite = 1,
        kctConditionalWrite = 2,
        kctInjectBranch = 3,
        kctInjectCall = 4,
        kctPatchExit = 5,
        kctConditionalInjectBranch = 6,
        kctConditionalInjectCall = 7
    }

    abstract class Hook
    {
        public static Hook Create(Linker.HookData data, AddressMapper mapper)
        {
            switch (data.type)
            {
                case (uint)HookType.kctWrite:
                    return new WriteHook(false, data.args, mapper);
                case (uint)HookType.kctConditionalWrite:
                    return new WriteHook(true, data.args, mapper);
                case (uint)HookType.kctInjectBranch:
                    return new BranchHook(false, false, data.args, mapper);
                case (uint)HookType.kctInjectCall:
                    return new BranchHook(true, false, data.args, mapper);
                case (uint)HookType.kctConditionalInjectBranch:
                    return new BranchHook(false, true, data.args, mapper);
                case (uint)HookType.kctConditionalInjectCall:
                    return new BranchHook(true, true, data.args, mapper);
                case (uint)HookType.kctPatchExit:
                    return new PatchExitHook(data.args, mapper);
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

        protected Word GetAbsoluteArg(Word word, AddressMapper mapper)
        {
            if (word.Type != WordType.AbsoluteAddr)
            {
                if (word.Type == WordType.Value)
                    return new Word(WordType.AbsoluteAddr, mapper.Remap(word.Value));
                else
                    throw new InvalidDataException(string.Format("hook {0} requested an absolute address argument, but got {1}", this, word));
            }

            return word;
        }

        protected Word GetAnyPointerArg(Word word, AddressMapper mapper)
        {
            switch (word.Type)
            {
                case WordType.Value:
                    return new Word(WordType.AbsoluteAddr, mapper.Remap(word.Value));
                case WordType.AbsoluteAddr:
                case WordType.RelativeAddr:
                    return word;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
