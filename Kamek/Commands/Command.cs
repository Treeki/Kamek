using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Commands
{
    abstract class Command
    {
        public enum Ids : byte
        {
            Null = 0,

            // these deliberately match the ELF relocations
            Addr32 = 1,
            Addr16Lo = 4,
            Addr16Hi = 5,
            Addr16Ha = 6,
            Rel24 = 10,

            // these are new
            WritePointer = 1, // same as Addr32 on purpose
            Write32 = 32,
            Write16 = 33,
            Write8 = 34,
            CondWritePointer = 35,
            CondWrite32 = 36,
            CondWrite16 = 37,
            CondWrite8 = 38,

            Branch = 64,
            BranchLink = 65,
        }

        public readonly Ids Id;

        private Word? _Address;
        public Word? Address
        {
            get => _Address;
            protected set
            {
                _Address = value;
            }
        }

        protected Command(Ids id, Word? address)
        {
            Id = id;
            Address = address;
        }

        public abstract void WriteArguments(BinaryWriter bw);
        public abstract string PackForRiivolution();
        public abstract string PackForDolphin();
        public abstract IEnumerable<ulong> PackGeckoCodes();
        public abstract IEnumerable<ulong> PackActionReplayCodes();
        public abstract bool Apply(KamekFile file);
        public abstract void ApplyToDol(Dol dol);

        public virtual void CalculateAddress(KamekFile file) {}

        public void AssertAddressNonNull()
        {
            if (!Address.HasValue)
                throw new NullReferenceException(string.Format("{0} command must have an address in this context", Id));
        }
    }
}
