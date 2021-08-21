using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace BemaniDiscord
{
    class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
            Int64 lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        static Discord.Discord client;

        enum GAME_ACTIVE { GTDR, IIDX };
        static void Main(string[] args)
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
                            Console.WriteLine("Entered callback");
                            if (res == Discord.Result.Ok)
                            {
                                Console.WriteLine("Wrote RPC properly");
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
                            Console.WriteLine("Entered callback");
                            if (res == Discord.Result.Ok)
                            {
                                Console.WriteLine("Wrote RPC properly");
                            }
                        });
                        client.RunCallbacks();
                        Console.WriteLine("Finished");
                    }
                }
                catch
                {
                }
                finally
                {
                    client.ActivityManagerInstance.ClearActivity((res) => { });
                    client.Dispose();
                }
            } while (true);
        }
    }
}
