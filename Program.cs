using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace rofi_volume
{
    class Program
    {
        public static void Main(string[] args)
        {
            var parser = new rofi_parser();
            if (args.Length > 0)
            {
                parser.repeatCommandGen(args[0]);
                runfromindex(parser.resolveIndex(args[0]));
            }
            Console.WriteLine(parser.prompt);
        }

        static void runfromindex(int index)
        {
            switch (index)
            {
                case 0:
                    amixer.IncreaseVolumeForSink("Master", 10);
                    break;
                case 1:
                    amixer.IncreaseVolumeForSink("Master", 5);
                    break;
                case 2:
                    amixer.IncreaseVolumeForSink("Master", 1);
                    break;
                case 3:
                    amixer.ToggleMuteForSink("Master");
                    break;
                case 4:
                    amixer.IncreaseVolumeForSink("Master", -1);
                    break;
                case 5:
                    amixer.IncreaseVolumeForSink("Master", -5);
                    break;
                case 6:
                    amixer.IncreaseVolumeForSink("Master", -10);
                    break;
            }
        }
    }

    class rofi_parser
    {
        private string promptHeaders { get { return $"\0prompt\x1fVol: {new progbar(42, amixer.GetVolumeForSinkLeft("Master")).text} {amixer.GetVolumeForSinkLeft("Master").ToString()}%\n\0no-custom\x1f"; } }
        private static string[] promptLines = new string[] { "🔊 +10%", "🔉 +5%", "🔈 +1%", "🔇 Toggle Mute", "🔈 -1%", "🔉 -5%", "🔊 -10%" };
        public string prompt
        {
            get
            {
                var text = "";
                text += promptHeaders + "\n";
                foreach (var line in promptLines)
                {
                    text += line + "\n";
                }
                return text;
            }
        }

        public int resolveIndex(string input)
        {
            List<string> promptLineList = promptLines.ToList();
            if (promptLineList.Where(x => x.EndsWith(" [repeat]")).ToArray().Length > 0)
            {
                var removeRepeat = promptLineList.Where(x => x.EndsWith(" [repeat]")).First();
                removeRepeat = removeRepeat.Remove(removeRepeat.IndexOf("[") - 1);
                return promptLineList.Skip(1).ToList().FindIndex(x => x == removeRepeat);
            }
            return promptLineList.FindIndex(x => x == input);
        }

        public void repeatCommandGen(string arg)
        {
            if (arg.EndsWith(" [repeat]"))
            {
                promptLines = promptLines.Prepend(arg.Remove(arg.LastIndexOf("[") - 1) + " [repeat]").ToArray<string>();
            }
            else
            {
                promptLines = promptLines.Prepend(arg + " [repeat]").ToArray<string>();
            }
        }
    }

    internal class progbar
    {
        private readonly string fullChar = "▓";
        private readonly string emptyChar = "░";

        public uint charLenght { get; set; }

        private double actualLenght { get { return charLenght - 2; } }

        public uint percentage
        {
            get
            {
                return _percentage;
            }
            set
            {
                if (value > 100)
                {
                    _percentage = 100;
                }
                else
                {
                    _percentage = value;
                }
            }
        }

        private uint _percentage;

        public string text { get; set; }

        public progbar(uint charLenght, uint initialPercentage)
        {
            this.percentage = initialPercentage;
            this.charLenght = charLenght;
            this.text = drawbar();
        }

        private string drawbar()
        {
            double text_full_length = Math.Floor(actualLenght / 100 * percentage);
            double text_empty_length = actualLenght - text_full_length;

            var text_full = "";
            for (int i = 0; i < text_full_length; i++)
            {
                text_full = text_full + fullChar;
            }

            var text_empty = "";
            for (int i = 0; i < text_empty_length; i++)
            {
                text_full = text_full + emptyChar;
            }

            return $"[{text_full}{text_empty}]";
        }

        public void increase(int ammount)
        {
            if (ammount >= 0)
            {
                percentage = percentage + (uint)ammount;
            }
            else
            {
                percentage = percentage - Convert.ToUInt32(ammount * -1);
            }
            this.text = drawbar();
        }

        public void clear()
        {
            percentage = 0;
            text = drawbar();
        }

        public void setPercent(uint percentage)
        {
            this.percentage = percentage;
            text = drawbar();
        }
    }

    internal class bashProcesses
    {
        public static string shellLaunch(string FileName, string[] args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                FileName = FileName,
                RedirectStandardOutput = true,
            };
            Process proc = new Process();
            proc.StartInfo = startInfo;
            proc.Start();
            proc.WaitForExit();
            return proc.StandardOutput.ReadToEnd();
        }
        public static string amixer(params string[] args)
        {
            return shellLaunch("amixer", args);
        }
    }

    internal static class amixer
    {
        public static uint GetVolumeForSinkLeft(string sink)
        {
            string stdout = bashProcesses.amixer("get", $"\'{sink}\'");
            int leftstart = stdout.IndexOf("Left: ");
            string leftline = stdout.Substring(leftstart, stdout.Substring(leftstart).IndexOf("]") + 1);
            string volumeStr = leftline.Substring(leftline.IndexOf("[") + 1, leftline.Substring(leftline.IndexOf("[") + 1).IndexOf("]") - 1);
            return Convert.ToUInt32(volumeStr);
        }

        public static uint GetVolumeForSinkRight(string sink)
        {
            string stdout = bashProcesses.amixer("get", $"\'{sink}\'");
            int leftstart = stdout.IndexOf("Right: ");
            string leftline = stdout.Substring(leftstart, stdout.Substring(leftstart).IndexOf("]") + 1);
            string volumeStr = leftline.Substring(leftline.IndexOf("[") + 1, leftline.Substring(leftline.IndexOf("[") + 1).IndexOf("]") - 1);
            return Convert.ToUInt32(volumeStr);
        }

        public static string SetVolumeForSink(string sink, uint volume)
        {
            return bashProcesses.amixer("set", $"{sink}", $"{volume.ToString()}%");
        }

        public static string IncreaseVolumeForSink(string sink, int volume)
        {
            if (volume >= 0)
            {
                return bashProcesses.amixer("set", $"{sink}", $"{volume.ToString()}%+");
            }
            else
            {
                var negativevol = volume * -1;
                return bashProcesses.amixer("set", $"{sink}", $"{negativevol.ToString()}%-");
            }
        }

        public static string ToggleMuteForSink(string sink)
        {
            return bashProcesses.amixer("set", "Master", "toggle");
        }
    }
}