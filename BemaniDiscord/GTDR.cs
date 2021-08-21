﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BemaniDiscord
{
    class GTDR : GameBase
    {
        long db;
        long result;
        long selection;
        long nowPlaying;
        struct Song
        {
            public string title;
            public int songID;
            public double[] guitarDifficulties;
            public double[] bassDifficulties;
            public double[] drumDifficulties;
        }
        Dictionary<int, Song> songdb = new Dictionary<int, Song>();

        public override string ImgName()
        {
            return "gtdr";
        }
        public override string GetSongString()
        {
            var playingID = GetPlayingID();

            var buffer = Util.ReadData(handle, selection, 16);
            var selectID = Util.BytesToInt32(buffer, 0);
            var diff = Util.BytesToInt32(buffer, 4);
            var mode = Util.BytesToInt32(buffer, 12);
            string modename = (mode == 0 ? "GUITAR" : "BASS");
            string difficulty = "";
            switch (diff)
            {
                case 0:
                    difficulty = "BASIC";
                    break;
                case 1:
                    difficulty = "ADVANCED";
                    break;
                case 2:
                    difficulty = "EXTREME";
                    break;
                case 3:
                    difficulty = "MASTER";
                    break;
            }
            string title = songdb[playingID].title;
            return $"{title} ({modename} {difficulty})";
        }

        public override GameState GetState()
        {
            int playingID = GetPlayingID();
            if (playingID == 0)
            {
                return GameState.Menu;
            }
            return GameState.Playing;
        }
        public override void LoadOffsets()
        {
            db = 0x145390F54;
            result = 0x14086744;
            selection = 0x144DF9524;
            nowPlaying = 0x14456ABC8;
        }
        public override bool IsLoaded()
        {
            var song = FetchSongInfo(handle, db + 64);
            return song.title == "I think about you";
        }

        public override void Init()
        {
            Song song;
            int offset = 0;
            do
            {
                song = FetchSongInfo(handle, db + 64 + offset);
                if (song.title == null)
                {
                    break;
                }
                songdb.Add(song.songID, song);
                offset += 364;
            } while (song.guitarDifficulties != null);
            /* Wait for 20 seconds, it's loading super quick */
            Thread.Sleep(20000);
        }
        int GetPlayingID()
        {
            var data = Util.ReadData(handle, nowPlaying, 4);
            int playingID = Util.BytesToInt32(data, 0);
            return playingID;
        }
        Song FetchSongInfo(IntPtr handle, long position)
        {
            short slab = 64;

            var buffer = Util.ReadData(handle, position, 1008);

            var ID = Util.BytesToInt32(buffer, 0);
            var diff_section = buffer.Skip(6).Take(30).ToArray();
            var gt_diff_levels = new double[] {
                Util.BytesToInt16(diff_section, 0) / 100.0,
                Util.BytesToInt16(diff_section, 2) / 100.0,
                Util.BytesToInt16(diff_section, 4) / 100.0,
                Util.BytesToInt16(diff_section, 6) / 100.0
                };
            var dm_diff_levels = new double[] {
                Util.BytesToInt16(diff_section, 10) / 100.0,
                Util.BytesToInt16(diff_section, 12) / 100.0,
                Util.BytesToInt16(diff_section, 14) / 100.0,
                Util.BytesToInt16(diff_section, 16) / 100.0
                };
            var bs_diff_levels = new double[] {
                Util.BytesToInt16(diff_section, 20) / 100.0,
                Util.BytesToInt16(diff_section, 22) / 100.0,
                Util.BytesToInt16(diff_section, 24) / 100.0,
                Util.BytesToInt16(diff_section, 26) / 100.0
                };

            var data = buffer.Skip(242).Take(slab).Where(x => x != 0).ToArray();
            //var title = Encoding.GetEncoding("Shift-JIS").GetString(data);
            var title = Encoding.GetEncoding("UTF-8").GetString(data);

            if (Util.BytesToInt32(buffer.Skip(242).Take(slab).ToArray(), 0) == 0)
            {
                return new Song();
            }

            var song = new Song
            {
                songID = ID,
                title = title,
                drumDifficulties = dm_diff_levels,
                guitarDifficulties = gt_diff_levels,
                bassDifficulties = bs_diff_levels
            };

            return song;

        }
    }
}