namespace DR2.Formats.BigFile
{
    public class Entry
    {
        public Entry()
        {

        }

        public string Name { get; set; }
        public uint NameHash { get; set; }
        public uint CompressedSize { get; set; }
        public uint RawSize { get; set; }
        public uint Offset { get; set; }
        public uint Alignment { get; set; }
        public Compression Compression { get; set; }
    }
}