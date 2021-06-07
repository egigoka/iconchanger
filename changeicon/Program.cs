using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shell32;
using System.Runtime.InteropServices;
using static System.Console;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace changeicon
{

    struct ConfigLine
    {

        public string[] lines_to_match;

        public string iconPath;

        public int iconIndex;
        
        public static ConfigLine Parse(string raw_config_string)
        {
            ConfigLine output = new ConfigLine();

            string[] substrings_good = new string[3];

            int substrings_good_index = 0;

            string current_substring = "";
            
            bool we_are_inside_quotes = false;

            for (int i = 0; i < raw_config_string.Length; i++)
            {
                char ch = raw_config_string[i];
                bool previous_char_is_dquote;
                try
                {
                    previous_char_is_dquote = raw_config_string[i - 1] == '"';
                }
                catch (IndexOutOfRangeException)
                {
                    previous_char_is_dquote = false;
                }
                bool its_comma = ch == ',';
                bool its_quote = ch == '"';

                if (ch == '"') 
                {
                    we_are_inside_quotes = !we_are_inside_quotes; 
                }

                else if (ch == ',') its_comma = true;

                if (its_comma && !we_are_inside_quotes) // it's ',' and not inside '"'
                {
                    substrings_good[substrings_good_index] = current_substring;
                    current_substring = "";
                    substrings_good_index++;
                    continue;
                }
                if (its_quote && !previous_char_is_dquote) // it's first or single '"'
                {
                    continue;
                }
                current_substring += ch;
                
            }
            substrings_good[substrings_good_index] = current_substring;

            output.lines_to_match = substrings_good[0].Substring(1, substrings_good[0].Length-2).Split(',');
            output.iconPath = substrings_good[1];
            output.iconIndex = Int32.Parse(substrings_good[2]);

            return output;
        }

    }

    class Program
    {
        [DllImport("Shell32", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phIconLarge, IntPtr[] phIconSmall, int nIcons);

        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        private static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll")]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string file, int index);

        static List<Process> GetProcessesWithMainWindowTItle()
        {
            Process[] processlist = Process.GetProcesses();
            List<Process> good_processes = new List<Process>();

            foreach (Process theprocess in processlist)
            {
                if (theprocess.MainWindowTitle != "")
                {
                    good_processes.Add(theprocess);
                }
            }

            return good_processes;
        }

        static List<string> GetProcessesMainWindowTitles()
        {
            List<string> processes_titles = new List<string>();
            List<Process> processes = GetProcessesWithMainWindowTItle();
            foreach(Process theprocess in processes)
            {
                processes_titles.Add(theprocess.MainWindowTitle);
            }
            return processes_titles;
        }

        static void SetIcon(string[] windowLabelExpression, string iconName, int iconIndex)
        {
            // init vars
            IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
            IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };
            IntPtr ZeroPtr = IntPtr.Zero;
            IntPtr hInst = IntPtr.Zero;
            uint WM_SETICON = 0x0080;

            // find processes
            List<Process> processlist = GetProcessesWithMainWindowTItle();

            List<Process> good_processes = new List<Process>();

            foreach (Process theprocess in processlist)
            {
                bool add_process = true;
                foreach (string title_substring in windowLabelExpression)
                {
                    if (!theprocess.MainWindowTitle.Contains(title_substring))
                    {
                        add_process = false;
                        break;
                    }
                }
                if (add_process)
                {
                    good_processes.Add(theprocess);
                    WriteLine("Found Process: {0} ID: {1} Handle: {2} Title: {3}", theprocess.ProcessName, theprocess.Id, theprocess.MainWindowHandle, theprocess.MainWindowTitle);
                }
            }

            //get icon
            //int readIconCount = ExtractIconEx(iconName, iconIndex, hDummy, hIconEx, 1);
            IntPtr extracted_icon = ExtractIcon(hInst, iconName, iconIndex);

            WriteLine("extracted_icon " + extracted_icon);
            //WriteLine("extracted_icon " + extracted_icon + ", hDummy[0] " + hDummy[0] + ", hIconEx[0] " + hIconEx[0] + ", readIconCount " + readIconCount);

            if (hIconEx[0] != IntPtr.Zero || hDummy[0] != IntPtr.Zero || extracted_icon != IntPtr.Zero)
            {
                // get first extracted icon
                //IntPtr extractedIcon = hIconEx[0];
                WriteLine("icon " + iconName + " found!");

                // set icon
                foreach (Process good_process in good_processes)
                {
                    // i f... don't know why sometimes work ExtractIcon and sometimes ExtractIconEx
                    //SendMessage(good_process.MainWindowHandle, WM_SETICON, ZeroPtr, extractedIcon);
                    //WriteLine("SendMessage(" + good_process.MainWindowHandle + ", " + WM_SETICON + ", " + ZeroPtr + ", " + extractedIcon + ")");
                    //
                    //Thread.Sleep(10000);

                    //SendMessage(good_process.MainWindowHandle, WM_SETICON, ZeroPtr, hDummy[0]);
                    //WriteLine("SendMessage(" + good_process.MainWindowHandle + ", " + WM_SETICON + ", " + ZeroPtr + ", " + hDummy[0] + ")");
                    //
                    //Thread.Sleep(10000);

                    SendMessage(good_process.MainWindowHandle, WM_SETICON, ZeroPtr, extracted_icon);
                    WriteLine("SendMessage(" + good_process.MainWindowHandle + ", " + WM_SETICON + ", " + ZeroPtr + ", " + extracted_icon + ")");

                    //Thread.Sleep(10000);
                }
            }
            else // no icons found
            {
                WriteLine("icon " + iconName + " not read. Index " + iconIndex);
            }

            // destroy
            Thread.Sleep(2000);
            foreach (IntPtr ptr in hIconEx)
                if (ptr != IntPtr.Zero)
                    DestroyIcon(ptr);

            foreach (IntPtr ptr in hDummy)
                if (ptr != IntPtr.Zero)
                    DestroyIcon(ptr);
            DestroyIcon(extracted_icon);
        }

        static ConfigLine[] ReadConfig(string configPath)
        {

            string[] lines = File.ReadAllLines(configPath);

            ConfigLine[] output = new ConfigLine[lines.Length-1];

            for (int cnt = 1; cnt<lines.Length; cnt++) 
            {
                output[cnt-1] = ConfigLine.Parse(lines[cnt]);
            }

            return output;
        }

        static void ReloadConfigAndSetIcons() 
        {

            // clear icons cache
            //ie4uinit.exe -ClearIconCache
            //ie4uinit.exe -show
            
            Process.Start(@"C:\Windows\SysNative\ie4uinit.exe", "-ClearIconCache");
            WriteLine(@"C:\Windows\SysNative\ie4uinit.exe" + " -ClearIconCache");
            //Thread.Sleep(10000);
            Process.Start(@"C:\Windows\SysNative\ie4uinit.exe", "-show");
            WriteLine(@"C:\Windows\SysNative\ie4uinit.exe" + " -show");
            //Thread.Sleep(10000);

            ConfigLine[] config;
            try
            {
                config = ReadConfig("config.txt");
                WriteLine(@"Config loaded from config.txt");
            } catch (FileNotFoundException)
            {
                config = ReadConfig(@"\\KGN-MK-W0013\iconchanger\changeicon\bin\Debug\config.txt");
                WriteLine(@"Config loaded from \\KGN-MK-W0013\iconchanger\changeicon\bin\Debug\config.txt");
            }
            

            foreach (ConfigLine setting in config)
            {
                SetIcon(setting.lines_to_match, setting.iconPath, setting.iconIndex);
            }
        }

        public static bool CompareLists(List<string> list1, List<string> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                    return false;
            }

            return true;
        }

        static void Main(string[] args)
        {
            WriteLine("iconchanger v0.2.13a");

            if (args.Contains("-o") || args.Contains("-once"))
            {
                ReloadConfigAndSetIcons();
            }
            else
            {
                List<string> previous_processes_with_mainWindowTItle = new List<string>();
                while (true)
                {
                    List<string> current_processes_with_mainWindowTItle = GetProcessesMainWindowTitles();
                    if (CompareLists(current_processes_with_mainWindowTItle, previous_processes_with_mainWindowTItle))
                    {
                        WriteLine("no changes, sleep");
                        
                    }
                    else
                    {
                        ReloadConfigAndSetIcons();
                        previous_processes_with_mainWindowTItle = current_processes_with_mainWindowTItle;
                    }
                    
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
