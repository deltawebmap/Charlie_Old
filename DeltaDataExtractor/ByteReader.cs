using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaDataExtractor
{
    public static class ByteReader
    {
        public static int ReadInt32(Stream s, bool littleEndian = true)
        {
            return BitConverter.ToInt32(EndianReaderHelper(s, 4, littleEndian));
        }

        private static byte[] EndianReaderHelper(Stream s, int length, bool isLittleEndian)
        {
            //Read data
            byte[] buf = new byte[length];
            s.Read(buf, 0, buf.Length);

            //Reverse
            if (BitConverter.IsLittleEndian != isLittleEndian)
                Array.Reverse(buf);

            //Return buffer
            return buf;
        }
    }
}
