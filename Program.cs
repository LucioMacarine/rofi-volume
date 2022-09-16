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
                // parser.repeatCommandGen(args[0]);
                runfromindex(parser.resolveIndex(args[0]));
            }
            Console.WriteLine(parser.prompt);
        }

        static void runfromindex(int index)
        {
            switch (index)
            {
                case 0:
                    amixer.IncreaseVolumeForSource("Master", 10);
                    break;
                case 1:
                    amixer.IncreaseVolumeForSource("Master", 5);
                    break;
                case 2:
                    amixer.IncreaseVolumeForSource("Master", 1);
                    break;
                case 3:
                    amixer.ToggleMuteForSource("Master");
                    break;
                case 4:
                    amixer.IncreaseVolumeForSource("Master", -1);
                    break;
                case 5:
                    amixer.IncreaseVolumeForSource("Master", -5);
                    break;
                case 6:
                    amixer.IncreaseVolumeForSource("Master", -10);
                    break;
            }
        }
    }

    class rofi_parser
    {
        private string promptHeaders
        {
            get
            {
                progbar bar = new progbar(42, 0);
                string vol = $"{amixer.GetVolumeForSourceLeft("Master").ToString()}%";
                if (amixer.IsSourceMuted("Master"))
                {
                    vol = vol + " (MUTED)";
                }
                else
                {
                    bar.setPercent(amixer.GetVolumeForSourceLeft("Master"));
                }
                return $"\0prompt\x1fVol: {bar.text} {vol}\n\0no-custom\x1f";
            }
        }
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
            return promptLineList.FindIndex(x => x == input);
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
        public static uint GetVolumeForSourceLeft(string Source)
        {
            string stdout = bashProcesses.amixer("get", $"\'{Source}\'");
            int leftstart = stdout.IndexOf("Left: ");
            string leftline = stdout.Substring(leftstart, stdout.Substring(leftstart).IndexOf("]") + 1);
            string volumeStr = leftline.Substring(leftline.IndexOf("[") + 1, leftline.Substring(leftline.IndexOf("[") + 1).IndexOf("]") - 1);
            return Convert.ToUInt32(volumeStr);
        }

        public static uint GetVolumeForSourceRight(string Source)
        {
            string stdout = bashProcesses.amixer("get", $"\'{Source}\'");
            int leftstart = stdout.IndexOf("Right: ");
            string leftline = stdout.Substring(leftstart, stdout.Substring(leftstart).LastIndexOf("]") + 1);
            string volumeStr = leftline.Substring(leftline.IndexOf("[") + 1, leftline.Substring(leftline.IndexOf("[") + 1).IndexOf("]") - 1);
            return Convert.ToUInt32(volumeStr);
        }

        public static string SetVolumeForSource(string Source, uint volume)
        {
            return bashProcesses.amixer("set", $"{Source}", $"{volume.ToString()}%");
        }

        public static string IncreaseVolumeForSource(string Source, int volume)
        {
            if (volume >= 0)
            {
                return bashProcesses.amixer("set", Source, $"{volume.ToString()}%+");
            }
            else
            {
                var negativevol = volume * -1;
                return bashProcesses.amixer("set", Source, $"{negativevol.ToString()}%-");
            }
        }

        public static string ToggleMuteForSource(string Source)
        {
            return bashProcesses.amixer("set", Source, "toggle");
        }

        public static bool IsSourceMuted(string Source)
        {
            string stdout = bashProcesses.amixer("get", Source);
            int leftstart = stdout.IndexOf("Left: ");
            string leftline = stdout.Substring(leftstart, stdout.Substring(leftstart).LastIndexOf("]") + 1);
            string mutestr = leftline.Substring(leftline.LastIndexOf("[") + 1, leftline.Substring(leftline.LastIndexOf("[") + 1).IndexOf("]"));
            if (mutestr == "on")
            {
                return false;
            }
            else return true;
        }
    }
}