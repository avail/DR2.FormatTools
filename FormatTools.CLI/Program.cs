using AwesomeLogger.Loggers;
using DR2.Formats.BigFile;
using DR2.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace DR2.FormatTools
{
    class Program
    {
        public static LogConsole Logger;

        public static void FinishWithSuccess(long process, long metadata, long export, int entries)
        {
            Console.Clear();

            TitleDraw.Complete(Logger);

            Logger.Info(" - ", Color.OrangeRed);

            Logger.Info("Processing took ", Color.White);
            Logger.Info(process.ToString(), Color.Aquamarine);
            Logger.Info("ms", Color.LightGoldenrodYellow);
            Logger.Info(" - ", Color.OrangeRed);

            Logger.Info("Metadata export took ", Color.White);
            Logger.Info(metadata.ToString(), Color.Aquamarine);
            Logger.Info("ms", Color.LightGoldenrodYellow);
            Logger.Info(" - ", Color.OrangeRed);

            Logger.Info("File export took ", Color.White);
            Logger.Info($"{export / 1000}", Color.Aquamarine);
            Logger.Info("s", Color.Goldenrod);

            Logger.InfoL(" - ", Color.OrangeRed);

            Logger.Info(" - ", Color.OrangeRed);

            Logger.Info("Exported ", Color.White);
            Logger.Info(entries.ToString(), Color.Aquamarine);
            Logger.Info(" files.", Color.White);

            Logger.InfoL(" - ", Color.OrangeRed);

            Console.ResetColor();
            Console.WriteLine("Press any key to exit.");

            Console.ReadKey();
            Environment.Exit(0);
        }

        public static void FinishWithError(string message, bool immediate = false)
        {
            Console.Clear();

            TitleDraw.DumpFailed(Logger);

            Logger.ErrorL(message, Color.OrangeRed);

            Console.ResetColor();
            Console.WriteLine("Press any key to exit.");

            if (!immediate)
            {            
                Console.ReadKey();
            }
            Environment.Exit(1);
        }

        public static long ProcessElapsed;
        public static long MetadataElapsed;
        public static long ExportElapsed;
        public static int EntryCount;
        private static bool ms_isTerminating = false;

        static string MappedCompression(Compression comp)
        {
            switch (comp)
            {
                case Compression.None:
                    return "NONE";
                case Compression.Xbox:
                    return "XBOX";
                default:
                case Compression.Zlib:
                    return "ZLIB";
            }
        }

        static List<int> tidMap = new List<int>();

        static int MappedTid(int tid)
        {
            if (tidMap.Contains(tid))
            {
                return tidMap.IndexOf(tid) + 1;//.IndexOf(tid) + 1;
            }

            tidMap.Add(tid);

            return tidMap.IndexOf(tid) + 1;
        }

        public static void UpdateConsole(int threadId, Entry entry)
        {
            if (ms_isTerminating)
            {
                return;
            }

            //Console.Clear();
            Console.SetCursorPosition(0, 8);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 8);

            Console.SetCursorPosition(0, 8 + MappedTid(threadId));

            Logger.Info("[", Color.White);
            Logger.Info("Thread ", Color.Azure);
            Logger.Info("#" + threadId.ToString().PadLeft(2, '0'), Color.Aquamarine);
            Logger.Info("] Processing compressed (", Color.White);

            Logger.Info(MappedCompression(entry.Compression), Color.LightGreen);
            Logger.Info(") file ", Color.White);

            Logger.Info(entry.Name.PadRight(70, ' '), Color.BlueViolet);

            Logger.Info(" - size ", Color.White);

            Logger.Info(entry.CompressedSize.ToString().PadLeft(8, ' '), Color.LightYellow);
            Logger.Info(" -> ", Color.White);
            Logger.InfoL(entry.RawSize.ToString(), Color.LightGoldenrodYellow);
        }

        public static void Main(string[] args)
        {
            Logger = new LogConsole(null, new LogOptions()
            {
                LogTimestamp = false
            });

            Logger.ClearPrependers();

            var origVisible = Console.CursorVisible;
            var origWidth = Console.WindowWidth;
            var origHeight = Console.WindowHeight;

            Console.CursorVisible = false;
            Console.CancelKeyPress += (s, e) =>
            {
                ms_isTerminating = true;
                e.Cancel = true;
                FinishWithError("User terminated execution.", true);
            };

            if (origWidth < 160)
            {
                Console.WindowWidth = 160;
            }

            if (origHeight < 8 + 16)
            {
                Console.WindowHeight = 8 + 16;
            }

            Console.Clear();

            void DrawCentered(string text, Color col)
            {
                Logger.Info(new string(' ', (Console.WindowWidth - text.Length) / 2), col);
                Console.WriteLine(text);
            }

            string filePath = "";

            if (args.Length == 0)
            {
                TitleDraw.Welcome(Logger);
                DrawCentered("DR2 Format Tools - by avail (alpha)", Color.OrangeRed);

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();

                DrawCentered("Drop a .big or .tex onto me to dump contents.", Color.Cyan);
                DrawCentered("(or drop to exe itself next time instead of launching it first)", Color.Green);

                Logger.Info("\t\t Input -> ", Color.Cyan);

                bool CheckExtension(char[] data, int len)
                {
                    int readOffset = 0;

                    if (data[0] == '"')
                    {
                        readOffset += 1;
                    }

                    if (data[len - (readOffset + 2)] == 't' &&
                        data[len - (readOffset + 1)] == 'e' &&
                        data[len - (readOffset + 0)] == 'x')
                    {
                        return true;
                    }

                    if (data[len - (readOffset + 2)] == 'b' &&
                        data[len - (readOffset + 1)] == 'i' &&
                        data[len - (readOffset + 0)] == 'g')
                    {
                        return true;
                    }

                    return false;
                }

                var input = new char[256];
                for (var i = 0; i < input.Length; i++)
                {
                    input[i] = Console.ReadKey().KeyChar;

                    if (i > 6 && CheckExtension(input, i))
                    {
                        Console.WriteLine();
                        break;
                    }
                }

                filePath = new string(input).Split('\0')[0].Replace("\"", "");

                if (!File.Exists(filePath))
                {
                    FinishWithError("The file you specified isn't valid.");
                }
            }
            else
            {
                filePath = args[0];
                var ext = Path.GetExtension(filePath);

                if (!File.Exists(filePath) || (ext != ".tex" && ext != ".big"))
                {
                    FinishWithError("The file you specified isn't valid.");
                }
            }

            var bf = new BigFile();

            var dumpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dump", Path.GetFileNameWithoutExtension(filePath));
            bool success = bf.Read(filePath, true, dumpDir);

            Console.CursorVisible = origVisible;
            //Console.WindowWidth = origWidth;
            //Console.WindowHeight = origHeight;

            Console.Clear();
            Console.SetCursorPosition(0, 0);

            if (success)
            {
                FinishWithSuccess(ProcessElapsed, MetadataElapsed, ExportElapsed, EntryCount);
            }
        }
    }
}