using System.Text;

namespace DR2.Formats
{
    public class Hash
    {
        public static uint Calculate(byte[] buffer, int offset, int length, uint magic)
        {
            uint hash = 0;

            for (int i = offset; i < offset + length; i++)
            {
                hash *= magic;
                hash ^= (uint)buffer[i];
            }

            return hash;
        }

        public static uint Calculate(byte[] buffer, int offset, int length)
        {
            return Calculate(buffer, offset, length, 33);
        }

        public static uint Calculate(string text, uint magic)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            return Calculate(bytes, 0, bytes.Length, magic);
        }

        public static uint Calculate(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            return Calculate(bytes, 0, bytes.Length);
        }
    }
}