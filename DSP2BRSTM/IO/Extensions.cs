using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DSP2BRSTM.IO
{
    public static class Extensions
    {
        public const byte DecimalSignBit = 128;

        public static byte[] GetBytes(this byte[] ba, int index, int count)
        {
            index = Math.Min(Math.Max(0, index), ba.Length);
            count = Math.Min(ba.Length - index, Math.Max(0, count));

            var result = new byte[count];
            for (int i = 0; i < count; i++)
                result[i] = ba[index + i];

            return result;
        }
    }

    public class BitConverterExt
    {
        public static byte[] GetBytes(decimal value)
        {
            var bits = decimal.GetBits(value);
            var bytes = new List<byte>();

            foreach (var i in bits)
                bytes.AddRange(BitConverter.GetBytes(i));

            return bytes.ToArray();
        }

        public static decimal ToDecimal(byte[] value)
        {
            if (value.Length != 16)
                throw new Exception("A decimal must be created from exactly 16 bytes");

            var bits = new int[4];
            for (var i = 0; i <= 15; i += 4)
                bits[i / 4] = BitConverter.ToInt32(value, i);

            return new decimal(bits);
        }
    }
}
