namespace S7Tools.Services.Adapters.Plc
{
    /// <summary>
    /// Represents the PlcInternalHelpers.
    /// </summary>
    internal static class PlcInternalHelpers
    {
        /// <summary>
        /// Executes the GetBigEndianBytes operation.
        /// </summary>
        public static byte[] GetBigEndianBytes(uint value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return b;
        }

        /// <summary>
        /// Executes the EncodeWithXor operation.
        /// </summary>
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

                byte[] res = new byte[chunk.Length + 1];
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
