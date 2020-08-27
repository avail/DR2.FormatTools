// TODO: decouple properly...
#if !IS_UI
using AwesomeLogger.Loggers;
using DR2.FormatTools;
#endif

using DR2.Utils;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DR2.Formats.BigFile
{
    public class BigFile
    {
        public bool HasDuplicateNames { get; private set; } = false;

        public uint Magic { get; set; }
        public uint HeaderLength { get; set; }
        public uint Unk1 { get; set; }
        public uint FileCount { get; set; }
        public uint FileTableOffset { get; set; }
        public uint Unk2 { get; set; }

        public List<Entry> Entries { get; set; }

        private long m_processElapsed = 0;
        private long m_metadataElapsed = 0;
        private long m_exportElapsed = 0;
        private Stopwatch m_stopwatch;

        public BigFile()
        {
            m_stopwatch = new Stopwatch();
        }

        private BinaryReader m_binaryReader;

        public bool Read(string fileName, bool dump = false, string dumpDirectory = "")
        {
            m_stopwatch.Restart();

            m_binaryReader = new BinaryReader(new FileStream(fileName, FileMode.Open));

            Magic = m_binaryReader.ReadUInt32();

            if (Magic != Version.Two)
            {
// TODO: decouple properly...
#if !IS_UI
                Program.FinishWithError($"Unsupported magic: {Magic:X6}");
#endif
                return false;
            }

            HeaderLength = m_binaryReader.ReadUInt32();

            if (m_binaryReader.BaseStream.Position + HeaderLength > m_binaryReader.BaseStream.Length)
            {
// TODO: decouple properly...
#if !IS_UI
                Program.FinishWithError("Incorrect header");
#endif
                return false;
            }

            Unk1 = m_binaryReader.ReadUInt32();

            FileCount = m_binaryReader.ReadUInt32();
            FileTableOffset = m_binaryReader.ReadUInt32();

            Unk2 = m_binaryReader.ReadUInt32();

            m_binaryReader.BaseStream.Seek(FileTableOffset, SeekOrigin.Begin);

            if (m_binaryReader.BaseStream.Position + (FileCount * 28) > m_binaryReader.BaseStream.Length)
            {
// TODO: decouple properly...
#if !IS_UI
                Program.FinishWithError("Incorrect file count");
#endif
                return false;
            }

            List<string> names = new List<string>();
            uint[] nameOffsets = new uint[FileCount];

            Entries = new List<Entry>();

            uint currentFile = 0;
            while (currentFile < FileCount)
            {
                nameOffsets[currentFile] = m_binaryReader.ReadUInt32();

                Entry entry = new Entry();
                entry.NameHash = m_binaryReader.ReadUInt32();

                uint size1 = m_binaryReader.ReadUInt32();
                uint size2 = m_binaryReader.ReadUInt32();

                entry.Offset = m_binaryReader.ReadUInt32();
                entry.Alignment = m_binaryReader.ReadUInt32();
                entry.Compression = (Compression)m_binaryReader.ReadUInt32();

                switch (entry.Compression)
                {
                    case Compression.Xbox:
                        entry.CompressedSize = size2;
                        entry.RawSize = size1;
                        break;

                    case Compression.Zlib:
                    case Compression.None:
                        entry.CompressedSize = size1;
                        entry.RawSize = size2;
                        break;
                }

                if (entry.CompressedSize != entry.RawSize && entry.Compression == Compression.None)
                {
// TODO: decouple properly...
#if !IS_UI
                    Program.FinishWithError("Sizes don't match on uncompressed entry");
#endif
                    return false;
                }

                Entries.Add(entry);//[currentFile] = entry;

                currentFile++;
            }

            string ReadNullTerminatedString()
            {
                string strin = "";

                char ch;
                while ((int)(ch = m_binaryReader.ReadChar()) != 0)
                {
                    strin = strin + ch;
                }

                return strin;
            }

            for (int i = 0; i < FileCount; i++)
            {
                m_binaryReader.BaseStream.Seek(nameOffsets[i], SeekOrigin.Begin);

                var strin = ReadNullTerminatedString();
                var split = strin.Split("\0");
                string name = split[0];

                Entries[i].Name = name;

                if (names.Contains(name))
                {
                    HasDuplicateNames = true;
                }
                else
                {
                    names.Add(name);
                }
            }

            m_processElapsed = m_stopwatch.ElapsedMilliseconds;
            m_stopwatch.Stop();

            if (dump)
            {
                if (!Dump(dumpDirectory, m_binaryReader))
                {
// TODO: decouple properly...
#if !IS_UI
                    Program.FinishWithError("Dumping failed. Corrupt file?");
#endif
                    return false;
                }
            }

            return true;
        }

        string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            using StringWriter sw = new StringWriter(sb);

            sw.WriteLine("[METADATA]");
            sw.WriteLine("HasDuplicateNames: " + HasDuplicateNames);
            sw.WriteLine($"Magic: {Magic:X6}");
            sw.WriteLine($"HeaderLength: {HeaderLength:X6}");
            sw.WriteLine($"Unk1: {Unk1:X6}");
            sw.WriteLine($"FileCount: {FileCount:X6}");
            sw.WriteLine($"FileTableOffset: {FileCount:X6}");
            sw.WriteLine($"Unk2: {FileCount:X6}");

            sw.WriteLine();

            foreach (var e in Entries)
            {
                sw.WriteLine("[ENTRY]");
                sw.WriteLine($"Name: {e.Name}");
                sw.WriteLine($"NameHash: {e.NameHash:X6}");
                sw.WriteLine($"CompressedSize: {e.CompressedSize}");
                sw.WriteLine($"RawSize: {e.RawSize}");
                sw.WriteLine($"Offset: {e.Offset:X6}");
                sw.WriteLine($"Alignment: {e.Alignment:X6}");
                sw.WriteLine($"Compression: {e.Compression}");

                sw.WriteLine();
            }

            sw.Flush();
            return sb.ToString();
        }

        byte[] DecompressZlib(byte[] data, int rawSize)
        {
            using MemoryStream memoryStream = new MemoryStream(data);

            using ZlibStream stream = new ZlibStream(memoryStream, CompressionMode.Decompress, CompressionLevel.Default);

            byte[] result = new byte[rawSize];

            int offset = 0;
            int b;

            while ((b = stream.ReadByte()) != -1)
            {
                result[offset++] = (byte)b;
            }

            return result;
        }

        public string ReadTextFile(string name)
        {
            var file = Entries.FirstOrDefault(f => f.Name == name);

            if (file.Compression == Compression.None)
            {
                m_binaryReader.BaseStream.Seek(file.Offset, SeekOrigin.Begin);
                var data = m_binaryReader.ReadBytes((int)file.RawSize);

                return Encoding.ASCII.GetString(data);
            }
            else if (file.Compression == Compression.Zlib)
            {
                byte[] data = new byte[file.CompressedSize];

                m_binaryReader.BaseStream.Seek(file.Offset + 4, SeekOrigin.Begin);
                data = m_binaryReader.ReadBytes((int)file.CompressedSize);

                var decompressed = DecompressZlib(data, (int)file.RawSize);

                char[] chars = new char[decompressed.Length];

                for (int i = 0; i < decompressed.Length; i++)
                {
                    chars[i] = (char)decompressed[i];
                }

                return new string(chars);
            }
            else
            {
                throw new Exception($"File compression ({file.Compression}) not supported.");
            }
        }

        bool Dump(string outputDirectory, BinaryReader str)
        {
            m_stopwatch.Restart();

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(Path.Combine(outputDirectory, "__meta.txt"), Serialize());

            m_metadataElapsed = m_stopwatch.ElapsedMilliseconds;

            object locky = new object();
            object logLocky = new object();

            m_stopwatch.Restart();

// TODO: decouple properly...
#if !IS_UI
            TitleDraw.Exporting(Program.Logger);
#endif

// imagine if debugging with async shit worked
#if true
            Parallel.ForEach(Entries, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 16
            },
#else
            Entries.ForEach(
#endif
            (entry) =>
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;

// TODO: decouple properly...
#if !IS_UI
                lock (logLocky)
                {
                    Program.UpdateConsole(threadId, entry);
                }
#endif

                string outPath = Path.Combine(outputDirectory, entry.Name);

                if (entry.Compression == Compression.None)
                {
                    byte[] data = new byte[entry.RawSize];

                    lock (locky)
                    {
                        str.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                        data = str.ReadBytes((int)entry.RawSize);
                    }

                    string suffix = "";

                    if (File.Exists(outPath) ||
                        File.Exists(outPath + ".dds"))
                    {
                        suffix += "_" + DateTime.Now.ToFileTime();
                    }

                    // ITS A DDS BOYS
                    if (TexFixup.IsValidTexture(data))
                    {
                        data = TexFixup.Perform(data);
                        suffix += ".dds";
                    }

                    File.WriteAllBytes(outPath + suffix, data);
                }
                else if (entry.Compression == Compression.Zlib)
                {
                    byte[] data = new byte[entry.CompressedSize];

                    lock (locky)
                    {
                        str.BaseStream.Seek(entry.Offset + 4, SeekOrigin.Begin);
                        data = str.ReadBytes((int)entry.CompressedSize);
                    }

                    var decompressed = DecompressZlib(data, (int)entry.RawSize);

                    string suffix = "";

                    if (File.Exists(outPath))
                    {
                        suffix += "_" + DateTime.Now.ToFileTime();
                    }
                    var ext = Path.GetExtension(outPath);
                    suffix += ext;

                    File.WriteAllBytes(outPath.Replace(ext, "") + suffix, decompressed);
                }
                else
                {
// TODO: decouple properly...
#if !IS_UI
                    Program.Logger.Info("[", Color.White);
                    Program.Logger.Info("Thread ", Color.Azure);
                    Program.Logger.Info("#" + threadId.ToString().PadLeft(2, '0'), Color.Aquamarine);
                    Program.Logger.Info("] ", Color.White);

                    Program.Logger.Info("Unsupported compression ", Color.OrangeRed);
                    Program.Logger.Info(entry.Compression.ToString(), Color.LightGreen);
                    Program.Logger.Info(" on file: ", Color.OrangeRed);

                    Program.Logger.InfoL(entry.Name, Color.BlueViolet);
#endif

                    //Console.WriteLine($"T{threadId}: Unsupported compression ({entry.Compression}) on file: {entry.Name}");
                }
            });

            m_stopwatch.Stop();
            m_exportElapsed = m_stopwatch.ElapsedMilliseconds;

// TODO: decouple properly...
#if !IS_UI
            Program.ProcessElapsed = m_processElapsed;
            Program.MetadataElapsed = m_metadataElapsed;
            Program.ExportElapsed = m_exportElapsed;
            Program.EntryCount = Entries.Count;
#endif

            //Console.WriteLine("Exported successfully.");
            //Console.WriteLine($"Processing took {m_processElapsed}ms - Metadata export took {m_jsonElapsed}ms - File export took {m_exportElapsed / 1000}s");

            return true;
        }

        public bool Write(string fileName)
        {

            return true;
        }
    }
}