using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Value64
    {
        private long m_value;

        public Value64(long value)
        {
            m_value = value;
        }

        public long Value { get => Interlocked.Read(ref m_value); set => Interlocked.Exchange(ref m_value, value); }

        public byte AddByte(int index, byte value)
        {
            GetParams(index, sizeof(byte), out ulong mask, out int shift);
            return (byte)AddInternal(value, mask, shift);
        }

        public long AddBytes(byte a, byte b, byte c, byte d, byte e, byte f, byte g, byte h)
            => AddMultiple(stackalloc long[] { a, b, c, d, e, f, g, h }, sizeof(byte));

        public short AddShort(int index, short value)
        {
            GetParams(index, sizeof(short), out ulong mask, out int shift);
            return (short)AddInternal(value, mask, shift);
        }

        public long AddShorts(short a, short b, short c, short d)
            => AddMultiple(stackalloc long[] { a, b, c, d }, sizeof(short));

        public int AddInt(int index, int value)
        {
            GetParams(index, sizeof(int), out ulong mask, out int shift);
            return (int)AddInternal(value, mask, shift);
        }

        public long AddInts(int a, int b)
            => AddMultiple(stackalloc long[] { a, b }, sizeof(int));

        public long AddLong(long value)
            => Interlocked.Add(ref m_value, value);

        public long CompareExchangeBits(long bits, ulong mask, long comparand)
            => CompareExchangeInternal(bits, ReadValue(comparand, mask, 0), mask, 0);

        public byte CompareExchangeByte(int index, byte value, byte comparand)
        {
            GetParams(index, sizeof(byte), out ulong mask, out int shift);
            return (byte)CompareExchangeInternal(value, comparand, mask, shift);
        }

        public short CompareExchangeShort(int index, short value, short comparand)
        {
            GetParams(index, sizeof(short), out ulong mask, out int shift);
            return (short)CompareExchangeInternal(value, comparand, mask, shift);
        }

        public int CompareExchangeInt(int index, int value, int comparand)
        {
            GetParams(index, sizeof(int), out ulong mask, out int shift);
            return (int)CompareExchangeInternal(value, comparand, mask, shift);
        }

        public long CompareExchangeLong(long value, long comparand)
            => Interlocked.CompareExchange(ref m_value, value, comparand);

        public bool ExchangeBit(int index, bool on)
        {
            GetParams(index, 1, out ulong mask, out int shift);
            return ExchangeInternal(on ? 1 : 0, mask, shift) == 1;
        }

        public long ExchangeBits(long bits, ulong mask)
            => ExchangeInternal(bits, mask, 0);

        public byte ExchangeByte(int index, byte value)
        {
            GetParams(index, sizeof(byte), out ulong mask, out int shift);
            return (byte)ExchangeInternal(value, mask, shift);
        }

        public long ExchangeBytes(byte? a, byte? b, byte? c, byte? d, byte? e, byte? f, byte? g, byte? h)
            => ExchangeMultiple(sizeof(byte), stackalloc long?[] { a, b, c, d, e, f, g, h });

        public short ExchangeShort(int index, short value)
        {
            GetParams(index, sizeof(short), out ulong mask, out int shift);
            return (short)ExchangeInternal(value, mask, shift);
        }

        public long ExchangeShorts(short? a, short? b, short? c, short? d)
            => ExchangeMultiple(sizeof(short), stackalloc long?[] { a, b, c, d });

        public int ExchangeInt(int index, int value)
        {
            GetParams(index, sizeof(int), out ulong mask, out int shift);
            return (int)ExchangeInternal(value, mask, shift);
        }

        public long ExchangeInts(int? a, int? b)
            => ExchangeMultiple(sizeof(int), stackalloc long?[] { a, b });

        public long ExchangeLong(long value)
            => Interlocked.Exchange(ref m_value, value);

        public byte IncrementByte(int index)
            => AddByte(index, 1);

        public short IncrementShort(int index)
            => AddShort(index, 1);

        public int IncrementInt(int index)
            => AddInt(index, 1);

        public long IncrementLong()
            => Interlocked.Increment(ref m_value);

        public bool ReadBit(int index)
        {
            GetParams(index, 1, out ulong mask, out int shift);
            return ReadValue(m_value, mask, shift) == 1;
        }

        public byte ReadByte(int index)
        {
            GetParams(index, sizeof(byte), out ulong mask, out int shift);
            return (byte)ReadValue(Value, mask, shift);
        }

        public short ReadShort(int index)
        {
            GetParams(index, sizeof(short), out ulong mask, out int shift);
            return (short)ReadValue(Value, mask, shift);
        }

        public int ReadInt(int index)
        {
            GetParams(index, sizeof(int), out ulong mask, out int shift);
            return (int)ReadValue(Value, mask, shift);
        }

        public long ReadLong()
            => Interlocked.Read(ref m_value);

        public bool ToggleBit(int index, bool value)
        {
            GetParams(index, 1, out ulong mask, out int shift);
            return AddInternal(value ? 1 : 0, mask, shift) == 1;
        }

        private long AddInternal(long value, ulong mask, int shift)
        {
            long prev = Value;
            while (true)
            {
                long old = ReadValue(prev, mask, shift);
                long next = WriteValue(prev, old + value, mask, shift);
                long tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return next;

                prev = tmp;
            }
        }

        private long AddMultiple(Span<long> values, int size)
        {
            GetParams(0, size, out ulong mask, out int shift);
            long prev = m_value;

            while (true)
            {
                long next = prev;
                for (int i = 0; i < values.Length; i++)
                {
                    long old = ReadValue(prev, mask, shift);
                    next = WriteValue(next, old + values[i], mask, shift);
                    ShiftParams(1, size, ref mask, ref shift);
                }

                long tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return next;

                prev = tmp;
                ShiftParams(-values.Length, size, ref mask, ref shift);
            }
        }

        private long CompareExchangeInternal(long value, long comparand, ulong mask, int shift)
        {
            long prev = Value;
            while (true)
            {
                long old = ReadValue(prev, mask, shift);
                if (old != comparand)
                    return old;

                long next = WriteValue(prev, value, mask, shift);
                long tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return old;
                prev = tmp;
            }
        }

        private long ExchangeInternal(long value, ulong mask, int shift)
        {
            long prev = this.Value;
            while (true)
            {
                long next = WriteValue(prev, value, mask, shift);
                long tmp = Interlocked.CompareExchange(ref m_value, next, prev);
                if (tmp == prev)
                    return ReadValue(tmp, mask, shift);
                prev = tmp;
            }
        }

        private long ExchangeMultiple(int size, Span<long?> data)
        {
            long value = 0;
            ulong v_mask = 0;
            GetParams(0, size, out ulong mask, out int shift);
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].HasValue)
                {
                    value = WriteValue(value, data[i].Value, mask, shift);
                    v_mask |= mask;
                }

                ShiftParams(1, size, ref mask, ref shift);
            }

            return ExchangeInternal(value, v_mask, 0);
        }

        public static long ReadBits(long value, ulong mask)
            => ReadValue(value, mask, 0);

        public static (byte, byte, byte, byte, byte, byte, byte, byte) ReadBytes(long value)
        {
            Span<byte> bytes = stackalloc byte[8];
            GetParams(0, sizeof(byte), out ulong mask, out int shift);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)ReadValue(value, mask, shift);
                ShiftParams(1, sizeof(byte), ref mask, ref shift);
            }

            return (bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], bytes[6], bytes[7]);
        }

        public static (short, short, short, short) ReadShorts(long value)
        {
            Span<short> shorts = stackalloc short[4];
            GetParams(0, sizeof(short), out ulong mask, out int shift);
            for (int i = 0; i < shorts.Length; i++)
            {
                shorts[i] = (short)ReadValue(value, mask, shift);
                ShiftParams(1, sizeof(short), ref mask, ref shift);
            }

            return (shorts[0], shorts[1], shorts[2], shorts[3]);
        }

        public static (int, int) ReadInts(long value)
        {
            Span<int> ints = stackalloc int[2];
            GetParams(0, sizeof(int), out ulong mask, out int shift);
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = (int)ReadValue(value, mask, shift);
                ShiftParams(1, sizeof(int), ref mask, ref shift);
            }

            return (ints[0], ints[1]);
        }

        private static void GetParams(int offset, int size, out ulong mask, out int shift)
        {
            size *= 8; //Adjustments of bytes to bits
            offset *= size; //Adjustments of Ts to bits, whatever T is in size

            int s = (sizeof(ulong) * 8) - size; //Initial shift
            shift = s - offset; //Calculate final shift
            mask = (ulong.MaxValue >> s) << shift; //Calculate mask from initial and final shift
        }

        private static long ReadValue(long target, ulong mask, int shift)
        {
            ulong result = (ulong)target & mask;
            return (long)(result >> shift);
        }

        private static void ShiftParams(int offset, int size, ref ulong mask, ref int shift)
        {
            int d_shift = offset * size * 8;
            shift -= d_shift;
            mask >>= d_shift;
        }

        private static long WriteValue(long target, long value, ulong mask, int shift)
        {
            ulong result = (ulong)target & ~mask;
            ulong write = (ulong)value << shift;
            write &= mask; //Ensure that we aren't writing outside our intended area

            return (long)(result | write);
        }
    }
}
