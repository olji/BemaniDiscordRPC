using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace BemaniDiscord
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
            Int64 lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        static Discord.Discord client;

        enum GAME_ACTIVE { GTDR, IIDX };
        [STAThread]
        static void Run()
        {
            long appid = 0;
            GameBase game = null;
            Process process = null;
            do
            {
                try
                {
                    process = null;
                    game = null;
                    Console.WriteLine("Trying to hook to bemani game...");
                    do
                    {
                        var processes = Process.GetProcessesByName("gitadora");
                        if (processes.Any())
                        {
                            appid = 876124615733305396;
                            process = processes[0];
                            game = new GTDR();
                            Console.WriteLine("Attached to gitadora.exe");
                        }
                        processes = Process.GetProcessesByName("bm2dx");
                        if (processes.Any())
                        {
                            appid = 876124685111291957;
                            process = processes[0];
                            game = new IIDX();
                            Console.WriteLine("Attached to bm2dx.exe");
                        }
                    } while (process == null);

                    IntPtr processHandle = OpenProcess(0x0410, false, process.Id); /* Open process for memory read */

                    game.handle = processHandle;
                    game.UpdateSupportFiles();
                    game.LoadOffsets();
                    while (!game.IsLoaded())
                    {
                        Thread.Sleep(2000);
                    }
                    Console.WriteLine("Game is loaded, initializing...");
                    /* Extra wait in case data loads in slowly */
                    Thread.Sleep(5000);

                    game.Init();

                    client = new Discord.Discord(appid, (UInt64)Discord.CreateFlags.Default);

                    var activityManager = client.GetActivityManager();

                    var activity = new Discord.Activity
                    {
                        Details = "In menu",
                        Assets =
                    {
                        LargeImage = game.ImgName()
                    }

                    };
                    activityManager.UpdateActivity(activity, (res) =>
                    {
                        Console.WriteLine("Entered callback");
                        if (res == Discord.Result.Ok)
                        {
                            Console.WriteLine("Wrote RPC properly");
                        }
                    });
                    Console.WriteLine("Entering loop");
                    while (!process.HasExited)
                    {
                        client.RunCallbacks();
                        var state = game.GetState();
                        if (state == GameState.Menu)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        string songString = game.GetSongString();

                        activity = new Discord.Activity
                        {
                            Details = "Playing",
                            State = songString,
                            Assets =
                        {
                            LargeImage = game.ImgName()
                        }

                        };
                        activityManager.UpdateActivity(activity, (res) =>
                        {
                            if (res == Discord.Result.Ok)
                            {
                            }
                        });
                        while (game.GetState() != GameState.Menu)
                        {
                            client.RunCallbacks();
                            Thread.Sleep(1000);
                        }
                        activity = new Discord.Activity
                        {
                            Details = "In menu",
                            Assets =
                    {
                        LargeImage = game.ImgName()
                    }

                        };
                        activityManager.UpdateActivity(activity, (res) =>
                        {
                            if (res == Discord.Result.Ok)
                            {
                            }
                        });
                        client.RunCallbacks();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e.Message}");
                }
                finally
                {
                    Console.WriteLine("Exiting...");
                    client.ActivityManagerInstance.ClearActivity((res) => { });
                    client.Dispose();
                }
            } while (true);
        }

        #region GUI control support stuff
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll")]
        static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        static NotifyIcon notifyIcon;
        static IntPtr processHandle;
        static IntPtr shell;
        static IntPtr desktop;
        static MenuItem hideMenu;
        static MenuItem restoreMenu;
        static void Main(string[] args)
        {
            notifyIcon = new NotifyIcon();
            try
            {
                notifyIcon.Icon = new Icon("icon.ico");
            } catch
            {
                Console.WriteLine("Couldn't open \"icon.ico\" for use as tray icon");
            }
            notifyIcon.Text = "BemaniRPC";
            notifyIcon.Visible = true;

            ContextMenu menu = new ContextMenu();
            hideMenu = new MenuItem("Hide", new EventHandler(Hide));
            restoreMenu = new MenuItem("Show", new EventHandler(Show));

            menu.MenuItems.Add(restoreMenu);
            menu.MenuItems.Add(hideMenu);
            menu.MenuItems.Add(new MenuItem("Exit", new EventHandler(Exit)));

            notifyIcon.ContextMenu = menu;
            Console.WriteLine("# Use tray icon to hide me #\n\n");
            Task.Factory.StartNew(Run);

            processHandle = Process.GetCurrentProcess().MainWindowHandle;

            bool hide = args.Any(x => x == "-hidden");

            HideWindow(hide);

            Application.Run();
        }
        static void Exit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
            Environment.Exit(0);
        }
        static void Hide(object sender, EventArgs e)
        {
            HideWindow(true);
        }
        static void Show(object sender, EventArgs e)
        {
            HideWindow(false);
        }
        static void HideWindow(bool hide)
        {

            var hwnd = FindWindow(null, Console.Title);
            if(hwnd == IntPtr.Zero)
            {
                return;
            }
            if (hide)
            {
                restoreMenu.Enabled = true;
                hideMenu.Enabled = false;
                ShowWindow(hwnd, 0);
            } else
            {
                restoreMenu.Enabled = false;
                hideMenu.Enabled = true;
                ShowWindow(hwnd, 1);
            }
        }
        #endregion
    }
}
