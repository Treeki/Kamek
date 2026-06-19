using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.Commands
{
    class WriteRangeCommand : Command
    {
        public readonly byte[] Value;

        public WriteRangeCommand(Word address, byte[] value)
            : base(Command.Ids.WriteRange, address)
        {
            Value = value;
        }

        public override void WriteArguments(BinaryWriter bw)
        {
            AssertValue();

            // Format (after the address):
            // - u32 length
            // - data, aligned so that you can do efficient 32-bit copies
            //   - Example: dest. addr 0x80800000 -> no padding between "length" and data blob
            //   - Example: dest. addr 0x80800003 -> data blob will be prefixed by 3 null pad bytes,
            //     so you can copy that single byte and then do 32-bit copies from 0x80800004 onward
            // - Additional pad bytes, if needed to align the next command to 4

            bw.WriteBE((uint)Value.Length);

            // Align to 4 (start)
            if (Address.Value.Value % 4 == 1)
                bw.Write(new byte[] {0});
            else if (Address.Value.Value % 4 == 2)
                bw.Write(new byte[] {0, 0});
            else if (Address.Value.Value % 4 == 3)
                bw.Write(new byte[] {0, 0, 0});

            bw.Write(Value);

            // Align to 4 (end)
            if ((Address.Value.Value + Value.Length) % 4 == 1)
                bw.Write(new byte[] {0, 0, 0});
            else if ((Address.Value.Value + Value.Length) % 4 == 2)
                bw.Write(new byte[] {0, 0});
            else if ((Address.Value.Value + Value.Length) % 4 == 3)
                bw.Write(new byte[] {0});
        }

        public override IEnumerable<string> PackForRiivolution()
        {
            Address.Value.AssertAbsolute();
            AssertValue();

            return Util.PackLargeWriteForRiivolution(Address.Value, Value);
        }

        public override IEnumerable<string> PackForDolphin()
        {
            Address.Value.AssertAbsolute();
            AssertValue();

            return Util.PackLargeWriteForDolphin(Address.Value, Value);
        }

        public override IEnumerable<ulong> PackGeckoCodes()
        {
            Address.Value.AssertAbsolute();
            AssertValue();

            if (Address.Value.Value >= 0x90000000)
                throw new NotImplementedException("MEM2 writes not yet supported for gecko");

            return Util.PackLargeWriteForGeckoCodes(Address.Value, Value);
        }

        public override IEnumerable<ulong> PackActionReplayCodes()
        {
            Address.Value.AssertAbsolute();
            AssertValue();

            if (Address.Value.Value >= 0x90000000)
                throw new NotImplementedException("MEM2 writes not yet supported for action replay");

            return Util.PackLargeWriteForActionReplayCodes(Address.Value, Value);
        }

        public override void ApplyToCodeFile(CodeFiles.CodeFile file)
        {
            Address.Value.AssertAbsolute();
            AssertValue();

            for (uint offs = 0; offs < Value.Length; offs++)
                file.WriteByte(Address.Value.Value + offs, Value[offs]);
        }

        public override bool Apply(KamekFile file)
        {
            return false;
        }

        private void AssertValue()
        {
            if (Value.Length == 0)
                throw new InvalidOperationException("WriteRangeCommand has no data to write");
        }
    }
}
