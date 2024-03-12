using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek.CodeFiles
{
    abstract class CodeFile
    {
        public abstract void Write(Stream output);
        public abstract uint ReadUInt32(uint address);
        public abstract void WriteUInt32(uint address, uint value);
        public abstract ushort ReadUInt16(uint address);
        public abstract void WriteUInt16(uint address, ushort value);
        public abstract byte ReadByte(uint address);
        public abstract void WriteByte(uint address, byte value);
    }
}
