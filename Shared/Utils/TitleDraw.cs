using AwesomeLogger.Loggers;
using System;
using System.Drawing;

namespace DR2.Utils
{
	public class TitleDraw
	{
		private static readonly string[] ms_exporting = new string[]
		{
			"",
			"███████╗██╗  ██╗██████╗  ██████╗ ██████╗ ████████╗██╗███╗   ██╗ ██████╗ ",
			"██╔════╝╚██╗██╔╝██╔══██╗██╔═══██╗██╔══██╗╚══██╔══╝██║████╗  ██║██╔════╝ ",
			"█████╗   ╚███╔╝ ██████╔╝██║   ██║██████╔╝   ██║   ██║██╔██╗ ██║██║  ███╗",
			"██╔══╝   ██╔██╗ ██╔═══╝ ██║   ██║██╔══██╗   ██║   ██║██║╚██╗██║██║   ██║",
			"███████╗██╔╝ ██╗██║     ╚██████╔╝██║  ██║   ██║   ██║██║ ╚████║╚██████╔╝",
			"╚══════╝╚═╝  ╚═╝╚═╝      ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚═╝╚═╝  ╚═══╝ ╚═════╝ "
		};

		private static readonly string[] ms_failed = new string[]
		{
            "",
            "██████╗ ██╗   ██╗███╗   ███╗██████╗     ███████╗ █████╗ ██╗██╗     ███████╗██████╗ ",
            "██╔══██╗██║   ██║████╗ ████║██╔══██╗    ██╔════╝██╔══██╗██║██║     ██╔════╝██╔══██╗",
            "██║  ██║██║   ██║██╔████╔██║██████╔╝    █████╗  ███████║██║██║     █████╗  ██║  ██║",
            "██║  ██║██║   ██║██║╚██╔╝██║██╔═══╝     ██╔══╝  ██╔══██║██║██║     ██╔══╝  ██║  ██║",
            "██████╔╝╚██████╔╝██║ ╚═╝ ██║██║         ██║     ██║  ██║██║███████╗███████╗██████╔╝",
            "╚═════╝  ╚═════╝ ╚═╝     ╚═╝╚═╝         ╚═╝     ╚═╝  ╚═╝╚═╝╚══════╝╚══════╝╚═════╝ "
        };

		private static readonly string[] ms_complete = new string[]
		{
            "",
			" ██████╗ ██████╗ ███╗   ███╗██████╗ ██╗     ███████╗████████╗███████╗",
            "██╔════╝██╔═══██╗████╗ ████║██╔══██╗██║     ██╔════╝╚══██╔══╝██╔════╝",
            "██║     ██║   ██║██╔████╔██║██████╔╝██║     █████╗     ██║   █████╗  ",
            "██║     ██║   ██║██║╚██╔╝██║██╔═══╝ ██║     ██╔══╝     ██║   ██╔══╝  ",
            "╚██████╗╚██████╔╝██║ ╚═╝ ██║██║     ███████╗███████╗   ██║   ███████╗",
            " ╚═════╝ ╚═════╝ ╚═╝     ╚═╝╚═╝     ╚══════╝╚══════╝   ╚═╝   ╚══════╝"
        };

		private static readonly string[] ms_welcome = new string[]
		{
            "",
            "██████╗ ██████╗ ██████╗ ███████╗████████╗",
            "██╔══██╗██╔══██╗╚════██╗██╔════╝╚══██╔══╝",
            "██║  ██║██████╔╝ █████╔╝█████╗     ██║   ",
            "██║  ██║██╔══██╗██╔═══╝ ██╔══╝     ██║   ",
            "██████╔╝██║  ██║███████╗██║        ██║   ",
            "╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝        ╚═╝   ",
        };

		static void DrawTitle(LogConsole logger, string[] arr, Color col)
        {
			var width = arr[1].Length;
			var consoleWidth = Console.BufferWidth;

			string prefix = "";

			int c = (consoleWidth / 2) - (width / 2);

			for (int i = 0; i < c; i++)
            {
				prefix += " ";
            }

			foreach (var line in arr)
            {
				logger.InfoL(prefix + line, col);
            }
        }

		public static void Exporting(LogConsole logger) => DrawTitle(logger, ms_exporting, Color.Cyan);
		public static void DumpFailed(LogConsole logger) => DrawTitle(logger, ms_failed, Color.OrangeRed);
		public static void Complete(LogConsole logger) => DrawTitle(logger, ms_complete, Color.LimeGreen);
		public static void Welcome(LogConsole logger) => DrawTitle(logger, ms_welcome, Color.DarkOrange);
	}
}