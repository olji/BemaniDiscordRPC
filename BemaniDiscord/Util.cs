using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace BemaniDiscord
{
    public static class Util
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
            Int64 lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        public static byte[] ReadData(IntPtr handle, long pos, int bytes)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[bytes];

            ReadProcessMemory((int)handle, pos, buffer, buffer.Length, ref bytesRead);

            return buffer;
        }
        public static Int16 BytesToInt16(byte[] input, int skip, int take = 2)
        {
            if (skip == 0)
            {
                return BitConverter.ToInt16(input.Take(take).ToArray(), 0);
            }
            return BitConverter.ToInt16(input.Skip(skip).Take(take).ToArray(), 0);
        }
        public static Int32 BytesToInt32(byte[] input, int skip, int take = 4)
        {
            if (skip == 0)
            {
                return BitConverter.ToInt32(input.Take(take).ToArray(), 0);
            }
            return BitConverter.ToInt32(input.Skip(skip).Take(take).ToArray(), 0);
        }
    }
}
