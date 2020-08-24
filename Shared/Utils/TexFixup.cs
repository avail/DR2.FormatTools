using System;

namespace DR2.Utils
{
    public class TexFixup
    {
        enum TexFormats
        {
            ARGB8 = 0,
            ARGB4,
            R5G6B5,
            P8,
            P4,
            L8,
            DXT1,
            DXT3,
            DXT5,
            // and the rest of shit we don't care about
        }

        static DirectXTexUtility.DXGIFormat MapFormatRaw(byte b)
        {
            if (b == 0xE0)
            {
                return DirectXTexUtility.DXGIFormat.BC1UNORM;
            }
            if (b == 0x60 ||
                b == 0x04 /* Y */ || 
                b == 0x64 /* Y */ ||
                b == 0xE4 /* Y */)
            {
                return DirectXTexUtility.DXGIFormat.BC3UNORM;
            }

            // 
            return DirectXTexUtility.DXGIFormat.BC1UNORM;
        }

        static DirectXTexUtility.DXGIFormat MapFormat(TexFormats f)
        {
            switch (f)
            {
                case TexFormats.ARGB8:
                    return DirectXTexUtility.DXGIFormat.B8G8R8A8UNORM;
                case TexFormats.ARGB4:
                    return DirectXTexUtility.DXGIFormat.B4G4R4A4UNORM;
                case TexFormats.DXT1:
                    return DirectXTexUtility.DXGIFormat.BC3UNORM;
                case TexFormats.DXT3:
                    return DirectXTexUtility.DXGIFormat.BC2UNORM;
                case TexFormats.DXT5:
                    return DirectXTexUtility.DXGIFormat.BC1UNORM;
                default:
                    {
                        Console.WriteLine("DXT FORMAT WAS {f} AND IT IS NOT IMPLEMENTED. HOPING DXT1 WORKS");
                        return DirectXTexUtility.DXGIFormat.BC1UNORM;
                    }
            }
        }

        // data[0..3] - header
        // data[4..7] - size
        // data[8] - format enum
        // data[9] - ?
        // data[10] - mip count
        // data[16] - offset to mip length (global)
        // data[aboveResult] - end of mip formats offset (global)

        public static bool IsValidTexture(byte[] data)
        {
            if (data.Length > 32)
            {
                if (data[0] == 0x00 &&
                    data[1] == 0x01 &&
                    data[2] == 0x01 &&
                    (data[3] == 0xE0 || data[3] == 0x60 || 
                    data[3] == 0xE4 || data[3] == 0x04 || 
                    data[3] == 0x64))
                {
                    return true;
                }
            }

            return false;
        }

        public static byte[] Perform(byte[] data)
        {
            //var format = MapFormat((TexFormats)data[8]);
            var format = MapFormatRaw(data[3]);
            var mipCount = data[10];
            var width = BitConverter.ToUInt16(data, 4);

            var MATADATAHAHA = DirectXTexUtility.GenerateMataData(width, width, mipCount, format, false);

            DirectXTexUtility.DDSHeader header;
            DirectXTexUtility.DX10Header header10;

            DirectXTexUtility.GenerateDDSHeader(MATADATAHAHA, DirectXTexUtility.DDSFlags.NONE, out header, out header10);

            uint mipStartOffset = BitConverter.ToUInt32(data, 16);
            uint mipEndOffset = BitConverter.ToUInt32(data, (int)mipStartOffset);

            byte[] newHeader = DirectXTexUtility.EncodeDDSHeader(header, header10);

            byte[] output = new byte[(data.Length - mipEndOffset) + newHeader.Length];

            Array.Copy(newHeader, 0, output, 0, newHeader.Length);
            Array.Copy(data, mipEndOffset, output, newHeader.Length, data.Length - mipEndOffset);

            return output;
        }
    }
}