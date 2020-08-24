using AwesomeLogger.Loggers;
using DR2.FormatTools;
using DR2.Utils;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        public bool Read(string fileName, bool dump = false, string dumpDirectory = "")
        {
            m_stopwatch.Restart();

            using BinaryReader str = new BinaryReader(new FileStream(fileName, FileMode.Open));

            Magic = str.ReadUInt32();

            if (Magic != Version.Two)
            {
                Program.FinishWithError($"Unsupported magic: {Magic:X6}");
                return false;
            }

            HeaderLength = str.ReadUInt32();

            if (str.BaseStream.Position + HeaderLength > str.BaseStream.Length)
            {
                Program.FinishWithError("Incorrect header");
                return false;
            }

            Unk1 = str.ReadUInt32();

            FileCount = str.ReadUInt32();
            FileTableOffset = str.ReadUInt32();

            Unk2 = str.ReadUInt32();

            str.BaseStream.Seek(FileTableOffset, SeekOrigin.Begin);

            if (str.BaseStream.Position + (FileCount * 28) > str.BaseStream.Length)
            {
                Program.FinishWithError("Incorrect file count");
                return false;
            }

            List<string> names = new List<string>();
            uint[] nameOffsets = new uint[FileCount];

            Entries = new List<Entry>();

            uint currentFile = 0;
            while (currentFile < FileCount)
            {
                nameOffsets[currentFile] = str.ReadUInt32();

                Entry entry = new Entry();
                entry.NameHash = str.ReadUInt32();

                uint size1 = str.ReadUInt32();
                uint size2 = str.ReadUInt32();

                entry.Offset = str.ReadUInt32();
                entry.Alignment = str.ReadUInt32();
                entry.Compression = (Compression)str.ReadUInt32();

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
                    Program.FinishWithError("Sizes don't match on uncompressed entry");
                    return false;
                }

                Entries.Add(entry);//[currentFile] = entry;

                currentFile++;
            }

            string ReadNullTerminatedString()
            {
                string strin = "";

                char ch;
                while ((int)(ch = str.ReadChar()) != 0)
                {
                    strin = strin + ch;
                }

                return strin;
            }

            for (int i = 0; i < FileCount; i++)
            {
                str.BaseStream.Seek(nameOffsets[i], SeekOrigin.Begin);

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
                if (!Dump(dumpDirectory, str))
                {
                    Program.FinishWithError("Dumping failed. Corrupt file?");
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

        bool Dump(string outputDirectory, BinaryReader str)
        {
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

            m_stopwatch.Restart();

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(Path.Combine(outputDirectory, "__meta.txt"), Serialize());

            m_metadataElapsed = m_stopwatch.ElapsedMilliseconds;

#if SLOW_BOI
            object locky = new object();
#endif
            object logLocky = new object();

            m_stopwatch.Restart();

            TitleDraw.Exporting(Program.Logger);

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

                lock (logLocky)
                {
                    Program.UpdateConsole(threadId, entry);
                }

                string outPath = Path.Combine(outputDirectory, entry.Name);

                if (entry.Compression == Compression.None)
                {
                    byte[] data = new byte[entry.RawSize];

#if SLOW_BOI
                    lock (locky)
                    {
                        str.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                        data = str.ReadBytes((int)entry.RawSize);
                    }
#else
                    byte[] tempFullData = new byte[str.BaseStream.Length];
                    str.BaseStream.CopyToAsync();
                    str.BaseStream.Seek(0, SeekOrigin.Begin);
                    str.BaseStream.CopyTo(ms);
                    Buffer.BlockCopy(ms.ToArray(), (int)entry.Offset, data, 0, (int)entry.RawSize);
#endif

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

#if SLOW_BOI
                    lock (locky)
                    {
                        str.BaseStream.Seek(entry.Offset + 4, SeekOrigin.Begin);
                        data = str.ReadBytes((int)entry.CompressedSize);
                    }
#else
                    using MemoryStream ms = new MemoryStream();
                    str.BaseStream.Seek(0, SeekOrigin.Begin);
                    str.BaseStream.CopyTo(ms);
                    Buffer.BlockCopy(ms.ToArray(), (int)entry.Offset, data, 0, (int)entry.CompressedSize);
#endif

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
                    Program.Logger.Info("[", Color.White);
                    Program.Logger.Info("Thread ", Color.Azure);
                    Program.Logger.Info("#" + threadId.ToString().PadLeft(2, '0'), Color.Aquamarine);
                    Program.Logger.Info("] ", Color.White);

                    Program.Logger.Info("Unsupported compression ", Color.OrangeRed);
                    Program.Logger.Info(entry.Compression.ToString(), Color.LightGreen);
                    Program.Logger.Info(" on file: ", Color.OrangeRed);

                    Program.Logger.InfoL(entry.Name, Color.BlueViolet);

                    //Console.WriteLine($"T{threadId}: Unsupported compression ({entry.Compression}) on file: {entry.Name}");
                }
            });

            m_stopwatch.Stop();
            m_exportElapsed = m_stopwatch.ElapsedMilliseconds;

            Program.ProcessElapsed = m_processElapsed;
            Program.MetadataElapsed = m_metadataElapsed;
            Program.ExportElapsed = m_exportElapsed;
            Program.EntryCount = Entries.Count;

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