using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace BemaniDiscord
{
    class IIDX : GameBase
    {

        struct SongInfo
        {
            public string ID;
            public int[] totalNotes; /* SPB, SPN, SPH, SPA, SPL, DPB, DPN, DPH, DPA, DPL */
            public int[] level; /* SPB, SPN, SPH, SPA, SPL, DPB, DPN, DPH, DPA, DPL */
            public string title;
            public string title_english;
            public string artist;
            public string genre;
            public string bpm;
        }
    enum Difficulty { 
        SPB = 0,
        SPN,
        SPH,
        SPA,
        SPL,
        DPB,
        DPN,
        DPH,
        DPA,
        DPL
    }
        Dictionary<string, SongInfo> songDb = new Dictionary<string, SongInfo>();
        readonly Dictionary<string, string> knownEncodingIssues = new Dictionary<string, string>();

        /* Addresses */
        long songlist;
        long currentSong;
        long judgeData;

        public override string ImgName()
        {
            return "infinitas";
        }
        public override string GetSongString()
        {
            var buffer = Util.ReadData(handle, currentSong, 32);
            int songid = Util.BytesToInt32(buffer, 0);
            int diff = Util.BytesToInt32(buffer, 4);
            return $"{songDb[songid.ToString("D5")].title} {(Difficulty)diff}";
        }

        public override GameState GetState()
        {
            return FetchGameState();
        }

        public override bool IsLoaded()
        {
            var buffer = Util.ReadData(handle, songlist, 64);
            var title = Encoding.GetEncoding("Shift-JIS").GetString(buffer.Where(x => x != 0).ToArray());
            return title.Contains("5.1.1.");
        }
        public override void Init()
        {
            LoadEncodingFixes();
            FetchSongDataBase();
        }
        #region Support funcs
        public override void LoadOffsets()
        {
            var lines = File.ReadAllLines(@"D:\Software\Reflux\offsets.txt");
            for(int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var sections = line.Split('=');
                if(sections.Length != 2) { continue; }
                sections[0] = sections[0].Trim();
                sections[1] = sections[1].Trim();
                var offset = Convert.ToInt64(sections[1], 16);
                switch (sections[0].ToLower())
                {
                    case "judgedata": judgeData = offset; break;
                    case "songlist": songlist = offset; break;
                    case "currentsong": currentSong = offset; break;
                }
            }
        }
        void LoadEncodingFixes()
        {
            try
            {
                foreach (var line in File.ReadAllLines(@"D:\Software\Reflux\encodingfixes.txt"))
                {
                    if (!line.Contains('\t')) { continue; } /* Skip version string */
                    var pair = line.Split('\t');
                    knownEncodingIssues.Add(pair[0], pair[1].Trim());
                }
            } catch (Exception e)
            {
            }
        }
        GameState FetchGameState()
        {
            short word = 4;

            var buffer = Util.ReadData(handle, judgeData + (word * 24), 4);
            var marker = Util.BytesToInt32(buffer, 0);
            if (marker != 0)
            {
                return GameState.Playing;
            }
            return GameState.Menu;
        }

        void FetchSongDataBase()
        {
            Dictionary<string, SongInfo> result = new Dictionary<string, SongInfo>();
            var current_position = 0;
            while (true)
            {

                var songInfo = FetchSongInfo(songlist + current_position);

                if (songInfo.title == null)
                {
                    break;
                }

                if (knownEncodingIssues.ContainsKey(songInfo.title))
                {
                    songInfo.title = knownEncodingIssues[songInfo.title];
                }
                if (knownEncodingIssues.ContainsKey(songInfo.artist))
                {
                    songInfo.artist = knownEncodingIssues[songInfo.artist];
                }
                if (!result.ContainsKey(songInfo.ID))
                {
                    result.Add(songInfo.ID, songInfo);
                }

                current_position += 0x3F0;

            }
            songDb = result;
        }
        private SongInfo FetchSongInfo(long position)
        {
            short slab = 64;
            short word = 4; /* Int32 */

            var buffer = Util.ReadData(handle, position, 1008);

            var title1 = Encoding.GetEncoding("Shift-JIS").GetString(buffer.Take(slab).Where(x => x != 0).ToArray());

            if (Util.BytesToInt32(buffer.Take(slab).ToArray(), 0) == 0)
            {
                return new SongInfo();
            }

            var title2 = Encoding.GetEncoding("Shift-JIS").GetString(buffer.Skip(slab).Take(slab).Where(x => x != 0).ToArray());
            var genre = Encoding.GetEncoding("Shift-JIS").GetString(buffer.Skip(slab * 2).Take(slab).Where(x => x != 0).ToArray());
            var artist = Encoding.GetEncoding("Shift-JIS").GetString(buffer.Skip(slab * 3).Take(slab).Where(x => x != 0).ToArray());

            var diff_section = buffer.Skip(slab * 4 + slab / 2).Take(10).ToArray();
            var diff_levels = new int[] { 
                Convert.ToInt32(diff_section[0]),
                Convert.ToInt32(diff_section[1]),
                Convert.ToInt32(diff_section[2]),
                Convert.ToInt32(diff_section[3]),
                Convert.ToInt32(diff_section[4]),
                Convert.ToInt32(diff_section[5]),
                Convert.ToInt32(diff_section[6]),
                Convert.ToInt32(diff_section[7]),
                Convert.ToInt32(diff_section[8]),
                Convert.ToInt32(diff_section[9]) };

            var bpms = buffer.Skip(slab * 5).Take(8).ToArray();
            var noteCounts_bytes = buffer.Skip(slab * 6 + 48).Take(slab).ToArray();

            var bpmMax = Util.BytesToInt32(bpms, 0);
            var bpmMin = Util.BytesToInt32(bpms, word);

            string bpm = "NA";
            if (bpmMin != 0)
            {
                bpm = $"{bpmMin:000}~{bpmMax:000}";
            }
            else
            {
                bpm = bpmMax.ToString("000");
            }

            var noteCounts = new int[] { 
                Util.BytesToInt32(noteCounts_bytes, 0),
                Util.BytesToInt32(noteCounts_bytes, word),
                Util.BytesToInt32(noteCounts_bytes, word * 2),
                Util.BytesToInt32(noteCounts_bytes, word * 3),
                Util.BytesToInt32(noteCounts_bytes, word * 4),
                Util.BytesToInt32(noteCounts_bytes, word * 5),
                Util.BytesToInt32(noteCounts_bytes, word * 6),
                Util.BytesToInt32(noteCounts_bytes, word * 7),
                Util.BytesToInt32(noteCounts_bytes, word * 8),
                Util.BytesToInt32(noteCounts_bytes, word * 9) 
            };


            var idarray = buffer.Skip(256 + 368).Take(4).ToArray();

            var ID = BitConverter.ToInt32(idarray, 0).ToString("D5");

            var song = new SongInfo
            {
                ID = ID,
                title = title1,
                title_english = title2,
                genre = genre,
                artist = artist,
                bpm = bpm,
                totalNotes = noteCounts,
                level = diff_levels
            };

            return song;

        }
        #endregion
    }
}
