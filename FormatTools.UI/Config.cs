using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FormatTools.UI
{
    public class Config
    {
        private static string Xor(string text, int xorKey)
        {
            StringBuilder input = new StringBuilder(text);
            StringBuilder output = new StringBuilder(text.Length);

            char ch;
            for (int i = 0; i < text.Length; i++)
            {
                ch = input[i];
                ch = (char)(ch ^ xorKey);

                output.Append(ch);
            }

            return output.ToString();
        }

        public string GamePath { get; set; }

        public static bool Initialize(out Config config)
        {
            var baseDir = Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "DR2Tools");
            Directory.CreateDirectory(baseDir);

            var configPath = Path.Combine(baseDir, "config.bin");

            // attempt to load existing config
            if (File.Exists(configPath))
            {
                var txt = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<Config>(Xor(txt, 0x33));
                return true;
            }
            // prompt user and create new config
            else
            {
                Config cfg = new Config();

                cfg.GamePath = GetGameDir();

                var cfgStr = JsonSerializer.Serialize(cfg);
                File.WriteAllText(configPath, Xor(cfgStr, 0x33));

                config = cfg;
            }

            return false;
        }

        private static string GetGameDir()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select directory containing DR2";
            fbd.RootFolder = Environment.SpecialFolder.ProgramFiles;

            var res = fbd.ShowDialog();

            var files = Directory.GetFiles(fbd.SelectedPath);
            var hasGame = files.Any(f => f.ToLowerInvariant().EndsWith("deadrising2.exe"));

            if ((res == DialogResult.OK ||
                 res == DialogResult.Yes) &&
                hasGame)
            {
                return fbd.SelectedPath;
            }
            else
            {
                var mb = MessageBox.Show("Specified directory is invalid. Try again.", "DR2FormatTools", MessageBoxButtons.RetryCancel);

                if (mb == DialogResult.Cancel)
                {
                    Environment.Exit(0);
                }

                return GetGameDir();
            }
        }
    }
}
