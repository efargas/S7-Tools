using System;
using System.Linq;

namespace S7Tools.Services.Adapters.Plc
{
    internal static class PlcInternalHelpers
    {
        public static byte[] GetBigEndianBytes(uint value)
        {
            var b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return b;
        }

        public static byte[] EncodeWithXor(byte[] chunk)
        {
            // Find Key
            for (int k = 1; k < 256; k++)
            {
                byte key = (byte)k;
                if (chunk.Contains(key))
                {
                    continue;
                }
                if (key == chunk.Length + 2)
                {
                    continue; // Length conflict check
                }

                var res = new byte[chunk.Length + 1];
                res[0] = key;
                for (int j = 0; j < chunk.Length; j++)
                {
                    res[j + 1] = (byte)(chunk[j] ^ key);
                }
                return res;
            }
            throw new Exception("No XOR key found");
        }
    }
}
