using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamek
{
    public enum WordType
    {
        Value = 1,
        AbsoluteAddr = 2,
        RelativeAddr = 3
    }

    public struct Word
    {
        public readonly WordType Type;
        public readonly uint Value;

        public Word(WordType type, uint addr)
        {
            this.Type = type;
            this.Value = addr;
        }

        #region Word Arithmetic Operators
        public static Word operator +(Word a, long addend)
        {
            return new Word(a.Type, (uint)(a.Value + addend));
        }
        public static long operator -(Word a, Word b)
        {
            if (a.Type != b.Type)
                throw new InvalidOperationException("cannot perform arithmetic on different kinds of words");
            return a.Value - b.Value;
        }
        #endregion

        #region Word Comparison Operators
        public static bool operator <(Word a, Word b)
        {
            if (a.Type != b.Type)
                throw new InvalidOperationException("cannot compare different kinds of words");
            return a.Value < b.Value;
        }
        public static bool operator >(Word a, Word b)
        {
            if (a.Type != b.Type)
                throw new InvalidOperationException("cannot compare different kinds of words");
            return a.Value > b.Value;
        }
        public static bool operator <=(Word a, Word b)
        {
            if (a.Type != b.Type)
                throw new InvalidOperationException("cannot compare different kinds of words");
            return a.Value <= b.Value;
        }
        public static bool operator >=(Word a, Word b)
        {
            if (a.Type != b.Type)
                throw new InvalidOperationException("cannot compare different kinds of words");
            return a.Value >= b.Value;
        }
        #endregion

        #region Type Checking
        public bool IsAbsolute { get { return (Type == WordType.AbsoluteAddr); } }
        public bool IsRelative { get { return (Type == WordType.RelativeAddr); } }
        public bool IsValue { get { return (Type == WordType.Value); } }

        public void AssertAbsolute()
        {
            if (Type != WordType.AbsoluteAddr)
                throw new InvalidOperationException(string.Format("word {0} must be an absolute address in this context", this));
        }
        public void AssertNotRelative()
        {
            if (Type == WordType.RelativeAddr)
                throw new InvalidOperationException(string.Format("word {0} cannot be a relative address in this context", this));
        }
        #endregion

        public override string ToString()
        {
            switch (Type)
            {
                case WordType.AbsoluteAddr:
                    return string.Format("<AbsoluteAddr 0x{0:X}>", Value);
                case WordType.RelativeAddr:
                    return string.Format("<RelativeAddr Base+0x{0:X}>", Value);
                case WordType.Value:
                    return string.Format("<Word 0x{0:X}>", Value);
            }

            throw new NotImplementedException();
        }
    }


    public static class WordUtils
    {
        public static void WriteBE(this BinaryWriter bw, Word word)
        {
            bw.Write((byte)word.Type);
            bw.WriteBE((uint)word.Value);
        }
    }
}
