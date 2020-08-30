using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Value32
    {
        private volatile int m_value;

        public Value32(int value)
        {
            m_value = value;
        }

        public int Value { get => m_value; set => m_value = value; }

        public byte AddByte(int index, byte value)
        {
            GetParams(index, sizeof(byte), out uint mask, out int shift);
            return (byte)AddInternal(value, mask, shift);
        }

        public int AddBytes(byte a, byte b, byte c, byte d)
            => AddMultiple(stackalloc int[] { a, b, c, d }, sizeof(byte));

        public short AddShort(int index, short value)
        {
            GetParams(index, sizeof(short), out uint mask, out int shift);
            return (short)AddInternal(value, mask, shift);
        }

        public int AddShorts(short a, short b)
            => AddMultiple(stackalloc int[] { a, b }, sizeof(short));

        public int AddInt(int value)
            => Interlocked.Add(ref m_value, value);

        public int CompareExchangeBits(int bits, uint mask, int comparand)
            => CompareExchangeInternal(bits, ReadValue(comparand, mask, 0), mask, 0);

        public byte CompareExchangeByte(int index, byte value, byte comparand)
        {
            GetParams(index, sizeof(byte), out uint mask, out int shift);
            return (byte)CompareExchangeInternal(value, comparand, mask, shift);
        }

        public short CompareExchangeShort(int index, short value, short comparand)
        {
            GetParams(index, sizeof(short), out uint mask, out int shift);
            return (short)CompareExchangeInternal(value, comparand, mask, shift);
        }

        public int CompareExchangeInt(int value, int comparand)
            => Interlocked.CompareExchange(ref m_value, value, comparand);

        public bool ExchangeBit(int index, bool on)
        {
            GetParams(index, 1, out uint mask, out int shift);
            return ExchangeInternal(on ? 1 : 0, mask, shift) == 1;
        }

        public int ExchangeBits(int bits, uint mask)
            => ExchangeInternal(bits, mask, 0);

        public byte ExchangeByte(int index, byte value)
        {
            GetParams(index, sizeof(byte), out uint mask, out int shift);
            return (byte)ExchangeInternal(value, mask, shift);
        }

        public int ExchangeBytes(byte? a, byte? b, byte? c, byte? d)
            => ExchangeMultiple(stackalloc int?[] { a, b, c, d }, sizeof(byte));

        public short ExchangeShort(int index, short value)
        {
            GetParams(index, sizeof(short), out uint mask, out int shift);
            return (short)ExchangeInternal(value, mask, shift);
        }

        public int ExchangeShorts(short? a, short? b)
            => ExchangeMultiple(stackalloc int?[] { a, b }, sizeof(short));

        public int ExchangeInt(int value)
            => Interlocked.Exchange(ref m_value, value);

        public byte IncrementByte(int index)
            => AddByte(index, 1);

        public short IncrementShort(int index)
            => AddShort(index, 1);

        public int IncrementInt()
            => Interlocked.Increment(ref m_value);

        public bool ReadBit(int index)
        {
            GetParams(index, 1, out uint mask, out int shift);
            return ReadValue(m_value, mask, shift) == 1;
        }

        public byte ReadByte(int index)
        {
            GetParams(index, sizeof(byte), out uint mask, out int shift);
            return (byte) ReadValue(m_value, mask, shift);
        }

        public short ReadShort(int index)
        {
            GetParams(index, sizeof(short), out uint mask, out int shift);
            return (short)ReadValue(m_value, mask, shift);
        }

        public int ReadInt()
            => m_value;

        public bool ToggleBit(int index)
        {
            GetParams(index, 1, out uint mask, out int shift);
            return AddInternal(1, mask, shift) == 1;
        }

        private int AddInternal(int value, uint mask, int shift)
        {
            int prev = m_value;
            while(true)
            {
                int old = ReadValue(prev, mask, shift);
                int next = WriteValue(prev, old + value, mask, shift);
                int tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return next;

                prev = tmp;
            }
        }

        private int AddMultiple(Span<int> values, int size)
        {
            GetParams(0, size, out uint mask, out int shift);
            int prev = m_value;

            while (true)
            {
                int next = prev;
                for(int i = 0; i < values.Length; i++)
                {
                    int old = ReadValue(prev, mask, shift);
                    next = WriteValue(next, old + values[i], mask, shift);
                    ShiftParams(1, size, ref mask, ref shift);
                }

                int tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return next;

                prev = tmp;
                ShiftParams(-values.Length, size, ref mask, ref shift);
            }
        }

        private int CompareExchangeInternal(int value, int comparand, uint mask, int shift)
        {
            int prev = m_value;
            while (true)
            {
                int old = ReadValue(prev, mask, shift);
                if (old != comparand)
                    return old;

                int next = WriteValue(prev, value, mask, shift);
                int tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return old;
                prev = tmp;
            }
        }

        private int ExchangeInternal(int value, uint mask, int shift)
        {
            int prev = m_value;
            while (true)
            {
                int next = WriteValue(prev, value, mask, shift);
                int tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return ReadValue(tmp, mask, shift);
                prev = tmp;
            }
        }

        private int ExchangeMultiple(Span<int?> values, int size)
        {
            int value = 0;
            uint v_mask = 0;
            GetParams(0, size, out uint mask, out int shift);
            for(int i = 0; i < values.Length; i++)
            {
                if (values[i].HasValue)
                {
                    value = WriteValue(value, values[i].Value, mask, shift);
                    v_mask |= mask;
                }

                ShiftParams(1, size, ref mask, ref shift);
            }

            return ExchangeInternal(value, v_mask, 0);
        }

        public static int ReadBits(int value, uint mask)
            => ReadValue(value, mask, 0);

        public static (byte, byte, byte, byte) ReadBytes(int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            GetParams(0, sizeof(byte), out uint mask, out int shift);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte) ReadValue(value, mask, shift);
                ShiftParams(1, sizeof(byte), ref mask, ref shift);
            }

            return (bytes[0], bytes[1], bytes[2], bytes[3]);
        }

        public static (short, short) ReadShorts(int value)
        {
            Span<short> shorts = stackalloc short[4];
            GetParams(0, sizeof(short), out uint mask, out int shift);
            for (int i = 0; i < shorts.Length; i++)
            {
                shorts[i] = (short)ReadValue(value, mask, shift);
                ShiftParams(1, sizeof(short), ref mask, ref shift);
            }

            return (shorts[0], shorts[1]);
        }

        private static void GetParams(int offset, int size, out uint mask, out int shift)
        {
            size *= 8; //Adjustments of bytes to bits
            offset *= size; //Adjustments of Ts to bits, whatever T is in size

            int s = (sizeof(int) * 8) - size; //Initial shift
            shift = s - offset; //Calculate final shift
            mask = (uint.MaxValue >> s) << shift; //Calculate mask from initial and final shift
        }

        private static int ReadValue(int target, uint mask, int shift)
        {
            uint result = (uint) target & mask;
            return (int) (result >> shift);
        }

        private static void ShiftParams(int offset, int size, ref uint mask, ref int shift)
        {
            int d_shift = offset * size * 8;
            shift -= d_shift;
            mask >>= d_shift;
        }

        private static int WriteValue(int target, int value, uint mask, int shift)
        {
            uint result = (uint)target & ~mask;
            uint write = (uint)value << shift;
            write &= mask; //Ensure that we aren't writing outside our intended area
            return (int)(result | write);
        }
    }
}
